using BuchstabenOS.Domain.Events;
using BuchstabenOS.Domain.Model.Security;
using BuchstabenOS.Domain.Model.Typing;
using BuchstabenOS.Domain.Services;
using Xunit;

namespace BuchstabenOS.Domain.Tests;

public class TextStageTests
{
    private readonly TextStage _stage;

    public TextStageTests()
    {
        var calculator = new FontScaleCalculator();
        var detector = new WordDetector(new[] { "MAMA", "PAPA", "MORITZ", "AUTO" });
        _stage = new TextStage(calculator, detector);
    }

    [Fact]
    public void Typing_Letter_Should_Emit_LetterTypedEvent()
    {
        _stage.TypeCharacter('a', 1366, 768);

        var events = _stage.DequeueEvents();
        Assert.Single(events);
        var letterEvent = Assert.IsType<LetterTypedEvent>(events[0]);
        Assert.Equal('A', letterEvent.Character);
        Assert.True(letterEvent.IsLetter);
        Assert.Equal("A", _stage.CurrentText);
    }

    [Fact]
    public void Typing_Complete_Word_Should_Emit_WordRecognizedEvent()
    {
        _stage.TypeCharacter('m', 1366, 768);
        _stage.DequeueEvents();

        _stage.TypeCharacter('a', 1366, 768);
        _stage.DequeueEvents();

        _stage.TypeCharacter('m', 1366, 768);
        _stage.DequeueEvents();

        _stage.TypeCharacter('a', 1366, 768);
        var events = _stage.DequeueEvents();

        Assert.Equal(2, events.Count); // LetterTyped + WordRecognized
        Assert.Contains(events, e => e is LetterTypedEvent);
        var wordEvent = Assert.Single(events.OfType<WordRecognizedEvent>());
        Assert.Equal("MAMA", wordEvent.Word);
        Assert.True(wordEvent.IsDictionaryMatch);
    }

    [Fact]
    public void Pressing_Space_Should_Emit_SpacePressedEvent_With_Raw_Word()
    {
        _stage.TypeCharacter('k', 1366, 768);
        _stage.TypeCharacter('w', 1366, 768);
        _stage.TypeCharacter('x', 1366, 768); // Quatschwort
        _stage.DequeueEvents();

        _stage.PressSpace(1366, 768);
        var events = _stage.DequeueEvents();

        var spaceEvent = Assert.Single(events.OfType<SpacePressedEvent>());
        Assert.Equal("KWX", spaceEvent.RawWord);
    }

    [Fact]
    public void Backspace_Should_Remove_Last_Character()
    {
        _stage.TypeCharacter('a', 1366, 768);
        _stage.TypeCharacter('b', 1366, 768);
        _stage.PressBackspace(1366, 768);

        Assert.Equal("A", _stage.CurrentText);
    }
}

public class ParentPinTests
{
    [Fact]
    public void Default_Pin_Should_Verify_Correctly()
    {
        var pin = ParentPin.CreateDefault();

        Assert.True(pin.Verify("1337"));
        Assert.False(pin.Verify("0000"));
        Assert.False(pin.Verify(""));
    }

    [Fact]
    public void Custom_Pin_Should_Verify_Correctly()
    {
        var pin = new ParentPin("4567");

        Assert.True(pin.Verify("4567"));
        Assert.False(pin.Verify("1337"));
    }
}
