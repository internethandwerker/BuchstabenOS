using System.Diagnostics;
using BuchstabenOS.Application.Ports;
using BuchstabenOS.Domain.Model.Typing;
using Serilog;

namespace BuchstabenOS.Infrastructure.Audio;

/// <summary>
/// Polyphoner Multitrack-Audioplayer für Buchstaben, Phoneme und Soundeffekte.
/// Ermöglicht wildes Tippen von Kindern, indem Töne parallel auf mehreren Spuren
/// gemischt werden (PulseAudio/PipeWire), ohne sich gegenseitig abzuschneiden.
/// </summary>
public class LinuxAudioSamplePlayer : IAudioPlayer
{
    private readonly string _assetsDirectory;
    private readonly string? _audioPlayerBinary;

    public LinuxAudioSamplePlayer(string? customAssetsPath = null)
    {
        _assetsDirectory = customAssetsPath ?? FindAssetsDirectory();
        _audioPlayerBinary = FindAudioPlayerBinary();
        Log.Information("LinuxAudioSamplePlayer initialisiert. Assets: {Assets}, Player: {Player}",
            _assetsDirectory, _audioPlayerBinary ?? "keiner");
    }

    public ValueTask PlayLetterAsync(char letter, SpeechMode mode, CancellationToken cancellationToken = default)
    {
        char upper = char.ToUpperInvariant(letter);
        string modeFolder = mode == SpeechMode.Phonetic ? "laute" : "alphabet";
        string samplePath = Path.Combine(_assetsDirectory, "audio", modeFolder, $"{upper}.wav");

        if (File.Exists(samplePath))
        {
            PlayTrackFireAndForget(samplePath);
        }
        else
        {
            Log.Warning("Audiodatei für '{Letter}' ({Mode}) nicht gefunden unter {Path}", upper, mode, samplePath);
            PlayFallbackTone(upper);
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask PlayJingleAsync(string jingleName, CancellationToken cancellationToken = default)
    {
        string jinglePath = Path.Combine(_assetsDirectory, "audio", "jingles", $"{jingleName}.wav");
        if (File.Exists(jinglePath))
        {
            PlayTrackFireAndForget(jinglePath);
        }

        return ValueTask.CompletedTask;
    }

    private void PlayTrackFireAndForget(string filePath)
    {
        if (string.IsNullOrEmpty(_audioPlayerBinary)) return;

        try
        {
            // Jeder Tastendruck startet einen eigenen, nicht-blockierenden Stream.
            // PulseAudio / PipeWire mischt diese polyphon zusammen (Multitrack).
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
        catch (Exception ex)
        {
            Log.Error(ex, "Fehler beim Abspielen von Track: {Path}", filePath);
        }
    }

    private void PlayFallbackTone(char character)
    {
        try
        {
            string tempWav = Path.Combine(Path.GetTempPath(), $"buchstabenos_tone_{(int)character}.wav");
            if (!File.Exists(tempWav))
            {
                GenerateSimpleWavTone(tempWav, 220 + ((character % 26) * 15), 120);
            }
            PlayTrackFireAndForget(tempWav);
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
            double envelope = Math.Sin(Math.PI * i / sampleCount);
            double value = Math.Sin(2 * Math.PI * frequency * t) * envelope;
            samples[i] = (short)(value * short.MaxValue * 0.4);
        }

        using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        using var writer = new BinaryWriter(stream);

        writer.Write("RIFF"u8);
        writer.Write(36 + samples.Length * 2);
        writer.Write("WAVE"u8);
        writer.Write("fmt "u8);
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)1);
        writer.Write(sampleRate);
        writer.Write(sampleRate * 2);
        writer.Write((short)2);
        writer.Write((short)16);
        writer.Write("data"u8);
        writer.Write(samples.Length * 2);

        foreach (var sample in samples)
        {
            writer.Write(sample);
        }
    }

    private static string FindAssetsDirectory()
    {
        // 1. AppContext BaseDirectory
        string baseDir = AppContext.BaseDirectory;
        string candidate = Path.Combine(baseDir, "assets");
        if (Directory.Exists(candidate)) return candidate;

        // 2. Suche in Elternverzeichnissen von CurrentDirectory aus (für dotnet run)
        string cur = Directory.GetCurrentDirectory();
        for (int i = 0; i < 4; i++)
        {
            candidate = Path.Combine(cur, "assets");
            if (Directory.Exists(candidate)) return candidate;
            string? parent = Directory.GetParent(cur)?.FullName;
            if (parent == null) break;
            cur = parent;
        }

        // 3. Linux Benutzer- und Systemverzeichnisse
        string userShare = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share", "buchstabenos", "assets");
        if (Directory.Exists(userShare)) return userShare;

        string optShare = "/opt/buchstabenos/assets";
        if (Directory.Exists(optShare)) return optShare;

        return candidate;
    }

    private static string? FindAudioPlayerBinary()
    {
        if (File.Exists("/usr/bin/pw-play")) return "/usr/bin/pw-play";
        if (File.Exists("/usr/bin/paplay")) return "/usr/bin/paplay";
        if (File.Exists("/usr/bin/aplay")) return "/usr/bin/aplay";
        return null;
    }
}
