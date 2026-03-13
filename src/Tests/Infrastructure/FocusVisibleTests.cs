using FluentAssertions;
using WcagAnalyzer.Infrastructure.Services;

namespace WcagAnalyzer.Tests.Infrastructure;

public class FocusVisibleTests
{
    [Fact]
    public void NoElements_ShouldReturnNoViolations()
    {
        var result = PlaywrightAccessibilityAnalyzer.AnalyzeFocusVisible(new List<FocusInfo>());

        result.Should().BeEmpty();
    }

    [Fact]
    public void ElementWithVisibleOutline_ShouldReturnNoViolations()
    {
        var elements = new List<FocusInfo>
        {
            new("button", "2px", "solid", "rgb(0, 0, 0)", "<button>Submit</button>")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeFocusVisible(elements);

        result.Should().BeEmpty();
    }

    [Fact]
    public void ElementWithOutlineNone_ShouldReturnViolation()
    {
        var elements = new List<FocusInfo>
        {
            new("button", "0px", "none", "transparent", "<button>Submit</button>")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeFocusVisible(elements);

        result.Should().HaveCount(1);
        result[0].RuleId.Should().Be("focus-visible-missing");
        result[0].Impact.Should().Be("serious");
    }

    [Fact]
    public void ElementWithOutlineWidthZero_ShouldReturnViolation()
    {
        var elements = new List<FocusInfo>
        {
            new("a", "0px", "solid", "rgb(0, 0, 0)", "<a href=\"/\">Home</a>")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeFocusVisible(elements);

        result.Should().HaveCount(1);
        result[0].RuleId.Should().Be("focus-visible-missing");
    }

    [Fact]
    public void ElementWithOutlineHidden_ShouldReturnViolation()
    {
        var elements = new List<FocusInfo>
        {
            new("input", "2px", "hidden", "rgb(0, 0, 0)", "<input type=\"text\">")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeFocusVisible(elements);

        result.Should().HaveCount(1);
        result[0].RuleId.Should().Be("focus-visible-missing");
    }

    [Fact]
    public void MultipleElements_ShouldReturnViolationForEachInvisible()
    {
        var elements = new List<FocusInfo>
        {
            new("button", "2px", "solid", "blue", "<button>OK</button>"),
            new("a", "0px", "none", "transparent", "<a href=\"/\">Link</a>"),
            new("input", "0px", "none", "transparent", "<input>")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeFocusVisible(elements);

        result.Should().HaveCount(2);
    }

    [Fact]
    public void ViolationDescription_ShouldMentionWCAG247()
    {
        var elements = new List<FocusInfo>
        {
            new("button", "0px", "none", "transparent", "<button>X</button>")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeFocusVisible(elements);

        result[0].Description.Should().Contain("WCAG 2.4.7");
    }
}
