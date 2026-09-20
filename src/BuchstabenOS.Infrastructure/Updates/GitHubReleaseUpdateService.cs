using System;
using System.Diagnostics;
using System.Formats.Tar;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BuchstabenOS.Application.Ports;
using Serilog;

namespace BuchstabenOS.Infrastructure.Updates;

/// <summary>
/// Implementierung des 1-Klick Update-Services über GitHub Releases.
/// Ermöglicht Eltern, ohne Git oder Terminal Updates abzufragen, herunterzuladen,
/// atomar zu installieren und per Knopfdruck neu zu starten.
/// </summary>
public class GitHubReleaseUpdateService : IUpdateService
{
    private const string DefaultRepoOwner = "internethandwerker";
    private const string DefaultRepoName = "BuchstabenOS";

    private readonly HttpClient _httpClient;
    private readonly ISystemControl _systemControl;
    private readonly string _repoOwner;
    private readonly string _repoName;

    public string CurrentVersion { get; }

    public GitHubReleaseUpdateService(
        HttpClient? httpClient = null,
        ISystemControl? systemControl = null,
        string repoOwner = DefaultRepoOwner,
        string repoName = DefaultRepoName,
        string? currentVersionOverride = null)
    {
        _httpClient = httpClient ?? new HttpClient();
        _systemControl = systemControl ?? new System.LinuxSystemControl();
        _repoOwner = repoOwner;
        _repoName = repoName;

        CurrentVersion = currentVersionOverride ?? DetermineCurrentVersion();
    }

    private static string DetermineCurrentVersion()
    {
        var asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        var infoVer = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(infoVer))
        {
            // Eventuelle Git-Commit-Hashes nach '+' entfernen (z. B. 1.0.0+abc1234 -> 1.0.0)
            int plusIndex = infoVer.IndexOf('+');
            return plusIndex > 0 ? infoVer[..plusIndex] : infoVer;
        }

        var ver = asm.GetName().Version;
        return ver != null ? $"{ver.Major}.{ver.Minor}.{ver.Build}" : "1.0.0";
    }

    public async Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        string apiUrl = $"https://api.github.com/repos/{_repoOwner}/{_repoName}/releases/latest";

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("BuchstabenOS", CurrentVersion));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));

            // Optionales GitHub-Token (z. B. für private Test-Repositories)
            string? token = Environment.GetEnvironmentVariable("BUCHSTABENOS_GITHUB_TOKEN");
            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            using var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.StatusCode == global::System.Net.HttpStatusCode.NotFound)
            {
                Log.Information("Keine Releases unter {Url} gefunden.", apiUrl);
                return new UpdateCheckResult(false, CurrentVersion, null, "Noch keine offiziellen Releases auf GitHub veröffentlicht.");
            }

            if (!response.IsSuccessStatusCode)
            {
                string error = $"GitHub API antwortete mit Status {(int)response.StatusCode} ({response.ReasonPhrase})";
                Log.Warning("Update-Prüfung fehlgeschlagen: {Error}", error);
                return new UpdateCheckResult(false, CurrentVersion, null, error);
            }

            string json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            string tagName = root.GetProperty("tag_name").GetString() ?? string.Empty;
            string releaseTitle = root.TryGetProperty("name", out var nameElem) ? nameElem.GetString() ?? tagName : tagName;
            string changelog = root.TryGetProperty("body", out var bodyElem) ? bodyElem.GetString() ?? string.Empty : string.Empty;
            DateTime publishedAt = root.TryGetProperty("published_at", out var pubElem) && pubElem.TryGetDateTime(out var dt) ? dt : DateTime.UtcNow;

            // Suche nach Linux x64 Release Asset (.tar.gz)
            string downloadUrl = string.Empty;
            long fileSize = 0;

            if (root.TryGetProperty("assets", out var assetsElem) && assetsElem.ValueKind == JsonValueKind.Array)
            {
                foreach (var asset in assetsElem.EnumerateArray())
                {
                    string assetName = asset.GetProperty("name").GetString() ?? string.Empty;
                    if (assetName.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase) ||
                        assetName.Contains("linux", StringComparison.OrdinalIgnoreCase))
                    {
                        downloadUrl = asset.GetProperty("browser_download_url").GetString() ?? string.Empty;
                        fileSize = asset.TryGetProperty("size", out var sizeElem) ? sizeElem.GetInt64() : 0;
                        break;
                    }
                }
            }

            string cleanRemoteVersion = tagName.TrimStart('v', 'V');
            string cleanCurrentVersion = CurrentVersion.TrimStart('v', 'V');

            bool isNewer = IsRemoteVersionNewer(cleanRemoteVersion, cleanCurrentVersion);

            if (isNewer && !string.IsNullOrEmpty(downloadUrl))
            {
                var updateInfo = new UpdateInfo(
                    Version: cleanRemoteVersion,
                    ReleaseTitle: releaseTitle,
                    ChangelogMarkdown: changelog,
                    AssetDownloadUrl: downloadUrl,
                    FileSizeBytes: fileSize,
                    PublishedAt: publishedAt
                );

                Log.Information("Neues Update gefunden: v{Version} (Aktuell: v{Current})", cleanRemoteVersion, cleanCurrentVersion);
                return new UpdateCheckResult(true, CurrentVersion, updateInfo);
            }

            Log.Information("BuchstabenOS ist auf dem neuesten Stand (v{Current}).", CurrentVersion);
            return new UpdateCheckResult(false, CurrentVersion, null);
        }
        catch (OperationCanceledException)
        {
            return new UpdateCheckResult(false, CurrentVersion, null, "Update-Prüfung abgebrochen.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Fehler bei der Update-Prüfung");
            return new UpdateCheckResult(false, CurrentVersion, null, $"Verbindungsfehler: {ex.Message}");
        }
    }

    public static bool IsRemoteVersionNewer(string remote, string current)
    {
        if (Version.TryParse(remote, out var remVer) && Version.TryParse(current, out var curVer))
        {
            return remVer > curVer;
        }

        // Fallback-Vergleich
        return string.Compare(remote, current, StringComparison.OrdinalIgnoreCase) > 0;
    }

    public async Task<bool> DownloadAndApplyUpdateAsync(UpdateInfo update, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(update.AssetDownloadUrl))
        {
            throw new ArgumentException("AssetDownloadUrl darf nicht leer sein.", nameof(update));
        }

        string tempDir = Path.Combine(Path.GetTempPath(), "buchstabenos-update");
        Directory.CreateDirectory(tempDir);

        string archivePath = Path.Combine(tempDir, "update.tar.gz");
        string stagingDir = Path.Combine(tempDir, "staging");

        try
        {
            if (Directory.Exists(stagingDir)) Directory.Delete(stagingDir, true);
            Directory.CreateDirectory(stagingDir);

            Log.Information("Lade Update von {Url} nach {File}...", update.AssetDownloadUrl, archivePath);

            // 1. Download mit Fortschrittsanzeige
            using (var response = await _httpClient.GetAsync(update.AssetDownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
            {
                response.EnsureSuccessStatusCode();
                long totalBytes = response.Content.Headers.ContentLength ?? update.FileSizeBytes;

                using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var fileStream = new FileStream(archivePath, FileMode.Create, FileAccess.Write, FileShare.None);

                var buffer = new byte[81920];
                long totalRead = 0;
                int read;

                while ((read = await contentStream.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                    totalRead += read;

                    if (totalBytes > 0)
                    {
                        double percent = (double)totalRead / totalBytes * 100.0;
                        progress?.Report(percent);
                    }
                }
            }

            Log.Information("Download abgeschlossen ({Bytes} Bytes). Entpacke Archiv...", new FileInfo(archivePath).Length);

            // 2. Archiv entpacken (.tar.gz)
            ExtractTarGz(archivePath, stagingDir);

            // 3. Zielverzeichnis ermitteln
            string targetDir = DetermineInstallationDirectory();
            Log.Information("Installiere Update in Zielverzeichnis: {Dir}", targetDir);

            // 4. Atomarer Tausch der Binärdateien unter Linux
            ApplyStagedFiles(stagingDir, targetDir);

            Log.Information("Update erfolgreich installiert! Bereit für Neustart.");
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Fehler beim Herunterladen oder Installieren des Updates!");
            throw;
        }
        finally
        {
            // Aufräumen temporärer Dateien
            try
            {
                if (File.Exists(archivePath)) File.Delete(archivePath);
                if (Directory.Exists(stagingDir)) Directory.Delete(stagingDir, true);
            }
            catch { }
        }
    }

    public void RestartApplication()
    {
        Log.Information("Anwendungs-Neustart angefordert.");
        _systemControl.RestartApp();
    }

    private static void ExtractTarGz(string archivePath, string destinationDir)
    {
        // Versuche erst das System-Tool 'tar' (sehr schnell & erhält Linux-Dateirechte perfekt)
        if (File.Exists("/usr/bin/tar") || File.Exists("/bin/tar"))
        {
            var psi = new ProcessStartInfo
            {
                FileName = "tar",
                Arguments = $"-xzf \"{archivePath}\" -C \"{destinationDir}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            proc?.WaitForExit(15000);
            if (proc?.ExitCode == 0) return;
        }

        // C# Fallback via System.Formats.Tar
        using var fs = File.OpenRead(archivePath);
        using var gz = new GZipStream(fs, CompressionMode.Decompress);
        TarFile.ExtractToDirectory(gz, destinationDir, overwriteFiles: true);
    }

    private static string DetermineInstallationDirectory()
    {
        // Wenn wir im Kiosk unter /opt/buchstabenos laufen:
        if (Directory.Exists("/opt/buchstabenos"))
        {
            return "/opt/buchstabenos";
        }

        // Ansonsten das aktuelle Verzeichnis der laufenden Anwendung
        string baseDir = AppContext.BaseDirectory;
        return baseDir.TrimEnd(Path.DirectorySeparatorChar);
    }

    private static void ApplyStagedFiles(string stagingDir, string targetDir)
    {
        // Finde die Haupt-Binary im Staging-Ordner (entweder direkt oder in einem Unterordner)
        string[] binaries = Directory.GetFiles(stagingDir, "BuchstabenOS.UI.Desktop", SearchOption.AllDirectories);
        string sourceDir = binaries.Length > 0 ? (Path.GetDirectoryName(binaries[0]) ?? stagingDir) : stagingDir;

        // Alle Dateien und Unterordner atomar übertragen
        foreach (string file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(sourceDir, file);
            string targetFile = Path.Combine(targetDir, relative);
            string? targetSubdir = Path.GetDirectoryName(targetFile);

            if (!string.IsNullOrEmpty(targetSubdir) && !Directory.Exists(targetSubdir))
            {
                Directory.CreateDirectory(targetSubdir);
            }

            // Linux-spezifisch: Laufende Binärdateien können nicht direkt überschrieben werden (Text file busy),
            // ABER sie können umbenannt oder gelöscht werden (unlink)!
            if (File.Exists(targetFile))
            {
                string backup = targetFile + ".old";
                try
                {
                    if (File.Exists(backup)) File.Delete(backup);
                    File.Move(targetFile, backup);
                }
                catch
                {
                    // Falls Verschieben fehlschlägt, direkt versuchen zu löschen
                    try { File.Delete(targetFile); } catch { }
                }
            }

            File.Copy(file, targetFile, overwrite: true);

            // Ausführungsrechte (+x) für Binärdateien sicherstellen
            if ((Path.GetFileName(targetFile) == "BuchstabenOS.UI.Desktop" || targetFile.EndsWith(".so")) && OperatingSystem.IsLinux())
            {
                try
                {
                    File.SetUnixFileMode(targetFile,
                        UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                        UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                        UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
                }
                catch { }
            }
        }
    }
}
