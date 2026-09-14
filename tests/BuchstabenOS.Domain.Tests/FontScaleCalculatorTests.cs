using BuchstabenOS.Domain.Model.Typing;
using BuchstabenOS.Domain.Services;
using Xunit;

namespace BuchstabenOS.Domain.Tests;

public class FontScaleCalculatorTests
{
    private readonly FontScaleCalculator _calculator = new();

    [Fact]
    public void Single_Letter_Should_Have_Max_Font_Size()
    {
        // Viewport 1366x768
        var size = _calculator.Calculate(1, 1366, 768, out bool requiresWrap);

        Assert.False(requiresWrap);
        Assert.True(size.Points >= 200, $"1 letter should be very large, was {size.Points}");
    }

    [Fact]
    public void Additional_Letters_Should_Shrink_Font_Size()
    {
        var size1 = _calculator.Calculate(1, 1366, 768, out _);
        var size5 = _calculator.Calculate(5, 1366, 768, out _);
        var size10 = _calculator.Calculate(10, 1366, 768, out _);

        Assert.True(size1.Points > size5.Points, "5 letters should be smaller than 1 letter");
        Assert.True(size5.Points > size10.Points, "10 letters should be smaller than 5 letters");
    }

    [Fact]
    public void Many_Letters_Should_Trigger_Wrap_At_Minimum_Threshold()
    {
        // 50 Zeichen auf 1366px sollten unter das Minimum fallen
        var size = _calculator.Calculate(50, 1366, 768, out bool requiresWrap);

        Assert.True(requiresWrap, "Should require wrap for long line");
        Assert.Equal(FontSize.DefaultMin, size.Points);
    }
}
