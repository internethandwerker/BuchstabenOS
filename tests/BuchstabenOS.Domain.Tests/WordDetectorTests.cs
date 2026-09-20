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

    [Fact]
    public void Should_LoadWords_And_Rebuild_Trie()
    {
        var detector = new WordDetector(new[] { "MAMA", "PAPA" });
        Assert.True(detector.IsExactWord("MAMA"));
        Assert.False(detector.IsExactWord("BAGGER"));

        detector.LoadWords(new[] { "BAGGER", "AUTO", "TRECKER" });

        Assert.Equal(3, detector.WordCount);
        Assert.True(detector.IsExactWord("BAGGER"));
        Assert.True(detector.IsExactWord("AUTO"));
        Assert.False(detector.IsExactWord("MAMA"));
    }

    [Fact]
    public void Should_RemoveWord_And_Rebuild_Trie()
    {
        var detector = new WordDetector(new[] { "MAMA", "PAPA", "MORITZ" });
        Assert.True(detector.IsExactWord("PAPA"));

        bool removed = detector.RemoveWord("PAPA");

        Assert.True(removed);
        Assert.Equal(2, detector.WordCount);
        Assert.False(detector.IsExactWord("PAPA"));
        Assert.True(detector.IsExactWord("MAMA"));
        Assert.True(detector.IsExactWord("MORITZ"));
    }
}
