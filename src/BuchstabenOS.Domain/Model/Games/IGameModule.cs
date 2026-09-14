using BuchstabenOS.Domain.Events;

namespace BuchstabenOS.Domain.Model.Games;

/// <summary>
/// Wissens- und Fortschritts-Score eines Spiels.
/// Transparent für Eltern-Dashboards und KI-Agenten zur Ermittlung des Lernfortschritts.
/// </summary>
public record KnowledgeScore(
    int TotalInteractions,
    int SuccessfulWordsRecognized,
    int UniqueLettersExplored,
    int CurrentStreak,
    TimeSpan PlayTime,
    DateTime LastPlayedAt
)
{
    public static KnowledgeScore Empty => new(0, 0, 0, 0, TimeSpan.Zero, DateTime.UtcNow);
}

/// <summary>
/// Repräsentiert eine Eingabe an ein Spielmodul.
/// </summary>
public record GameInput(
    char KeyChar,
    bool IsSpace,
    bool IsBackspace,
    bool IsControlModified,
    bool IsAltModified,
    bool IsShiftModified,
    DateTime Timestamp
)
{
    public static GameInput FromChar(char c) => new(c, c == ' ', false, false, false, false, DateTime.UtcNow);
    public static GameInput Backspace => new('\0', false, true, false, false, false, DateTime.UtcNow);
    public static GameInput Space => new(' ', true, false, false, false, false, DateTime.UtcNow);
}

/// <summary>
/// Das Ergebnis der Eingabeverarbeitung durch ein Spiel.
/// </summary>
public record GameInputResult(
    bool WasHandled,
    IReadOnlyList<IDomainEvent> EmittedEvents,
    string? FeedbackMessage = null
)
{
    public static GameInputResult Unhandled => new(false, Array.Empty<IDomainEvent>());
    public static GameInputResult Handled(params IDomainEvent[] events) => new(true, events);
}

/// <summary>
/// Kontext, den das Spielsystem einem Spielmodul zur Verfügung stellt.
/// </summary>
public interface IGameContext
{
    string ActiveGameId { get; }
    void PublishEvent(IDomainEvent domainEvent);
}

/// <summary>
/// Kern-Schnittstelle für alle Spiele in BuchstabenOS.
/// Jedes Spiel (vom freien Tippen bis zu Mathe und Quiz) implementiert dieses Interface.
/// Ermöglicht Plugin-Architektur, Sequenzierung zur Laufzeit und agentengesteuerte Auswahl.
/// </summary>
public interface IGameModule
{
    /// <summary>Metadaten zur pädagogischen Einordnung und LLM-Interpretation.</summary>
    GameMetadata Metadata { get; }

    /// <summary>Gibt an, ob das Spiel gerade aktiv und spielbereit ist.</summary>
    bool IsActive { get; }

    /// <summary>Initialisiert das Spiel mit dem übergeordneten Spielkontext.</summary>
    Task InitializeAsync(IGameContext context, CancellationToken cancellationToken = default);

    /// <summary>Verarbeitet eine Benutzereingabe (z. B. Tastendruck).</summary>
    ValueTask<GameInputResult> ProcessInputAsync(GameInput input);

    /// <summary>Liefert den aktuellen Wissens- und Fortschritts-Score.</summary>
    KnowledgeScore GetScore();

    /// <summary>Setzt den Zustand des Spiels auf Anfang zurück.</summary>
    void Reset();
}
