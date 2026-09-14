using BuchstabenOS.Domain.Model.Security;
using BuchstabenOS.Domain.Model.Typing;

namespace BuchstabenOS.Application.Configuration;

/// <summary>
/// Globale Konfiguration für BuchstabenOS.
/// </summary>
public class AppSettings
{
    public string ActiveGameId { get; set; } = "free-typing";
    public SpeechMode SpeechMode { get; set; } = SpeechMode.Phonetic;
    public int VolumePercent { get; set; } = 80;
    public string ParentPinHash { get; set; } = new ParentPin("1337").GetHashCode().ToString();
    public bool AutoShutdownOnInactivity { get; set; } = false;
    public int InactivityMinutes { get; set; } = 15;
}
