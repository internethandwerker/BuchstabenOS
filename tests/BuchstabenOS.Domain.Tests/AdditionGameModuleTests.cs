using System;
using System.Linq;
using System.Threading.Tasks;
using BuchstabenOS.Domain.Events;
using BuchstabenOS.Domain.Model.Games;
using Xunit;

namespace BuchstabenOS.Domain.Tests;

public class AdditionGameModuleTests
{
    private class TestGameContext : IGameContext
    {
        public string ActiveGameId => "math-addition";
        public List<IDomainEvent> PublishedEvents { get; } = new();

        public void PublishEvent(IDomainEvent domainEvent)
        {
            PublishedEvents.Add(domainEvent);
        }
    }

    [Fact]
    public async Task Initialized_Task_Should_Respect_MaxSum()
    {
        var game = new AdditionGameModule { MaxSum = 8 };
        var context = new TestGameContext();
        await game.InitializeAsync(context);

        Assert.True(game.OperandA >= 1);
        Assert.True(game.OperandB >= 1);
        Assert.True(game.ExpectedResult <= 8);
        Assert.Equal(game.OperandA + game.OperandB, game.ExpectedResult);
        Assert.Contains("+", game.DisplayText);
    }

    [Fact]
    public async Task Non_Digit_Inputs_Should_Be_Ignored()
    {
        var game = new AdditionGameModule();
        var context = new TestGameContext();
        await game.InitializeAsync(context);

        var result = await game.ProcessInputAsync(GameInput.FromChar('a'));

        Assert.False(result.WasHandled);
        Assert.Empty(game.UserAnswer);
    }

    [Fact]
    public async Task Correct_Answer_Should_Emit_Solved_And_RoundCompleted_Events()
    {
        var game = new AdditionGameModule();
        var context = new TestGameContext();
        await game.InitializeAsync(context);

        int expected = game.ExpectedResult;
        string expectedStr = expected.ToString();

        GameInputResult lastResult = GameInputResult.Unhandled;
        foreach (char digit in expectedStr)
        {
            lastResult = await game.ProcessInputAsync(GameInput.FromChar(digit));
        }

        Assert.True(lastResult.WasHandled);
        Assert.True(game.IsCelebrating);
        Assert.False(game.IsWrongFeedback);

        var solvedEvent = context.PublishedEvents.OfType<MathTaskSolvedEvent>().FirstOrDefault();
        Assert.NotNull(solvedEvent);
        Assert.Equal(expected, solvedEvent.Result);

        var roundEvent = context.PublishedEvents.OfType<GameRoundCompletedEvent>().FirstOrDefault();
        Assert.NotNull(roundEvent);
        Assert.Equal("math-addition", roundEvent.GameId);
        Assert.Equal(1, roundEvent.TotalCompletedRounds);

        Assert.Single(game.CompletedLines);
    }

    [Fact]
    public async Task Wrong_Answer_Should_Emit_Failed_Event_And_Retain_Same_Task()
    {
        var game = new AdditionGameModule();
        var context = new TestGameContext();
        await game.InitializeAsync(context);

        int originalA = game.OperandA;
        int originalB = game.OperandB;
        int expected = game.ExpectedResult;

        // Falsche Antwort wählen mit gleicher Ziffernlänge
        int targetLength = expected.ToString().Length;
        string wrongAnswer = expected == 5 ? "4" : "0";
        if (targetLength == 2 && wrongAnswer.Length == 1)
        {
            wrongAnswer = expected == 10 ? "11" : "99";
        }

        GameInputResult lastResult = GameInputResult.Unhandled;
        foreach (char digit in wrongAnswer)
        {
            lastResult = await game.ProcessInputAsync(GameInput.FromChar(digit));
        }

        Assert.True(lastResult.WasHandled);
        Assert.True(game.IsWrongFeedback);
        Assert.False(game.IsCelebrating);
        Assert.Empty(game.UserAnswer); // Puffer geleert

        // Selbe Aufgabe muss beibehalten worden sein!
        Assert.Equal(originalA, game.OperandA);
        Assert.Equal(originalB, game.OperandB);
        Assert.Equal(expected, game.ExpectedResult);

        var failedEvent = context.PublishedEvents.OfType<MathTaskFailedEvent>().FirstOrDefault();
        Assert.NotNull(failedEvent);
        Assert.Equal(wrongAnswer, failedEvent.WrongAnswer);
    }

    [Fact]
    public async Task Backspace_Should_Remove_Last_Typed_Digit()
    {
        var game = new AdditionGameModule { MaxSum = 15 };
        var context = new TestGameContext();
        await game.InitializeAsync(context);

        // Wir tippen '1'
        await game.ProcessInputAsync(GameInput.FromChar('1'));
        if (game.ExpectedResult < 10)
        {
            // Wenn 1 Ziffer ausreicht, hat er schon evaluiert.
            // Falls 2 Ziffern nötig sind:
        }
        else
        {
            Assert.Equal("1", game.UserAnswer);
            await game.ProcessInputAsync(GameInput.Backspace);
            Assert.Empty(game.UserAnswer);
        }
    }

    [Fact]
    public void Configuration_Descriptors_And_Application_Should_Work()
    {
        var game = new AdditionGameModule();
        var descriptors = game.GetConfigurationDescriptors();

        Assert.Single(descriptors);
        Assert.Equal("MaxSum", descriptors[0].Key);

        game.ApplyConfiguration(new Dictionary<string, object> { ["MaxSum"] = 5 });
        Assert.Equal(5, game.MaxSum);
    }

    [Fact]
    public void FormatTemplate_Should_Replace_Placeholders_And_Handle_Static_Text()
    {
        // 1. Platzhalter [a] und [b]
        string resultAb = AdditionGameModule.FormatTemplate("Was ist [a] plus [b]?", 2, 3);
        Assert.Equal("Was ist zwei plus drei?", resultAb);

        // 2. Platzhalter {0} und {1}
        string resultPos = AdditionGameModule.FormatTemplate("Kannst du mir sagen, was {0} plus {1} ist?", 1, 4);
        Assert.Equal("Kannst du mir sagen, was eins plus vier ist?", resultPos);

        // 3. Statischer Text ohne Platzhalter
        string resultStatic = AdditionGameModule.FormatTemplate("Wie viel ist das? Bitte rechne die Aufgabe aus.", 5, 5);
        Assert.Equal("Wie viel ist das? Bitte rechne die Aufgabe aus.", resultStatic);
    }

    [Fact]
    public void Dice_RollSpokenPrompt_Should_Not_Repeat_Immediate_Previous_Template()
    {
        var game = new AdditionGameModule();
        string previous = string.Empty;

        for (int i = 0; i < 25; i++)
        {
            string current = game.RollSpokenPrompt(2, 3);
            Assert.False(string.IsNullOrWhiteSpace(current));

            if (!string.IsNullOrEmpty(previous))
            {
                Assert.NotEqual(previous, current);
            }

            previous = current;
        }
    }

    [Fact]
    public async Task Initialized_And_Generated_Tasks_Should_Have_Spoken_Prompt()
    {
        var game = new AdditionGameModule();
        var context = new TestGameContext();
        await game.InitializeAsync(context);

        Assert.False(string.IsNullOrWhiteSpace(game.CurrentTaskSpokenPrompt));

        var generatedEvent = game.GenerateNewTask();
        Assert.False(string.IsNullOrWhiteSpace(game.CurrentTaskSpokenPrompt));
        Assert.Equal(game.CurrentTaskSpokenPrompt, generatedEvent.SpokenPrompt);
    }
}
