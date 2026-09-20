using System.Diagnostics;
using BuchstabenOS.Application.Ports;

namespace BuchstabenOS.Infrastructure.System;

/// <summary>
/// Führt Linux-spezifische Betriebssystem-Aktionen auf BunsenLabs aus.
/// </summary>
public class LinuxSystemControl : ISystemControl
{
    private int _lastKnownVolume = 80;

    public void ExitToDesktop()
    {
        // Exit-Code 42 signalisiert dem Kiosk-Skript (buchstabenos-session.sh),
        // die Kiosk-Schleife zu beenden und den Desktop für die Eltern freizugeben.
        Environment.Exit(42);
    }

    public void RestartApp()
    {
        string? currentProcess = Environment.ProcessPath;
        if (!string.IsNullOrEmpty(currentProcess) && File.Exists(currentProcess))
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = currentProcess,
                    UseShellExecute = true
                });
            }
            catch
            {
                // Fallback: Im Kiosk-Modus startet buchstabenos-session.sh die App bei Exit-Code 0 sowieso neu.
            }
        }
        Environment.Exit(0);
    }

    public void PowerOff()
    {
        RunCommand("systemctl", "poweroff");
    }

    public void Reboot()
    {
        RunCommand("systemctl", "reboot");
    }

    public void SetSystemVolume(int percent)
    {
        _lastKnownVolume = Math.Clamp(percent, 0, 100);

        // Versuche PipeWire (wpctl)
        if (File.Exists("/usr/bin/wpctl"))
        {
            double frac = _lastKnownVolume / 100.0;
            RunCommand("wpctl", $"set-volume @DEFAULT_AUDIO_SINK@ {frac:F2}");
        }
        // Fallback ALSA (amixer)
        else if (File.Exists("/usr/bin/amixer"))
        {
            RunCommand("amixer", $"-q sset Master {_lastKnownVolume}%");
        }
    }

    public int GetSystemVolume() => _lastKnownVolume;

    private static void RunCommand(string binary, string args)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = binary,
                Arguments = args,
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
}
