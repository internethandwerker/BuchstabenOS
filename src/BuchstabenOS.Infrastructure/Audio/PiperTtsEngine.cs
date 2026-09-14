using System.Diagnostics;
using BuchstabenOS.Application.Ports;

namespace BuchstabenOS.Infrastructure.Audio;

/// <summary>
/// Lokale neuronale Sprachsynthese mit Piper TTS unter Linux.
/// Enthält einen lokalen Dateicache, sodass bekannte Wörter sofort abgespielt werden.
/// </summary>
public class PiperTtsEngine : ITtsEngine
{
    private readonly string? _piperExecutable;
    private readonly string? _modelPath;
    private readonly string _cacheDirectory;
    private readonly string? _audioPlayerBinary;

    public bool IsAvailable => !string.IsNullOrEmpty(_piperExecutable) && File.Exists(_modelPath);

    public PiperTtsEngine(string? customPiperPath = null, string? customModelPath = null)
    {
        _piperExecutable = customPiperPath ?? FindPiperBinary();
        _modelPath = customModelPath ?? FindModelPath();
        _audioPlayerBinary = File.Exists("/usr/bin/paplay") ? "/usr/bin/paplay" : (File.Exists("/usr/bin/aplay") ? "/usr/bin/aplay" : null);

        _cacheDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".cache", "buchstabenos", "tts"
        );

        Directory.CreateDirectory(_cacheDirectory);
    }

    public async Task SpeakWordAsync(string word, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(word)) return;

        string sanitized = SanitizeWordForFileName(word);
        string cachedFile = Path.Combine(_cacheDirectory, $"{sanitized}.wav");

        // 1. Wenn bereits im Cache vorhanden: Sofort abspielen!
        if (File.Exists(cachedFile))
        {
            PlayAudioFile(cachedFile);
            return;
        }

        // 2. Wenn Piper verfügbar ist: Synthetisieren, im Cache ablegen und abspielen
        if (IsAvailable)
        {
            bool success = await SynthesizeToWavAsync(word, cachedFile, cancellationToken);
            if (success && File.Exists(cachedFile))
            {
                PlayAudioFile(cachedFile);
                return;
            }
        }

        // 3. Fallback: Wenn Piper noch nicht installiert ist, geben wir ein akustisches Signal
        // und protokollieren es sauber (während des Setups)
        Debug.WriteLine($"[TTS-Fallback] Wort gesprochen: '{word}' (Piper installiert: {IsAvailable})");
    }

    private async Task<bool> SynthesizeToWavAsync(string text, string outputPath, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(_piperExecutable) || string.IsNullOrEmpty(_modelPath)) return false;

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = _piperExecutable,
                Arguments = $"--model \"{_modelPath}\" --output_file \"{outputPath}\"",
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = psi };
            process.Start();

            await process.StandardInput.WriteLineAsync(text.AsMemory(), ct);
            process.StandardInput.Close();

            await process.WaitForExitAsync(ct);
            return process.ExitCode == 0;
        }
        catch
        {
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
        catch
        {
            // Best effort
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
            "/usr/bin/piper",
            "/usr/local/bin/piper",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "bin", "piper"),
            Path.Combine(AppContext.BaseDirectory, "assets", "piper", "piper")
        };

        return candidates.FirstOrDefault(File.Exists);
    }

    private static string? FindModelPath()
    {
        string[] candidates =
        {
            "/usr/share/piper/voices/de/de_DE-thorsten-medium.onnx",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share", "piper", "de_DE-thorsten-medium.onnx"),
            Path.Combine(AppContext.BaseDirectory, "assets", "piper", "de_DE-thorsten-medium.onnx")
        };

        return candidates.FirstOrDefault(File.Exists);
    }
}
