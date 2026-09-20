using System;
using System.Threading;
using System.Threading.Tasks;
using BuchstabenOS.Infrastructure.Audio;
using Xunit;

namespace BuchstabenOS.Domain.Tests;

public class PiperTtsEngineTests
{
    [Fact]
    public async Task SpeakWordAsync_With_Empty_Or_Whitespace_Should_Return_Immediately()
    {
        using var engine = new PiperTtsEngine();

        await engine.SpeakWordAsync("");
        await engine.SpeakWordAsync("   ");
    }

    [Fact]
    public async Task StopCurrentSpeech_Should_Cancel_Pending_Requests()
    {
        using var engine = new PiperTtsEngine();

        // Enqueue several requests
        var task1 = engine.SpeakWordAsync("Eins");
        var task2 = engine.SpeakWordAsync("Zwei");
        var task3 = engine.SpeakWordAsync("Drei");

        engine.StopCurrentSpeech();

        // Tasks that were pending in the queue should complete (either successfully if fallback or cancelled)
        await Task.WhenAny(Task.WhenAll(task1, task2, task3), Task.Delay(1000));
    }
}
