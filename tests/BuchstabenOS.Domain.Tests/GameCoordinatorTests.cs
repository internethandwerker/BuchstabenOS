using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BuchstabenOS.Application.Ports;
using BuchstabenOS.Application.UseCases;
using BuchstabenOS.Domain.Model.Games;
using BuchstabenOS.Domain.Model.Typing;
using Xunit;

namespace BuchstabenOS.Domain.Tests;

public class GameCoordinatorTests
{
    private class FakeAudioPlayer : IAudioPlayer
    {
        public List<string> JinglesPlayed { get; } = new();
        public ValueTask PlayLetterAsync(char letter, SpeechMode mode, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask PlayJingleAsync(string jingleName, CancellationToken cancellationToken = default)
        {
            JinglesPlayed.Add(jingleName);
            return ValueTask.CompletedTask;
        }
    }

    private class FakeTtsEngine : ITtsEngine
    {
        public List<string> SpokenWords { get; } = new();
        public bool IsAvailable => true;

        public Task SpeakWordAsync(string word, CancellationToken cancellationToken = default)
        {
            SpokenWords.Add(word);
            return Task.CompletedTask;
        }
    }

    private class FakeGameRegistry : IGameRegistry
    {
        private readonly Dictionary<string, IGameModule> _games = new();

        public FakeGameRegistry(params IGameModule[] games)
        {
            foreach (var g in games) _games[g.Metadata.Id] = g;
        }

        public IGameModule? GetGameById(string id) => _games.TryGetValue(id, out var g) ? g : null;
        public IReadOnlyList<IGameModule> GetAllGames() => _games.Values.ToList();
        public void RegisterGame(IGameModule game) => _games[game.Metadata.Id] = game;
    }

    [Fact]
    public async Task StartGameAsync_With_Math_Game_Should_Speak_Initial_Task()
    {
        var mathGame = new AdditionGameModule();
        var registry = new FakeGameRegistry(mathGame);
        var audio = new FakeAudioPlayer();
        var tts = new FakeTtsEngine();
        var coordinator = new GameCoordinator(registry, audio, tts);

        await coordinator.StartGameAsync("math-addition");

        Assert.Single(tts.SpokenWords);
        Assert.Equal(mathGame.CurrentTaskSpokenPrompt, tts.SpokenWords[0]);
    }

    [Fact]
    public async Task SwitchGameAsync_With_Math_Game_Should_Speak_Intro_Then_Task()
    {
        var mathGame = new AdditionGameModule();
        var registry = new FakeGameRegistry(mathGame);
        var audio = new FakeAudioPlayer();
        var tts = new FakeTtsEngine();
        var coordinator = new GameCoordinator(registry, audio, tts);

        await coordinator.SwitchGameAsync("math-addition");

        Assert.Equal(2, tts.SpokenWords.Count);
        Assert.Equal("Neues Spiel!", tts.SpokenWords[0]);
        Assert.Equal(mathGame.CurrentTaskSpokenPrompt, tts.SpokenWords[1]);
    }

    [Fact]
    public async Task HandleInputAsync_On_MathTaskSolved_Should_Speak_Praise_Then_Next_Task()
    {
        var mathGame = new AdditionGameModule();
        var registry = new FakeGameRegistry(mathGame);
        var audio = new FakeAudioPlayer();
        var tts = new FakeTtsEngine();
        var coordinator = new GameCoordinator(registry, audio, tts);

        await coordinator.StartGameAsync("math-addition");
        tts.SpokenWords.Clear(); // Initialen Prompt leeren

        int expected = mathGame.ExpectedResult;
        string expectedStr = expected.ToString();

        foreach (char c in expectedStr)
        {
            await coordinator.HandleInputAsync(GameInput.FromChar(c));
        }

        // Erwartet:
        // 1. Lob für gelöste Aufgabe
        // 2. Neuer Prompt für die nächste Aufgabe
        Assert.Equal(2, tts.SpokenWords.Count);
        Assert.StartsWith("Prima!", tts.SpokenWords[0]);
        Assert.Equal(mathGame.CurrentTaskSpokenPrompt, tts.SpokenWords[1]);
    }
}
