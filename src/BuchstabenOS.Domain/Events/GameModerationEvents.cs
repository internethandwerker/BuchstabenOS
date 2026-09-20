namespace BuchstabenOS.Domain.Events;

/// <summary>
/// Wird von einem Spielmodul ausgelöst, wenn eine Spielrunde abgeschlossen wurde
/// (z. B. 1 gelöste Matheaufgabe oder 1 erkanntes Wort).
/// Dient dem Moderator zur Fortschrittserfassung und Aufmerksamkeitsprüfung.
/// </summary>
public record GameRoundCompletedEvent(
    string GameId,
    int TotalCompletedRounds,
    DateTime OccurredOn
) : IDomainEvent
{
    public GameRoundCompletedEvent(string gameId, int completedRounds)
        : this(gameId, completedRounds, DateTime.UtcNow) { }
}

/// <summary>
/// Signalisiert, dass ein Wechsel des aktiven Spiels ansteht.
/// </summary>
public record GameSwitchRequestedEvent(
    string CurrentGameId,
    string NextGameId,
    string AnnouncementPhrase,
    DateTime OccurredOn
) : IDomainEvent
{
    public GameSwitchRequestedEvent(string currentGameId, string nextGameId, string announcementPhrase)
        : this(currentGameId, nextGameId, announcementPhrase, DateTime.UtcNow) { }
}
