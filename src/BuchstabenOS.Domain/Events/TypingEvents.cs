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
