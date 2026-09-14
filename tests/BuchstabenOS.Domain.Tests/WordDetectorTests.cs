using BuchstabenOS.Domain.Services;
using Xunit;

namespace BuchstabenOS.Domain.Tests;

public class WordDetectorTests
{
    [Fact]
    public void Should_Recognize_Exact_Words_Case_Insensitive()
    {
        var detector = new WordDetector(new[] { "MAMA", "PAPA", "MORITZ", "AUTO" });

        Assert.True(detector.IsExactWord("mama"));
        Assert.True(detector.IsExactWord("MAMA"));
        Assert.True(detector.IsExactWord("Moritz"));
        Assert.True(detector.IsExactWord("AUTO"));

        Assert.False(detector.IsExactWord("XYZ"));
        Assert.False(detector.IsExactWord("MAM"));
    }

    [Fact]
    public void Should_Recognize_German_Umlauts()
    {
        var detector = new WordDetector(new[] { "BÄR", "LÖWE", "KÜHE" });

        Assert.True(detector.IsExactWord("bär"));
        Assert.True(detector.IsExactWord("LÖWE"));
        Assert.True(detector.IsExactWord("kühe"));
    }

    [Fact]
    public void Should_Detect_Prefixes_Correctly()
    {
        var detector = new WordDetector(new[] { "MORITZ", "MOTOR" });

        Assert.True(detector.IsPrefix("MO"));
        Assert.True(detector.IsPrefix("MORI"));
        Assert.False(detector.IsPrefix("MA"));
    }
}
