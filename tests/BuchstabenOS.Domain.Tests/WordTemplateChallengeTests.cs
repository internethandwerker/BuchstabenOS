using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BuchstabenOS.Domain.Events;
using BuchstabenOS.Domain.Model.Games;
using BuchstabenOS.Domain.Model.Typing;
using BuchstabenOS.Domain.Services;
using Xunit;

namespace BuchstabenOS.Domain.Tests;

public class WordTemplateChallengeTests
{
    private class TestGameContext : IGameContext
    {
        public string ActiveGameId => "free-typing";
        public List<IDomainEvent> PublishedEvents { get; } = new();

        public void PublishEvent(IDomainEvent domainEvent)
        {
            PublishedEvents.Add(domainEvent);
        }
    }

    private static (FreeTypingGameModule Game, TestGameContext Context) CreateGameWithWords(params string[] words)
    {
        var wordDetector = new WordDetector(words.Length > 0 ? words : new[] { "MAMA", "AUTO", "BALL" });
        var fontCalc = new FontScaleCalculator();
        var stage = new TextStage(fontCalc, wordDetector);
        var game = new FreeTypingGameModule(stage);
        var context = new TestGameContext();
        game.InitializeAsync(context);
        return (game, context);
    }

    [Fact]
    public void Manual_TriggerChallenge_Should_Activate_Template_Mode()
    {
        var (game, context) = CreateGameWithWords("MAMA");

        var startEv = game.TriggerChallenge("MAMA");

        Assert.NotNull(startEv);
        Assert.True(game.IsTemplateChallengeActive);
        Assert.Equal("MAMA", game.TemplateTargetWord);
        Assert.Equal(0, game.TemplateProgressIndex);
        Assert.False(game.IsTemplateMistake);
        Assert.Contains(startEv, context.PublishedEvents);
    }

    [Fact]
    public async Task Correct_Letters_Should_Advance_Progress_Until_Completion()
    {
        var (game, context) = CreateGameWithWords("AUTO");
        game.TriggerChallenge("AUTO");

        // 1. Buchstabe: 'A'
        var resA = await game.ProcessInputAsync(new GameInput('A', false, false, false, false, false, DateTime.UtcNow));
        Assert.True(resA.WasHandled);
        Assert.Equal(1, game.TemplateProgressIndex);
        Assert.False(game.IsTemplateMistake);
        Assert.Contains(resA.EmittedEvents, e => e is WordTemplateLetterMatchedEvent m && m.Letter == 'A');

        // 2. Buchstabe: 'u' (Kleinbuchstabe sollte case-insensitive funktionieren)
        var resU = await game.ProcessInputAsync(new GameInput('u', false, false, false, false, false, DateTime.UtcNow));
        Assert.Equal(2, game.TemplateProgressIndex);
        Assert.Contains(resU.EmittedEvents, e => e is WordTemplateLetterMatchedEvent m && m.Letter == 'U');

        // 3. Buchstabe: 'T'
        await game.ProcessInputAsync(new GameInput('T', false, false, false, false, false, DateTime.UtcNow));
        Assert.Equal(3, game.TemplateProgressIndex);

        // 4. Letzter Buchstabe: 'O' -> Vollendung!
        var resO = await game.ProcessInputAsync(new GameInput('O', false, false, false, false, false, DateTime.UtcNow));
        Assert.False(game.IsTemplateChallengeActive); // Zurückgesetzt für nächste freie Runde
        Assert.True(game.IsCelebrating);
        Assert.Contains("AUTO", game.CelebrationMessage);
        Assert.Contains(resO.EmittedEvents, e => e is WordTemplateChallengeCompletedEvent c && c.TargetWord == "AUTO");
    }

    [Fact]
    public async Task Wrong_Letter_Should_Trigger_Mistake_Without_Advancing()
    {
        var (game, context) = CreateGameWithWords("MAMA");
        game.TriggerChallenge("MAMA");

        // Falscher Buchstabe 'X'
        var res = await game.ProcessInputAsync(new GameInput('X', false, false, false, false, false, DateTime.UtcNow));

        Assert.True(res.WasHandled);
        Assert.True(game.IsTemplateChallengeActive);
        Assert.Equal(0, game.TemplateProgressIndex); // Kein Fortschritt
        Assert.True(game.IsTemplateMistake);
        Assert.True(game.IsWrongFeedback);
        Assert.Contains(res.EmittedEvents, e => e is WordTemplateMistakeEvent m && m.ActualLetter == 'X');

        // Nun richtiger Buchstabe 'M' -> Fehlerzustand löst sich auf
        await game.ProcessInputAsync(new GameInput('M', false, false, false, false, false, DateTime.UtcNow));
        Assert.Equal(1, game.TemplateProgressIndex);
        Assert.False(game.IsTemplateMistake);
        Assert.False(game.IsWrongFeedback);
    }

    [Fact]
    public async Task Space_Key_Should_Skip_Challenge_And_Emit_Skipped_Event()
    {
        var (game, context) = CreateGameWithWords("PAPA");
        game.TriggerChallenge("PAPA");
        Assert.True(game.IsTemplateChallengeActive);

        // Kind drückt Leertaste zum Überspringen
        var res = await game.ProcessInputAsync(new GameInput(' ', true, false, false, false, false, DateTime.UtcNow));

        Assert.True(res.WasHandled);
        Assert.False(game.IsTemplateChallengeActive);
        Assert.Null(game.TemplateTargetWord);
        Assert.Contains(res.EmittedEvents, e => e is WordTemplateChallengeSkippedEvent s && s.TargetWord == "PAPA");
    }

    [Fact]
    public async Task FreeTyping_Should_Trigger_Challenge_When_Threshold_Reached()
    {
        var (game, _) = CreateGameWithWords("BALL");

        // Setze Intervall auf genau 5 für schnellen Test
        game.ApplyConfiguration(new Dictionary<string, object>
        {
            ["WordTemplatesEnabled"] = true,
            ["WordTemplateInterval"] = 5
        });

        // Tippe Buchstaben im freien Modus
        for (int i = 0; i < 20; i++)
        {
            if (game.IsTemplateChallengeActive) break;
            await game.ProcessInputAsync(new GameInput('X', false, false, false, false, false, DateTime.UtcNow));
        }

        Assert.True(game.IsTemplateChallengeActive);
        Assert.Equal("BALL", game.TemplateTargetWord);
    }

    [Fact]
    public async Task Disabled_Templates_Should_Never_Trigger_Challenge()
    {
        var (game, _) = CreateGameWithWords("BALL");

        game.ApplyConfiguration(new Dictionary<string, object>
        {
            ["WordTemplatesEnabled"] = false,
            ["WordTemplateInterval"] = 5
        });

        for (int i = 0; i < 30; i++)
        {
            await game.ProcessInputAsync(new GameInput('X', false, false, false, false, false, DateTime.UtcNow));
        }

        Assert.False(game.IsTemplateChallengeActive);
    }
}
