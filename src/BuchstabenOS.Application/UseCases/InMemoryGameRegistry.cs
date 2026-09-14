using System.Collections.Concurrent;
using BuchstabenOS.Application.Ports;
using BuchstabenOS.Domain.Model.Games;

namespace BuchstabenOS.Application.UseCases;

/// <summary>
/// Verwaltet alle registrierten Spielmodule im System.
/// </summary>
public class InMemoryGameRegistry : IGameRegistry
{
    private readonly ConcurrentDictionary<string, IGameModule> _games = new(StringComparer.OrdinalIgnoreCase);

    public InMemoryGameRegistry(IEnumerable<IGameModule>? initialGames = null)
    {
        if (initialGames != null)
        {
            foreach (var game in initialGames)
            {
                RegisterGame(game);
            }
        }
    }

    public IReadOnlyList<IGameModule> GetAllGames() => _games.Values.ToList();

    public IGameModule? GetGameById(string gameId)
    {
        _games.TryGetValue(gameId, out var game);
        return game;
    }

    public void RegisterGame(IGameModule game)
    {
        ArgumentNullException.ThrowIfNull(game);
        _games[game.Metadata.Id] = game;
    }
}
