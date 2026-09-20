using System;
using System.Collections.Generic;
using BuchstabenOS.Application.Configuration;
using BuchstabenOS.Application.UseCases;
using BuchstabenOS.Domain.Model.Games;
using BuchstabenOS.Domain.Model.Typing;
using BuchstabenOS.Domain.Services;
using Xunit;

namespace BuchstabenOS.Domain.Tests;

public class GameModeratorTests
{
    private class FakeGameModule : IGameModule
    {
        public GameMetadata Metadata { get; }
        public bool IsActive => true;
        public FakeGameModule(string id, int defaultWeight = 50)
        {
            Metadata = new GameMetadata(
                Id: id,
                Title: id,
                Description: "Test",
                Category: "Test",
                Age: new AgeRecommendation(3, 6),
                TargetSkills: Array.Empty<PedagogicalSkill>(),
                Prerequisites: Array.Empty<SkillPrerequisite>(),
                LearningObjectives: Array.Empty<string>(),
                DefaultWeight: defaultWeight
            );
        }
        public Task InitializeAsync(IGameContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public ValueTask<GameInputResult> ProcessInputAsync(GameInput input) => ValueTask.FromResult(GameInputResult.Unhandled);
        public KnowledgeScore GetScore() => KnowledgeScore.Empty;
        public void Reset() { }
    }

    [Fact]
    public void Moderator_Should_Trigger_Switch_After_Math_Attention_Span()
    {
        var settings = new AppSettings
        {
            AutoGameSwitching = true,
            MathAttentionSpan = 2
        };

        var moderator = new WeightedRandomGameModerator(settings);

        // 1. Gelöste Mathe-Aufgabe
        bool switch1 = moderator.RecordRoundCompleted("math-addition", 1, TimeSpan.FromMinutes(1));
        Assert.False(switch1);

        // 2. Gelöste Mathe-Aufgabe (Schwelle 2 erreicht!)
        bool switch2 = moderator.RecordRoundCompleted("math-addition", 2, TimeSpan.FromMinutes(2));
        Assert.True(switch2);
    }

    [Fact]
    public void Moderator_Should_Trigger_Switch_After_Typing_Attention_Span()
    {
        var settings = new AppSettings
        {
            AutoGameSwitching = true,
            TypingAttentionSpan = 5
        };

        var moderator = new WeightedRandomGameModerator(settings);

        for (int i = 1; i < 5; i++)
        {
            Assert.False(moderator.RecordRoundCompleted("free-typing", i, TimeSpan.FromMinutes(1)));
        }

        // 5. Wort erreicht Schwelle
        Assert.True(moderator.RecordRoundCompleted("free-typing", 5, TimeSpan.FromMinutes(2)));
    }

    [Fact]
    public void Moderator_Should_Not_Trigger_When_Auto_Switch_Is_Disabled()
    {
        var settings = new AppSettings
        {
            AutoGameSwitching = false,
            MathAttentionSpan = 2
        };

        var moderator = new WeightedRandomGameModerator(settings);

        Assert.False(moderator.RecordRoundCompleted("math-addition", 1, TimeSpan.FromMinutes(1)));
        Assert.False(moderator.RecordRoundCompleted("math-addition", 2, TimeSpan.FromMinutes(2)));
        Assert.False(moderator.RecordRoundCompleted("math-addition", 10, TimeSpan.FromMinutes(10)));
    }

    [Fact]
    public void Moderator_Should_Respect_Zero_Weight()
    {
        var settings = new AppSettings
        {
            GameWeights = new Dictionary<string, int>
            {
                ["free-typing"] = 100,
                ["math-addition"] = 0 // Deaktiviert
            }
        };

        var moderator = new WeightedRandomGameModerator(settings);
        var games = new List<IGameModule>
        {
            new FakeGameModule("free-typing", 100),
            new FakeGameModule("math-addition", 0)
        };

        for (int i = 0; i < 20; i++)
        {
            string next = moderator.SelectNextGame("math-addition", games);
            Assert.Equal("free-typing", next);
        }
    }
}
