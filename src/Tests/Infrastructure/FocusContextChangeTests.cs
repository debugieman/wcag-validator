using FluentAssertions;
using WcagAnalyzer.Infrastructure.Services;

namespace WcagAnalyzer.Tests.Infrastructure;

public class FocusContextChangeTests
{
    [Fact]
    public void NoContextChanges_ShouldReturnNoViolations()
    {
        var result = PlaywrightAccessibilityAnalyzer.AnalyzeFocusContextChange(new List<FocusContextInfo>());

        result.Should().BeEmpty();
    }

    [Fact]
    public void ContextChangeOnFocus_ShouldReturnViolation()
    {
        var items = new List<FocusContextInfo>
        {
            new("<a href=\"/other\">Link</a>", "https://example.com/", "https://example.com/other")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeFocusContextChange(items);

        result.Should().HaveCount(1);
        result[0].RuleId.Should().Be("focus-causes-context-change");
        result[0].Impact.Should().Be("serious");
    }

    [Fact]
    public void MultipleContextChanges_ShouldReturnViolationForEach()
    {
        var items = new List<FocusContextInfo>
        {
            new("<a href=\"/a\">A</a>", "https://example.com/", "https://example.com/a"),
            new("<a href=\"/b\">B</a>", "https://example.com/a", "https://example.com/b")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeFocusContextChange(items);

        result.Should().HaveCount(2);
    }

    [Fact]
    public void ViolationDescription_ShouldContainUrlsAndWCAG321()
    {
        var items = new List<FocusContextInfo>
        {
            new("<button>Go</button>", "https://example.com/", "https://example.com/other")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeFocusContextChange(items);

        result[0].Description.Should().Contain("https://example.com/");
        result[0].Description.Should().Contain("https://example.com/other");
        result[0].Description.Should().Contain("WCAG 3.2.1");
    }
}
