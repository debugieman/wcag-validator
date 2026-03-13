using FluentAssertions;
using WcagAnalyzer.Infrastructure.Services;

namespace WcagAnalyzer.Tests.Infrastructure;

public class TouchTargetTests
{
    [Fact]
    public void NoElements_ShouldReturnNoViolations()
    {
        var result = PlaywrightAccessibilityAnalyzer.AnalyzeTouchTargetSize(new List<TouchTargetInfo>());

        result.Should().BeEmpty();
    }

    [Fact]
    public void ElementWith44x44_ShouldReturnNoViolations()
    {
        var elements = new List<TouchTargetInfo>
        {
            new("button", 44, 44, "<button>Submit</button>")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeTouchTargetSize(elements);

        result.Should().BeEmpty();
    }

    [Fact]
    public void ElementLargerThan44x44_ShouldReturnNoViolations()
    {
        var elements = new List<TouchTargetInfo>
        {
            new("button", 120, 48, "<button>Submit</button>")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeTouchTargetSize(elements);

        result.Should().BeEmpty();
    }

    [Fact]
    public void ElementTooSmallInWidth_ShouldReturnViolation()
    {
        var elements = new List<TouchTargetInfo>
        {
            new("a", 30, 44, "<a href=\"/\">Link</a>")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeTouchTargetSize(elements);

        result.Should().HaveCount(1);
        result[0].RuleId.Should().Be("touch-target-too-small");
        result[0].Impact.Should().Be("moderate");
    }

    [Fact]
    public void ElementTooSmallInHeight_ShouldReturnViolation()
    {
        var elements = new List<TouchTargetInfo>
        {
            new("button", 44, 20, "<button>X</button>")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeTouchTargetSize(elements);

        result.Should().HaveCount(1);
        result[0].RuleId.Should().Be("touch-target-too-small");
    }

    [Fact]
    public void MultipleElements_ShouldReturnViolationForEachTooSmall()
    {
        var elements = new List<TouchTargetInfo>
        {
            new("button", 44, 44, "<button>OK</button>"),
            new("a", 20, 20, "<a href=\"/\">x</a>"),
            new("input", 200, 16, "<input type=\"text\">")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeTouchTargetSize(elements);

        result.Should().HaveCount(2);
    }

    [Fact]
    public void ViolationDescription_ShouldContainActualSizeAndWCAG255()
    {
        var elements = new List<TouchTargetInfo>
        {
            new("button", 30, 20, "<button>X</button>")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeTouchTargetSize(elements);

        result[0].Description.Should().Contain("30");
        result[0].Description.Should().Contain("20");
        result[0].Description.Should().Contain("WCAG 2.5.5");
    }
}
