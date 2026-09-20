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

    // Spiele-Moderation & Gewichte
    public bool AutoGameSwitching { get; set; } = true;
    public Dictionary<string, int> GameWeights { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["free-typing"] = 50,
        ["math-addition"] = 50
    };

    // Aufmerksamkeitsspannen
    public int MathAttentionSpan { get; set; } = 2; // Default: 2 gelöste Aufgaben
    public int TypingAttentionSpan { get; set; } = 5; // Default: 5 erkannte Wörter

    // Spezifische Spiele-Konfigurationen
    public int AdditionMaxSum { get; set; } = 10; // Default: Rechnen bis 10
}
