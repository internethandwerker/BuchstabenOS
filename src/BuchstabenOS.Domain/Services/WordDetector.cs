namespace BuchstabenOS.Domain.Services;

/// <summary>
/// Knotenpunkt für den Trie-Präfixbaum zur O(k) Worterkennung.
/// </summary>
public class TrieNode
{
    public Dictionary<char, TrieNode> Children { get; } = new();
    public bool IsEndOfWord { get; set; }
    public string? Word { get; set; }
}

/// <summary>
/// Erkennt in O(k) Zeit, ob eine getippte Zeichenfolge ein gültiges deutsches Wort ist.
/// </summary>
public class WordDetector
{
    private readonly TrieNode _root = new();
    private readonly HashSet<string> _knownWords = new(StringComparer.OrdinalIgnoreCase);

    public int WordCount => _knownWords.Count;
    public IReadOnlyCollection<string> KnownWords => _knownWords;

    /// <summary>
    /// Liefert ein zufälliges Wort aus dem Wörterbuch (z. B. für Wortvorlagen).
    /// </summary>
    public string? GetRandomWord(Random? random = null)
    {
        if (_knownWords.Count == 0) return null;
        var list = _knownWords.ToList();
        var rnd = random ?? Random.Shared;
        return list[rnd.Next(list.Count)];
    }

    public WordDetector(IEnumerable<string>? initialWords = null)
    {
        if (initialWords != null)
        {
            foreach (var word in initialWords)
            {
                AddWord(word);
            }
        }
    }

    /// <summary>
    /// Lädt eine neue Liste von Wörtern und initialisiert den Suchbaum vollständig neu.
    /// </summary>
    public void LoadWords(IEnumerable<string> words)
    {
        _knownWords.Clear();
        _root.Children.Clear();
        _root.IsEndOfWord = false;
        _root.Word = null;

        if (words != null)
        {
            foreach (var word in words)
            {
                AddWord(word);
            }
        }
    }

    /// <summary>
    /// Entfernt ein Wort aus dem Wörterbuch und baut den Präfixbaum neu auf.
    /// </summary>
    public bool RemoveWord(string word)
    {
        if (string.IsNullOrWhiteSpace(word)) return false;

        string normalized = word.Trim().ToUpperInvariant();
        if (!_knownWords.Remove(normalized)) return false;

        // Trie neu aufbauen
        _root.Children.Clear();
        _root.IsEndOfWord = false;
        _root.Word = null;

        foreach (var w in _knownWords)
        {
            var current = _root;
            foreach (char c in w)
            {
                if (!current.Children.TryGetValue(c, out var nextNode))
                {
                    nextNode = new TrieNode();
                    current.Children[c] = nextNode;
                }
                current = nextNode;
            }
            current.IsEndOfWord = true;
            current.Word = w;
        }

        return true;
    }

    /// <summary>
    /// Fügt ein Wort dem Wörterbuch hinzu.
    /// </summary>
    public bool AddWord(string word)
    {
        if (string.IsNullOrWhiteSpace(word)) return false;

        string normalized = word.Trim().ToUpperInvariant();
        if (_knownWords.Contains(normalized)) return false;

        _knownWords.Add(normalized);

        var current = _root;
        foreach (char c in normalized)
        {
            if (!current.Children.TryGetValue(c, out var nextNode))
            {
                nextNode = new TrieNode();
                current.Children[c] = nextNode;
            }
            current = nextNode;
        }

        current.IsEndOfWord = true;
        current.Word = normalized;
        return true;
    }

    /// <summary>
    /// Prüft, ob das exakte Wort im Wörterbuch existiert.
    /// </summary>
    public bool IsExactWord(string word)
    {
        if (string.IsNullOrWhiteSpace(word)) return false;
        return _knownWords.Contains(word.Trim().ToUpperInvariant());
    }

    /// <summary>
    /// Prüft, ob der übergebene Puffer mit einem bekannten Wort endet.
    /// Beispiel: Wenn im Puffer "HALLOMAMA" steht, kann geprüft werden,
    /// ob die letzten Zeichen ein Wort wie "MAMA" bilden.
    /// </summary>
    public bool TryFindWordAtEnd(string buffer, out string? matchedWord)
    {
        matchedWord = null;
        if (string.IsNullOrWhiteSpace(buffer)) return false;

        string normalized = buffer.Trim().ToUpperInvariant();

        // Prüfe von längsten Suffixen zu kürzesten
        for (int i = 0; i < normalized.Length; i++)
        {
            string suffix = normalized[i..];
            if (_knownWords.Contains(suffix))
            {
                matchedWord = suffix;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Prüft, ob für den angegebenen Präfix Wörter existieren.
    /// </summary>
    public bool IsPrefix(string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix)) return false;

        var current = _root;
        foreach (char c in prefix.Trim().ToUpperInvariant())
        {
            if (!current.Children.TryGetValue(c, out var nextNode))
            {
                return false;
            }
            current = nextNode;
        }
        return true;
    }
}
