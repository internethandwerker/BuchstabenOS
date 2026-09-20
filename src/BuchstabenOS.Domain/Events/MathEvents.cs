namespace BuchstabenOS.Domain.Events;

/// <summary>
/// Wird ausgelöst, wenn eine neue Mathe-Aufgabe generiert wurde.
/// Enthält auch den Vorlesetext für die TTS-Stimme.
/// </summary>
public record MathTaskGeneratedEvent(
    string TaskText,
    int OperandA,
    int OperandB,
    int ExpectedResult,
    string SpokenPrompt,
    DateTime OccurredOn
) : IDomainEvent
{
    public MathTaskGeneratedEvent(string taskText, int a, int b, int result, string prompt)
        : this(taskText, a, b, result, prompt, DateTime.UtcNow) { }
}

/// <summary>
/// Wird ausgelöst, wenn eine Matheaufgabe erfolgreich gelöst wurde.
/// </summary>
public record MathTaskSolvedEvent(
    string TaskText,
    int Result,
    int Attempts,
    string SpokenPraise,
    DateTime OccurredOn
) : IDomainEvent
{
    public MathTaskSolvedEvent(string taskText, int result, int attempts, string praise)
        : this(taskText, result, attempts, praise, DateTime.UtcNow) { }
}

/// <summary>
/// Wird ausgelöst, wenn eine falsche Antwort eingegeben wurde.
/// </summary>
public record MathTaskFailedEvent(
    string TaskText,
    string WrongAnswer,
    int ExpectedResult,
    string SpokenEncouragement,
    DateTime OccurredOn
) : IDomainEvent
{
    public MathTaskFailedEvent(string taskText, string wrongAnswer, int expectedResult, string encouragement)
        : this(taskText, wrongAnswer, expectedResult, encouragement, DateTime.UtcNow) { }
}
