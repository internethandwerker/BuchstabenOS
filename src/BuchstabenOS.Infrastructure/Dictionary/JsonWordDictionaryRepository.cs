using System.Text.Json;
using BuchstabenOS.Application.Ports;

namespace BuchstabenOS.Infrastructure.Dictionary;

/// <summary>
/// Persistiert Wörterlisten im JSON-Format, strikt getrennt PRO SPIEL.
/// Gespeichert unter ~/.config/buchstabenos/dictionaries/{gameId}.json
/// </summary>
public class JsonWordDictionaryRepository : IWordDictionaryRepository
{
    private readonly string _storageDirectory;

    public JsonWordDictionaryRepository(string? customStorageDir = null)
    {
        _storageDirectory = customStorageDir ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".config", "buchstabenos", "dictionaries"
        );

        Directory.CreateDirectory(_storageDirectory);
    }

    public async Task<IReadOnlyList<string>> LoadWordsForGameAsync(string gameId, CancellationToken cancellationToken = default)
    {
        string filePath = GetFilePath(gameId);

        if (File.Exists(filePath))
        {
            try
            {
                using var stream = File.OpenRead(filePath);
                var words = await JsonSerializer.DeserializeAsync<List<string>>(stream, cancellationToken: cancellationToken);
                if (words != null && words.Count > 0)
                {
                    return words;
                }
            }
            catch
            {
                // Fallback zu Default-Wörtern
            }
        }

        // Liefere Standard-Wörter für dieses Spiel
        var defaults = GetDefaultWordsForGame(gameId);
        await SaveWordsForGameAsync(gameId, defaults, cancellationToken);
        return defaults;
    }

    public async Task AddWordForGameAsync(string gameId, string word, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(word)) return;

        var currentWords = (await LoadWordsForGameAsync(gameId, cancellationToken)).ToList();
        string normalized = word.Trim().ToUpperInvariant();

        if (!currentWords.Contains(normalized))
        {
            currentWords.Add(normalized);
            await SaveWordsForGameAsync(gameId, currentWords, cancellationToken);
        }
    }

    public async Task RemoveWordForGameAsync(string gameId, string word, CancellationToken cancellationToken = default)
    {
        var currentWords = (await LoadWordsForGameAsync(gameId, cancellationToken)).ToList();
        string normalized = word.Trim().ToUpperInvariant();

        if (currentWords.Remove(normalized))
        {
            await SaveWordsForGameAsync(gameId, currentWords, cancellationToken);
        }
    }

    private async Task SaveWordsForGameAsync(string gameId, IEnumerable<string> words, CancellationToken ct)
    {
        string filePath = GetFilePath(gameId);
        var options = new JsonSerializerOptions { WriteIndented = true };
        using var stream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(stream, words, options, ct);
    }

    private string GetFilePath(string gameId) => Path.Combine(_storageDirectory, $"{gameId.ToLowerInvariant()}.json");

    public static List<string> GetDefaultWordsForGame(string gameId)
    {
        if (gameId.Equals("free-typing", StringComparison.OrdinalIgnoreCase))
        {
            return new List<string>
            {
                // Familie & Identität (Moritz als Held!)
                "MAMA", "PAPA", "MORITZ", "OMA", "OPA", "BABY", "KIND", "FAMILIE",
                
                // Fahrzeuge & Lieblingsthemen von Vorschulkindern
                "AUTO", "BAGGER", "TRAKTOR", "BUS", "ZUG", "FEUERWEHR", "POLIZEI", "BOOT", "FLUGZEUG",
                
                // Tiere
                "KATZE", "HUND", "MAUS", "KUH", "PFERD", "SCHWEIN", "LOEWE", "TIGER", "BAER", "ELEFANT", "VOGEL", "FISCH",
                
                // Natur & Alltag
                "HAUS", "BAUM", "BALL", "EIS", "SONNE", "MOND", "STERN", "BROT", "MILCH", "WASSER", "APFEL", "BANANE",
                
                // Farben & Konzepte
                "ROT", "BLAU", "GRUEN", "GELB", "BUNT", "JA", "NEIN", "HALLO", "DANKE", "BITTE"
            };
        }

        return new List<string> { "START", "JA", "NEIN" };
    }
}
