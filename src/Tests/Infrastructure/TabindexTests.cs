using FluentAssertions;
using WcagAnalyzer.Infrastructure.Services;

namespace WcagAnalyzer.Tests.Infrastructure;

public class TabindexTests
{
    [Fact]
    public void NoTabindexElements_ShouldReturnNoViolations()
    {
        var result = PlaywrightAccessibilityAnalyzer.AnalyzeTabindex(new List<TabindexInfo>());

        result.Should().BeEmpty();
    }

    [Fact]
    public void ElementWithTabindexZero_ShouldReturnNoViolations()
    {
        var elements = new List<TabindexInfo>
        {
            new("div", 0, false, "<div tabindex=\"0\">")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeTabindex(elements);

        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(10)]
    public void ElementWithPositiveTabindex_ShouldReturnViolation(int tabindex)
    {
        var elements = new List<TabindexInfo>
        {
            new("div", tabindex, false, $"<div tabindex=\"{tabindex}\">")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeTabindex(elements);

        result.Should().HaveCount(1);
        result[0].RuleId.Should().Be("tabindex-positive");
        result[0].Impact.Should().Be("moderate");
    }

    [Theory]
    [InlineData("button")]
    [InlineData("a")]
    [InlineData("input")]
    [InlineData("select")]
    [InlineData("textarea")]
    public void InteractiveElementWithTabindexMinusOne_ShouldReturnViolation(string tag)
    {
        var elements = new List<TabindexInfo>
        {
            new(tag, -1, true, $"<{tag} tabindex=\"-1\">")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeTabindex(elements);

        result.Should().HaveCount(1);
        result[0].RuleId.Should().Be("interactive-not-focusable");
        result[0].Impact.Should().Be("serious");
    }

    [Fact]
    public void NonInteractiveElementWithTabindexMinusOne_ShouldReturnNoViolations()
    {
        var elements = new List<TabindexInfo>
        {
            new("div", -1, false, "<div tabindex=\"-1\">")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeTabindex(elements);

        result.Should().BeEmpty();
    }

    [Fact]
    public void MixedElements_ShouldReturnCorrectViolations()
    {
        var elements = new List<TabindexInfo>
        {
            new("a", 0, true, "<a tabindex=\"0\">"),
            new("button", 2, true, "<button tabindex=\"2\">"),
            new("input", -1, true, "<input tabindex=\"-1\">"),
            new("div", -1, false, "<div tabindex=\"-1\">")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeTabindex(elements);

        result.Should().HaveCount(2);
        result.Should().Contain(v => v.RuleId == "tabindex-positive");
        result.Should().Contain(v => v.RuleId == "interactive-not-focusable");
    }

    [Fact]
    public void ViolationDescription_ShouldMentionWCAG243ForPositiveTabindex()
    {
        var elements = new List<TabindexInfo>
        {
            new("div", 1, false, "<div tabindex=\"1\">")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeTabindex(elements);

        result[0].Description.Should().Contain("WCAG 2.4.3");
    }

    [Fact]
    public void ViolationDescription_ShouldMentionWCAG211ForNotFocusable()
    {
        var elements = new List<TabindexInfo>
        {
            new("button", -1, true, "<button tabindex=\"-1\">")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeTabindex(elements);

        result[0].Description.Should().Contain("WCAG 2.1.1");
    }
}
