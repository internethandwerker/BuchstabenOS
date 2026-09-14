using System.Diagnostics;
using BuchstabenOS.Application.Ports;
using BuchstabenOS.Domain.Model.Typing;

namespace BuchstabenOS.Infrastructure.Audio;

/// <summary>
/// Spielt vorgerenderte WAV-Dateien für Buchstaben und Jingles unter Linux mit minimaler Latenz ab.
/// Nutzt bevorzugt 'paplay' (PulseAudio/PipeWire) oder 'aplay' (ALSA).
/// </summary>
public class LinuxAudioSamplePlayer : IAudioPlayer
{
    private readonly string _assetsDirectory;
    private readonly string? _audioPlayerBinary;

    public LinuxAudioSamplePlayer(string? customAssetsPath = null)
    {
        _assetsDirectory = customAssetsPath ?? FindAssetsDirectory();
        _audioPlayerBinary = FindAudioPlayerBinary();
    }

    public ValueTask PlayLetterAsync(char letter, SpeechMode mode, CancellationToken cancellationToken = default)
    {
        char upper = char.ToUpperInvariant(letter);
        string modeFolder = mode == SpeechMode.Phonetic ? "laute" : "alphabet";
        string samplePath = Path.Combine(_assetsDirectory, "audio", modeFolder, $"{upper}.wav");

        if (File.Exists(samplePath))
        {
            PlayWavFileFireAndForget(samplePath);
        }
        else
        {
            // Fallback: Wenn für diesen Buchstaben noch keine WAV existiert,
            // spiele einen harmonischen Feedback-Klang oder erzeuge Beep
            PlayFallbackTone(upper);
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask PlayJingleAsync(string jingleName, CancellationToken cancellationToken = default)
    {
        string jinglePath = Path.Combine(_assetsDirectory, "audio", "jingles", $"{jingleName}.wav");
        if (File.Exists(jinglePath))
        {
            PlayWavFileFireAndForget(jinglePath);
        }

        return ValueTask.CompletedTask;
    }

    private void PlayWavFileFireAndForget(string filePath)
    {
        if (string.IsNullOrEmpty(_audioPlayerBinary)) return;

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = _audioPlayerBinary,
                Arguments = $"\"{filePath}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = false,
                RedirectStandardError = false
            };

            Process.Start(psi);
        }
        catch
        {
            // Ignorieren, damit die UI niemals blockiert oder abstürzt
        }
    }

    private void PlayFallbackTone(char character)
    {
        // Erzeuge eine kurze, unaufdringliche Sinus-WAV im Temp-Verzeichnis
        try
        {
            string tempWav = Path.Combine(Path.GetTempPath(), $"buchstabenos_tone_{(int)character}.wav");
            if (!File.Exists(tempWav))
            {
                GenerateSimpleWavTone(tempWav, 220 + ((character % 26) * 15), 120);
            }
            PlayWavFileFireAndForget(tempWav);
        }
        catch
        {
            // Best-effort
        }
    }

    private static void GenerateSimpleWavTone(string filePath, double frequency, int durationMs)
    {
        int sampleRate = 22050;
        int sampleCount = (int)(sampleRate * (durationMs / 1000.0));
        short[] samples = new short[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            double t = (double)i / sampleRate;
            // Sanftes Ein- und Ausblenden (Envelope), um Klicken zu verhindern
            double envelope = Math.Sin(Math.PI * i / sampleCount);
            double value = Math.Sin(2 * Math.PI * frequency * t) * envelope;
            samples[i] = (short)(value * short.MaxValue * 0.4);
        }

        using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        using var writer = new BinaryWriter(stream);

        // WAV Header
        writer.Write("RIFF"u8);
        writer.Write(36 + samples.Length * 2);
        writer.Write("WAVE"u8);
        writer.Write("fmt "u8);
        writer.Write(16); // Chunk-Größe
        writer.Write((short)1); // PCM
        writer.Write((short)1); // 1 Kanal (Mono)
        writer.Write(sampleRate);
        writer.Write(sampleRate * 2); // Byte-Rate
        writer.Write((short)2); // Block Align
        writer.Write((short)16); // Bits per Sample
        writer.Write("data"u8);
        writer.Write(samples.Length * 2);

        foreach (var sample in samples)
        {
            writer.Write(sample);
        }
    }

    private static string FindAssetsDirectory()
    {
        string baseDir = AppContext.BaseDirectory;
        string candidate = Path.Combine(baseDir, "assets");
        if (Directory.Exists(candidate)) return candidate;

        // Dev-Umgebung
        string projectDir = Directory.GetCurrentDirectory();
        candidate = Path.Combine(projectDir, "assets");
        if (Directory.Exists(candidate)) return candidate;

        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share", "buchstabenos", "assets");
    }

    private static string? FindAudioPlayerBinary()
    {
        if (File.Exists("/usr/bin/paplay")) return "/usr/bin/paplay";
        if (File.Exists("/usr/bin/aplay")) return "/usr/bin/aplay";
        return null;
    }
}
