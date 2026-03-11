using FluentAssertions;
using WcagAnalyzer.Infrastructure.Services;

namespace WcagAnalyzer.Tests.Infrastructure;

public class EmptyButtonTests
{
    [Fact]
    public void NotEmptyButton_ShouldReturnNoViolation()
    {
        var buttons = new List<ButtonInfo>
        {
            new("Submit", "", "", "<button>Submit</button>")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeEmptyButtons(buttons);

        result.Should().BeEmpty();
    }

    [Fact]
    public void EmptyButton_ShouldReturnViolation()
    {
        var buttons = new List<ButtonInfo>
        {
            new("", "", "", "<button></button>")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeEmptyButtons(buttons);

        result.Should().HaveCount(1);
        result[0].RuleId.Should().Be("button-empty");
        result[0].Impact.Should().Be("serious");
    }

    [Fact]
    public void ButtonWithAriaLabel_ShouldReturnNoViolation()
    {
        var buttons = new List<ButtonInfo>
        {
            new("", "Close dialog", "", "<button aria-label=\"Close dialog\">")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeEmptyButtons(buttons);

        result.Should().BeEmpty();
    }
}
