namespace BuchstabenOS.Domain.Model.Typing;

/// <summary>
/// Pädagogischer Sprachmodus für die Einzellaut-Aussprache.
/// </summary>
public enum SpeechMode
{
    /// <summary>
    /// Lautieren (Standard für Vorschulkinder):
    /// Aussprache der reinen Sprachlaute/Phoneme ('Mmm', 'B', 'P', 'A'),
    /// damit das Kind Laute direkt zu Wörtern zusammenziehen kann.
    /// </summary>
    Phonetic,

    /// <summary>
    /// Alphabet-Namen (Klassisches ABC-Lied):
    /// Aussprache der Buchstabennamen ('Em', 'Be', 'Pe', 'Ah').
    /// </summary>
    Alphabet
}

/// <summary>
/// Kapselt die typografische Schriftgröße für den Bildschirm.
/// </summary>
public readonly record struct FontSize
{
    public const double DefaultMin = 48.0;
    public const double DefaultMax = 240.0;

    public double Points { get; }

    public FontSize(double points)
    {
        Points = Math.Max(DefaultMin, Math.Min(DefaultMax, points));
    }

    public static FontSize Min => new(DefaultMin);
    public static FontSize Max => new(DefaultMax);

    public bool IsAtMinimum => Points <= DefaultMin + 0.001;
    public bool IsAtMaximum => Points >= DefaultMax - 0.001;

    public override string ToString() => $"{Points:F0}pt";
}

/// <summary>
/// Kapselt ein getipptes oder erkanntes Wort.
/// </summary>
public record Word(string Text, bool IsDictionaryWord)
{
    public string NormalizedText => Text.Trim().ToUpperInvariant();
    public int Length => Text.Length;

    public override string ToString() => Text;
}

/// <summary>
/// Repräsentiert einen einzelnen Tastaturanschlag mit Metadaten.
/// </summary>
public record Keystroke(
    char Character,
    bool IsLetter,
    bool IsDigit,
    bool IsSpace,
    bool IsControlModified = false,
    bool IsAltModified = false,
    bool IsShiftModified = false
)
{
    public static Keystroke FromChar(char c)
    {
        return new Keystroke(
            c,
            char.IsLetter(c),
            char.IsDigit(c),
            c == ' '
        );
    }
}
