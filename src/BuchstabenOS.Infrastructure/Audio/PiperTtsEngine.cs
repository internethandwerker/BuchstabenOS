using System.Diagnostics;
using BuchstabenOS.Application.Ports;
using Serilog;

namespace BuchstabenOS.Infrastructure.Audio;

/// <summary>
/// Lokale neuronale Sprachsynthese mit Piper TTS unter Linux.
/// Arbeitet auf einer eigenen Sprach-Spur (Track 2) unabhängig von der Buchstaben-Spur,
/// sodass getippte Buchstaben und gesprochene Wörter parallel erklingen.
/// </summary>
public class PiperTtsEngine : ITtsEngine
{
    private readonly string? _piperExecutable;
    private readonly string? _modelPath;
    private readonly string _cacheDirectory;
    private readonly string? _audioPlayerBinary;
    private static readonly object _synthLock = new();

    public bool IsAvailable => !string.IsNullOrEmpty(_piperExecutable) && File.Exists(_modelPath);

    public PiperTtsEngine(string? customPiperPath = null, string? customModelPath = null)
    {
        _piperExecutable = customPiperPath ?? FindPiperBinary();
        _modelPath = customModelPath ?? FindModelPath();
        _audioPlayerBinary = File.Exists("/usr/bin/pw-play") ? "/usr/bin/pw-play" :
                             (File.Exists("/usr/bin/paplay") ? "/usr/bin/paplay" :
                             (File.Exists("/usr/bin/aplay") ? "/usr/bin/aplay" : null));

        _cacheDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".cache", "buchstabenos", "tts"
        );

        Directory.CreateDirectory(_cacheDirectory);
        Log.Information("PiperTtsEngine initialisiert. Piper: {Piper}, Model: {Model}, Cache: {Cache}",
            _piperExecutable ?? "nicht gefunden", _modelPath ?? "nicht gefunden", _cacheDirectory);
    }

    public async Task SpeakWordAsync(string word, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(word)) return;

        string sanitized = SanitizeWordForFileName(word);
        string cachedFile = Path.Combine(_cacheDirectory, $"{sanitized}.wav");

        // 1. Wenn bereits im Cache vorhanden: Sofort auf Track 2 abspielen!
        if (File.Exists(cachedFile))
        {
            PlayAudioFile(cachedFile);
            return;
        }

        // 2. Wenn Piper verfügbar ist: Synthetisieren, im Cache ablegen und abspielen
        if (IsAvailable)
        {
            bool success = await Task.Run(() => SynthesizeToWavThreadSafe(word, cachedFile), cancellationToken);
            if (success && File.Exists(cachedFile))
            {
                PlayAudioFile(cachedFile);
                return;
            }
        }

        Log.Information("Wort gesprochen (simuliert/Fallback): '{Word}'", word);
    }

    private bool SynthesizeToWavThreadSafe(string text, string targetWavPath)
    {
        if (string.IsNullOrEmpty(_piperExecutable) || string.IsNullOrEmpty(_modelPath)) return false;

        string tempPath = $"{targetWavPath}.{Guid.NewGuid():N}.tmp";

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = _piperExecutable,
                Arguments = $"--model \"{_modelPath}\" --output_file \"{tempPath}\"",
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = psi };
            process.Start();

            using (var writer = process.StandardInput)
            {
                writer.WriteLine(text);
            }

            process.WaitForExit(5000);

            if (process.ExitCode == 0 && File.Exists(tempPath))
            {
                lock (_synthLock)
                {
                    if (!File.Exists(targetWavPath))
                    {
                        File.Move(tempPath, targetWavPath, overwrite: true);
                    }
                    else
                    {
                        File.Delete(tempPath);
                    }
                }
                return true;
            }

            if (File.Exists(tempPath)) File.Delete(tempPath);
            return false;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Fehler bei Piper Sprachsynthese für Wort '{Text}'", text);
            if (File.Exists(tempPath))
            {
                try { File.Delete(tempPath); } catch { }
            }
            return false;
        }
    }

    private void PlayAudioFile(string filePath)
    {
        if (string.IsNullOrEmpty(_audioPlayerBinary)) return;

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = _audioPlayerBinary,
                Arguments = $"\"{filePath}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };
            Process.Start(psi);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Fehler beim Abspielen von Sprach-WAV: {File}", filePath);
        }
    }

    private static string SanitizeWordForFileName(string word)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = word.Trim().ToUpperInvariant().Where(c => !invalid.Contains(c)).ToArray();
        return new string(chars);
    }

    private static string? FindPiperBinary()
    {
        string[] candidates =
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "bin", "piper"),
            "/usr/local/bin/piper",
            "/usr/bin/piper",
            Path.Combine(AppContext.BaseDirectory, "assets", "piper", "piper")
        };

        return candidates.FirstOrDefault(File.Exists);
    }

    private static string? FindModelPath()
    {
        string[] candidates =
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share", "piper", "de_DE-thorsten-medium.onnx"),
            "/usr/share/piper/voices/de/de_DE-thorsten-medium.onnx",
            Path.Combine(AppContext.BaseDirectory, "assets", "piper", "de_DE-thorsten-medium.onnx")
        };

        return candidates.FirstOrDefault(File.Exists);
    }
}
