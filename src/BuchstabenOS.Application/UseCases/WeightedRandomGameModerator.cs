using System;
using System.Collections.Generic;
using System.Linq;
using BuchstabenOS.Application.Configuration;
using BuchstabenOS.Domain.Model.Games;

namespace BuchstabenOS.Application.UseCases;

/// <summary>
/// Gewichteter Zufallswürfel zur Moderation zwischen Spielen:
/// Zählt Durchläufe/Aufgaben und Verweildauer pro Spiel. Ist die Aufmerksamkeitsspanne
/// erreicht, würfelt er unter Berücksichtigung der konfigurierten Gewichte das nächste Spiel aus.
/// </summary>
public class WeightedRandomGameModerator : IGameModerator
{
    private readonly Random _random;
    private AppSettings _settings;
    private readonly Dictionary<string, int> _roundsSinceSwitch = new(StringComparer.OrdinalIgnoreCase);

    public bool IsAutoSwitchEnabled
    {
        get => _settings.AutoGameSwitching;
        set => _settings.AutoGameSwitching = value;
    }

    public WeightedRandomGameModerator(AppSettings? initialSettings = null, Random? random = null)
    {
        _settings = initialSettings ?? new AppSettings();
        _random = random ?? new Random();
    }

    public void UpdateSettings(AppSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public bool RecordRoundCompleted(string gameId, int completedRounds, TimeSpan elapsedPlayTime)
    {
        if (!IsAutoSwitchEnabled) return false;

        _roundsSinceSwitch.TryGetValue(gameId, out int count);
        count++;
        _roundsSinceSwitch[gameId] = count;

        // Ermittle Schwellenwert für dieses Spiel
        int threshold = gameId.Equals("math-addition", StringComparison.OrdinalIgnoreCase)
            ? _settings.MathAttentionSpan
            : _settings.TypingAttentionSpan;

        TimeSpan maxDuration = TimeSpan.FromMinutes(_settings.InactivityMinutes > 0 ? Math.Min(_settings.InactivityMinutes, 5) : 5);

        // Prüfe Kriterien: Schwellenwert der Aufgaben erreicht ODER maximale Zeit abgelaufen
        if (count >= threshold || elapsedPlayTime >= maxDuration)
        {
            _roundsSinceSwitch[gameId] = 0;
            return true;
        }

        return false;
    }

    public string SelectNextGame(string currentGameId, IReadOnlyList<IGameModule> availableGames)
    {
        if (availableGames == null || availableGames.Count == 0) return currentGameId;

        // Filtere alle Spiele mit Gewicht > 0
        var candidates = new List<(string GameId, int Weight)>();

        foreach (var game in availableGames)
        {
            string id = game.Metadata.Id;
            int weight = _settings.GameWeights.TryGetValue(id, out int w) ? w : game.Metadata.DefaultWeight;
            if (weight > 0)
            {
                candidates.Add((id, weight));
            }
        }

        if (candidates.Count == 0)
        {
            return currentGameId;
        }

        if (candidates.Count == 1)
        {
            return candidates[0].GameId;
        }

        // Bevorzuge andere Spiele gegenüber dem aktuellen (Wechsel-Erlebnis)
        var otherCandidates = candidates.Where(c => !c.GameId.Equals(currentGameId, StringComparison.OrdinalIgnoreCase)).ToList();
        var pool = otherCandidates.Count > 0 ? otherCandidates : candidates;

        int totalWeight = pool.Sum(c => c.Weight);
        if (totalWeight <= 0) return currentGameId;

        int roll = _random.Next(0, totalWeight);
        int currentSum = 0;

        foreach (var (gameId, weight) in pool)
        {
            currentSum += weight;
            if (roll < currentSum)
            {
                return gameId;
            }
        }

        return pool.Last().GameId;
    }

    public string GetGameIntroPrompt(string nextGameId)
    {
        return nextGameId.ToLowerInvariant() switch
        {
            "math-addition" => "Prima! Jetzt wird gerechnet. Mal sehen, wie gut du zählen kannst!",
            "free-typing" => "Super gemacht! Jetzt zaubern wir wieder Buchstaben auf der Tastatur!",
            _ => "Los geht's mit der nächsten Runde!"
        };
    }
}
