using BuchstabenOS.Application.Configuration;
using BuchstabenOS.Domain.Events;
using BuchstabenOS.Domain.Model.Games;

namespace BuchstabenOS.Application.UseCases;

/// <summary>
/// Schnittstelle für die Moderation und den Spielablauf.
/// Erkennt das Erreichen von Aufmerksamkeitsspannen und entscheidet,
/// welches Spiel als Nächstes gestartet werden soll.
/// Bereit für zukünftige Erweiterung durch einen KI-Tutor (LLM-Agenten).
/// </summary>
public interface IGameModerator
{
    /// <summary>Gibt an, ob automatischer Spielewechsel aktiv ist.</summary>
    bool IsAutoSwitchEnabled { get; set; }

    /// <summary>Wird aufgerufen, wenn ein Spiel ein Fortschrittsereignis ausgelöst hat.</summary>
    /// <returns>True, wenn ein Spielwechsel ausgelöst werden soll.</returns>
    bool RecordRoundCompleted(string gameId, int completedRounds, TimeSpan elapsedPlayTime);

    /// <summary>Wählt anhand von Gewichten das nächste Spiel aus.</summary>
    string SelectNextGame(string currentGameId, IReadOnlyList<IGameModule> availableGames);

    /// <summary>Liefert eine kindgerechte Vorlese-Begrüßung beim Wechsel zu einem Spiel.</summary>
    string GetGameIntroPrompt(string nextGameId);

    /// <summary>Aktualisiert die Einstellungen des Moderators.</summary>
    void UpdateSettings(AppSettings settings);
}
