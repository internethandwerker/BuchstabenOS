using System.Text;
using BuchstabenOS.Domain.Events;
using BuchstabenOS.Domain.Services;

namespace BuchstabenOS.Domain.Model.Typing;

/// <summary>
/// Aggregate Root für die Schreib-Bühne im freien Tipp-Modus.
/// Verwaltet den Puffer, Zeilenumbruch mit schwebender Historie nach oben,
/// dynamische Schriftgrößen und feuert Domänen-Ereignisse.
/// </summary>
public class TextStage
{
    private readonly StringBuilder _currentBuffer = new();
    private readonly List<string> _completedLines = new();
    private readonly List<IDomainEvent> _uncommittedEvents = new();

    public FontScaleCalculator FontCalculator { get; }
    public WordDetector WordDetector { get; }

    public string CurrentText => _currentBuffer.ToString();
    public IReadOnlyList<string> CompletedLines => _completedLines.AsReadOnly();
    public FontSize CurrentFontSize { get; private set; } = FontSize.Max;
    public SpeechMode SpeechMode { get; set; } = SpeechMode.Phonetic;

    public IReadOnlyList<IDomainEvent> UncommittedEvents => _uncommittedEvents.AsReadOnly();

    public TextStage(FontScaleCalculator fontCalculator, WordDetector wordDetector)
    {
        FontCalculator = fontCalculator ?? throw new ArgumentNullException(nameof(fontCalculator));
        WordDetector = wordDetector ?? throw new ArgumentNullException(nameof(wordDetector));
    }

    /// <summary>
    /// Verarbeitet das Tippen eines Zeichens.
    /// </summary>
    public void TypeCharacter(char character, double viewportWidth, double viewportHeight)
    {
        if (char.IsControl(character)) return;

        char displayChar = char.ToUpperInvariant(character);
        _currentBuffer.Append(displayChar);

        // Schriftgröße für die aktuelle Zeile neu berechnen (schiebt bei Überlauf nach oben)
        RecalculateFontSize(viewportWidth, viewportHeight);

        // 1. Event: Buchstabe getippt (für sofortigen Laut/Klang)
        _uncommittedEvents.Add(new LetterTypedEvent(displayChar, char.IsLetter(displayChar)));

        // 2. Event: Wort-Prüfung auf das aktuelle Wort-Token
        string currentWord = GetCurrentWordToken();
        if (currentWord.Length >= 2 && WordDetector.IsExactWord(currentWord))
        {
            _uncommittedEvents.Add(new WordRecognizedEvent(currentWord, true));
        }
    }

    /// <summary>
    /// Verarbeitet den Druck der Leertaste: Schließt das aktuelle Wort ab
    /// und löst die ganzheitliche Aussprache aus.
    /// </summary>
    public void PressSpace(double viewportWidth, double viewportHeight)
    {
        string currentWord = GetCurrentWordToken();
        if (!string.IsNullOrWhiteSpace(currentWord))
        {
            _uncommittedEvents.Add(new SpacePressedEvent(currentWord));
        }

        _currentBuffer.Append(' ');
        RecalculateFontSize(viewportWidth, viewportHeight);
    }

    /// <summary>
    /// Verarbeitet die Eingabetaste (Enter): Schließt die Zeile ab,
    /// schiebt sie nach oben in die verblassende Historie und startet im Zentrum neu.
    /// </summary>
    public void CommitLine()
    {
        if (_currentBuffer.Length > 0)
        {
            string currentWord = GetCurrentWordToken();
            if (!string.IsNullOrWhiteSpace(currentWord))
            {
                _uncommittedEvents.Add(new SpacePressedEvent(currentWord));
            }

            _completedLines.Add(_currentBuffer.ToString().Trim());
            _currentBuffer.Clear();
            CurrentFontSize = FontSize.Max;

            while (_completedLines.Count > 4)
            {
                _completedLines.RemoveAt(0);
            }
        }
    }

    /// <summary>
    /// Verarbeitet die Rücktaste (Backspace / Löschen).
    /// </summary>
    public void PressBackspace(double viewportWidth, double viewportHeight)
    {
        if (_currentBuffer.Length > 0)
        {
            _currentBuffer.Remove(_currentBuffer.Length - 1, 1);
            RecalculateFontSize(viewportWidth, viewportHeight);
        }
        else if (_completedLines.Count > 0)
        {
            // Vorherige Zeile von oben wieder zurückholen
            string lastLine = _completedLines[^1];
            _completedLines.RemoveAt(_completedLines.Count - 1);
            _currentBuffer.Append(lastLine);
            RecalculateFontSize(viewportWidth, viewportHeight);
        }
    }

    /// <summary>
    /// Setzt die gesamte Schreib-Bühne zurück.
    /// </summary>
    public void Clear()
    {
        _currentBuffer.Clear();
        _completedLines.Clear();
        CurrentFontSize = FontSize.Max;
        _uncommittedEvents.Add(new StageResetEvent());
    }

    /// <summary>
    /// Leert die Liste der noch nicht verarbeiteten Domänen-Ereignisse.
    /// </summary>
    public IReadOnlyList<IDomainEvent> DequeueEvents()
    {
        var events = _uncommittedEvents.ToList();
        _uncommittedEvents.Clear();
        return events;
    }

    private void RecalculateFontSize(double viewportWidth, double viewportHeight)
    {
        int length = _currentBuffer.Length;

        if (length == 0)
        {
            CurrentFontSize = FontSize.Max;
            return;
        }

        var newSize = FontCalculator.Calculate(length, viewportWidth, viewportHeight, out bool requiresWrap);

        if (requiresWrap)
        {
            WrapCurrentLine();
            CurrentFontSize = FontCalculator.Calculate(_currentBuffer.Length, viewportWidth, viewportHeight, out _);
        }
        else
        {
            CurrentFontSize = newSize;
        }
    }

    private void WrapCurrentLine()
    {
        string text = _currentBuffer.ToString();
        int lastSpace = text.LastIndexOf(' ');

        if (lastSpace > 0 && lastSpace < text.Length - 1)
        {
            string lineToSave = text[..lastSpace].Trim();
            string remaining = text[(lastSpace + 1)..];

            _completedLines.Add(lineToSave);
            _currentBuffer.Clear();
            _currentBuffer.Append(remaining);
        }
        else
        {
            // Harter Umbruch
            if (text.Length > 1)
            {
                string lineToSave = text[..^1].Trim();
                char lastChar = text[^1];
                _completedLines.Add(lineToSave);
                _currentBuffer.Clear();
                _currentBuffer.Append(lastChar);
            }
            else
            {
                _completedLines.Add(text);
                _currentBuffer.Clear();
            }
        }

        while (_completedLines.Count > 4)
        {
            _completedLines.RemoveAt(0);
        }
    }

    private string GetCurrentWordToken()
    {
        string text = _currentBuffer.ToString();
        int lastSpace = text.LastIndexOf(' ');
        return lastSpace >= 0 ? text[(lastSpace + 1)..].Trim() : text.Trim();
    }
}
