using FluentAssertions;
using WcagAnalyzer.Infrastructure.Services;

namespace WcagAnalyzer.Tests.Infrastructure;

public class PageTitleDescriptiveTests
{
    [Theory]
    [InlineData("WCAG Validator - Accessibility Analysis")]
    [InlineData("Dashboard | My App")]
    [InlineData("Contact Us - Acme Corp")]
    [InlineData("Wyniki analizy")]
    public void DescriptiveTitle_ShouldReturnNull(string title)
    {
        var result = PlaywrightAccessibilityAnalyzer.AnalyzePageTitleDescriptive(title);

        result.Should().BeNull();
    }

    [Fact]
    public void EmptyTitle_ShouldReturnNull()
    {
        var result = PlaywrightAccessibilityAnalyzer.AnalyzePageTitleDescriptive("");

        result.Should().BeNull();
    }

    [Theory]
    [InlineData("Home")]
    [InlineData("home")]
    [InlineData("Index")]
    [InlineData("Untitled")]
    [InlineData("Page")]
    [InlineData("Welcome")]
    [InlineData("Default")]
    public void GenericTitle_ShouldReturnViolation(string title)
    {
        var result = PlaywrightAccessibilityAnalyzer.AnalyzePageTitleDescriptive(title);

        result.Should().NotBeNull();
        result!.RuleId.Should().Be("page-title-not-descriptive");
        result.Impact.Should().Be("moderate");
    }

    [Theory]
    [InlineData("OK")]
    [InlineData("Hi")]
    [InlineData("x")]
    public void TooShortTitle_ShouldReturnViolation(string title)
    {
        var result = PlaywrightAccessibilityAnalyzer.AnalyzePageTitleDescriptive(title);

        result.Should().NotBeNull();
        result!.RuleId.Should().Be("page-title-not-descriptive");
    }

    [Fact]
    public void ViolationDescription_ShouldContainTitleAndWCAG242()
    {
        var result = PlaywrightAccessibilityAnalyzer.AnalyzePageTitleDescriptive("Home");

        result!.Description.Should().Contain("Home");
        result.Description.Should().Contain("WCAG 2.4.2");
    }
}
