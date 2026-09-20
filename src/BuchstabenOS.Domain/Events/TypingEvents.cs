namespace BuchstabenOS.Domain.Events;

/// <summary>
/// Wird ausgelöst, wenn ein Buchstabe oder Zeichen getippt wurde.
/// </summary>
/// <param name="Character">Das getippte Zeichen (z. B. 'A', 'M', '7').</param>
/// <param name="IsLetter">Gibt an, ob es sich um einen Buchstaben handelt.</param>
/// <param name="OccurredOn">Zeitpunkt des Tastendrucks.</param>
public record LetterTypedEvent(char Character, bool IsLetter, DateTime OccurredOn) : IDomainEvent
{
    public LetterTypedEvent(char character, bool isLetter) : this(character, isLetter, DateTime.UtcNow) { }
}

/// <summary>
/// Wird ausgelöst, wenn ein gültiges Wort aus dem Wörterbuch erkannt wurde.
/// </summary>
/// <param name="Word">Das erkannte Wort (z. B. "MAMA", "AUTO").</param>
/// <param name="IsDictionaryMatch">True, wenn es im Wörterbuch existiert.</param>
/// <param name="OccurredOn">Zeitpunkt der Erkennung.</param>
public record WordRecognizedEvent(string Word, bool IsDictionaryMatch, DateTime OccurredOn) : IDomainEvent
{
    public WordRecognizedEvent(string word, bool isDictionaryMatch) : this(word, isDictionaryMatch, DateTime.UtcNow) { }
}

/// <summary>
/// Wird ausgelöst, wenn die Leertaste gedrückt wurde, um ein (ggf. Quatsch-)Wort vorzulesen und abzuschließen.
/// </summary>
/// <param name="RawWord">Der aktuell getippte Text, der vorgelesen werden soll.</param>
/// <param name="OccurredOn">Zeitpunkt des Drückens.</param>
public record SpacePressedEvent(string RawWord, DateTime OccurredOn) : IDomainEvent
{
    public SpacePressedEvent(string rawWord) : this(rawWord, DateTime.UtcNow) { }
}

/// <summary>
/// Wird ausgelöst, wenn der Bildschirm geleert oder zurückgesetzt wird.
/// </summary>
public record StageResetEvent(DateTime OccurredOn) : IDomainEvent
{
    public StageResetEvent() : this(DateTime.UtcNow) { }
}

/// <summary>
/// Wird ausgelöst, wenn die geheime Eltern-Tastenkombination gedrückt wurde.
/// </summary>
public record ParentMenuTriggeredEvent(DateTime OccurredOn) : IDomainEvent
{
    public ParentMenuTriggeredEvent() : this(DateTime.UtcNow) { }
}

/// <summary>
/// Wird ausgelöst, wenn eine interaktive Wortvorlage (Lern-Impuls) auf der Bühne erscheint.
/// </summary>
public record WordTemplateChallengeStartedEvent(string TargetWord, string SpokenPrompt, DateTime OccurredOn) : IDomainEvent
{
    public WordTemplateChallengeStartedEvent(string targetWord, string spokenPrompt) : this(targetWord, spokenPrompt, DateTime.UtcNow) { }
}

/// <summary>
/// Wird ausgelöst, wenn ein Buchstabe der Wortvorlage korrekt eingetippt wurde.
/// </summary>
public record WordTemplateLetterMatchedEvent(string TargetWord, int MatchedIndex, char Letter, DateTime OccurredOn) : IDomainEvent
{
    public WordTemplateLetterMatchedEvent(string targetWord, int matchedIndex, char letter) : this(targetWord, matchedIndex, letter, DateTime.UtcNow) { }
}

/// <summary>
/// Wird ausgelöst, wenn eine Taste gedrückt wurde, die nicht zum nächsten Buchstaben der Vorlage passt.
/// </summary>
public record WordTemplateMistakeEvent(string TargetWord, int ExpectedIndex, char ActualLetter, DateTime OccurredOn) : IDomainEvent
{
    public WordTemplateMistakeEvent(string targetWord, int expectedIndex, char actualLetter) : this(targetWord, expectedIndex, actualLetter, DateTime.UtcNow) { }
}

/// <summary>
/// Wird ausgelöst, wenn alle Buchstaben der Wortvorlage erfolgreich abgetippt wurden.
/// </summary>
public record WordTemplateChallengeCompletedEvent(string TargetWord, DateTime OccurredOn) : IDomainEvent
{
    public WordTemplateChallengeCompletedEvent(string targetWord) : this(targetWord, DateTime.UtcNow) { }
}

/// <summary>
/// Wird ausgelöst, wenn das Kind die Wortvorlage per Leertaste oder Timeout überspringt.
/// </summary>
public record WordTemplateChallengeSkippedEvent(string TargetWord, DateTime OccurredOn) : IDomainEvent
{
    public WordTemplateChallengeSkippedEvent(string targetWord) : this(targetWord, DateTime.UtcNow) { }
}

