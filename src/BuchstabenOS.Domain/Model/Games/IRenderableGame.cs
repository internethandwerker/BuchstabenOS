namespace BuchstabenOS.Domain.Model.Games;

/// <summary>
/// Schnittstelle für Spielmodule, die ihren visuellen Zustand auf der
/// Hauptbühne (Bildschirmmitte, schwebende Historie, Feedback-Banner) präsentieren.
/// Ermöglicht MainWindowViewModel die vollständige Entkopplung von konkreten Spielen.
/// </summary>
public interface IRenderableGame
{
    /// <summary>Der primäre, zentrierte Text auf der Bühne (z. B. "3 + 2 = 5" oder "MAMA").</summary>
    string DisplayText { get; }

    /// <summary>Die berechnete dynamische Schriftgröße in Punkten.</summary>
    double CurrentFontSizePoints { get; }

    /// <summary>Ein sanfter Hinweistext, wenn noch nichts eingegeben wurde.</summary>
    string HintText { get; }

    /// <summary>Abgeschlossene Zeilen/Aufgaben, die nach oben schweben und verblassen.</summary>
    IReadOnlyList<string> CompletedLines { get; }

    /// <summary>Gibt an, ob gerade eine Erfolgs-Feier aktiv ist.</summary>
    bool IsCelebrating { get; }

    /// <summary>Die Nachricht für das Feier-Banner (z. B. "Wort gezaubert: MAMA!" oder "Richtig gerechnet: 3 + 2 = 5!").</summary>
    string CelebrationMessage { get; }

    /// <summary>Gibt an, ob gerade ein Falsch-Feedback (rotes Aufleuchten/Schütteln) aktiv ist.</summary>
    bool IsWrongFeedback { get; }

    /// <summary>Wird ausgelöst, wenn sich der visuelle Zustand des Spiels geändert hat.</summary>
    event Action? ViewStateChanged;
}
