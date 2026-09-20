using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using BuchstabenOS.Application.Ports;
using Serilog;

namespace BuchstabenOS.Infrastructure.Audio;

/// <summary>
/// Lokale neuronale Sprachsynthese mit Piper TTS unter Linux.
/// Verwendet eine sequentielle Sprach-Warteschlange (Speech Queue), sodass Ansagen
/// (Begrüßung, Aufgabenstellung, Lob, Feedback) niemals gleichzeitig oder überlappend
/// abgespielt werden, sondern verständlich nacheinander mit einer natürlichen Atempause.
/// </summary>
public class PiperTtsEngine : ITtsEngine, IDisposable
{
    private record SpeechRequest(string Word, TaskCompletionSource<bool> Completion, CancellationToken CancellationToken);

    private readonly string? _piperExecutable;
    private readonly string? _modelPath;
    private readonly string _cacheDirectory;
    private readonly string? _audioPlayerBinary;
    private static readonly object _synthLock = new();

    private readonly Channel<SpeechRequest> _speechChannel = Channel.CreateUnbounded<SpeechRequest>(
        new UnboundedChannelOptions { SingleReader = true, SingleWriter = false }
    );
    private readonly CancellationTokenSource _lifecycleCts = new();
    private readonly Task _workerTask;
    private Process? _currentPlayProcess;
    private readonly object _processLock = new();

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
        Log.Information("PiperTtsEngine initialisiert. Piper: {Piper}, Model: {Model}, Player: {Player}, Cache: {Cache}",
            _piperExecutable ?? "nicht gefunden", _modelPath ?? "nicht gefunden", _audioPlayerBinary ?? "keiner", _cacheDirectory);

        _workerTask = Task.Run(() => ProcessSpeechQueueAsync(_lifecycleCts.Token));
    }

    /// <summary>
    /// Reiht eine Sprachansage in die sequentielle Sprach-Warteschlange ein.
    /// Der zurückgegebene Task schließt erst ab, wenn die Sprachansage tatsächlich vollständig abgespielt wurde.
    /// </summary>
    public async Task SpeakWordAsync(string word, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(word)) return;

        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var request = new SpeechRequest(word, tcs, cancellationToken);

        await _speechChannel.Writer.WriteAsync(request, cancellationToken);
        await tcs.Task;
    }

    /// <summary>
    /// Bricht die aktuell laufende Sprachausgabe sofort ab und leert alle noch wartenden Ansagen in der Queue.
    /// </summary>
    public void StopCurrentSpeech()
    {
        lock (_processLock)
        {
            try
            {
                if (_currentPlayProcess != null && !_currentPlayProcess.HasExited)
                {
                    _currentPlayProcess.Kill();
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Prozessabbruch bei StopCurrentSpeech");
            }
            _currentPlayProcess = null;
        }

        // Alle wartenden Anfragen in der Queue abbrechen
        while (_speechChannel.Reader.TryRead(out var pending))
        {
            pending.Completion.TrySetCanceled();
        }
    }

    private async Task ProcessSpeechQueueAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (await _speechChannel.Reader.WaitToReadAsync(cancellationToken))
            {
                while (_speechChannel.Reader.TryRead(out var request))
                {
                    if (request.CancellationToken.IsCancellationRequested || cancellationToken.IsCancellationRequested)
                    {
                        request.Completion.TrySetCanceled();
                        continue;
                    }

                    try
                    {
                        await ExecuteSpeechAsync(request.Word, request.CancellationToken);

                        // Natürliche Atempause zwischen aufeinanderfolgenden Ansagen (ca. 220ms)
                        await Task.Delay(220, cancellationToken);

                        request.Completion.TrySetResult(true);
                    }
                    catch (OperationCanceledException)
                    {
                        request.Completion.TrySetCanceled();
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Fehler bei Sprachausgabe für '{Word}'", request.Word);
                        request.Completion.TrySetException(ex);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Worker beendet
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unerwarteter Fehler im TTS Speech-Queue Worker");
        }
    }

    private async Task ExecuteSpeechAsync(string word, CancellationToken cancellationToken)
    {
        string sanitized = SanitizeWordForFileName(word);
        string cachedFile = Path.Combine(_cacheDirectory, $"{sanitized}.wav");

        // 1. Wenn bereits im Cache vorhanden: Abspielen und auf Ende warten!
        if (File.Exists(cachedFile))
        {
            await PlayAudioFileAsync(cachedFile, cancellationToken);
            return;
        }

        // 2. Wenn Piper verfügbar ist: Synthetisieren, im Cache ablegen und abspielen
        if (IsAvailable)
        {
            bool success = await Task.Run(() => SynthesizeToWavThreadSafe(word, cachedFile), cancellationToken);
            if (success && File.Exists(cachedFile))
            {
                await PlayAudioFileAsync(cachedFile, cancellationToken);
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

            process.WaitForExit(7000);

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

    private async Task PlayAudioFileAsync(string filePath, CancellationToken cancellationToken)
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

            using var process = new Process { StartInfo = psi };
            process.Start();

            lock (_processLock)
            {
                _currentPlayProcess = process;
            }

            try
            {
                await process.WaitForExitAsync(cancellationToken);
            }
            finally
            {
                lock (_processLock)
                {
                    if (_currentPlayProcess == process)
                    {
                        _currentPlayProcess = null;
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            lock (_processLock)
            {
                try
                {
                    _currentPlayProcess?.Kill();
                }
                catch { }
                _currentPlayProcess = null;
            }
            throw;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Fehler beim Abspielen von Sprach-WAV: {File}", filePath);
        }
    }

    private static string SanitizeWordForFileName(string word)
    {
        var invalid = Path.GetInvalidFileNameChars();
        string clean = new string(word.Trim().Where(c => !invalid.Contains(c) && c != '?' && c != '!' && c != ':').ToArray());

        if (clean.Length <= 40)
        {
            return clean.Replace(' ', '_').ToUpperInvariant();
        }

        // Bei längeren Sätzen (z. B. Mathe-Templates): Eindeutigen SHA256-Hash anhängen
        using var sha = SHA256.Create();
        byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(word.Trim()));
        string hashStr = Convert.ToHexString(hash)[..16];
        string prefix = clean[..Math.Min(20, clean.Length)].Replace(' ', '_').ToUpperInvariant();
        return $"{prefix}_{hashStr}";
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

    public void Dispose()
    {
        _lifecycleCts.Cancel();
        StopCurrentSpeech();
        _lifecycleCts.Dispose();
    }
}
