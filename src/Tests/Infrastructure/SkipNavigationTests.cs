using FluentAssertions;
using WcagAnalyzer.Infrastructure.Services;

namespace WcagAnalyzer.Tests.Infrastructure;

public class SkipNavigationTests
{
    [Fact]
    public void HasSkipToMainContentLink_ShouldReturnNoViolation()
    {
        var links = new List<SkipLinkInfo>
        {
            new("#main", "skip to main content"),
            new("#nav", "navigation")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeSkipNavigation(links);

        result.Should().BeNull();
    }

    [Fact]
    public void HasSkipNavigationLink_ShouldReturnNoViolation()
    {
        var links = new List<SkipLinkInfo>
        {
            new("#content", "skip navigation")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeSkipNavigation(links);

        result.Should().BeNull();
    }

    [Fact]
    public void HasJumpToContentLink_ShouldReturnNoViolation()
    {
        var links = new List<SkipLinkInfo>
        {
            new("#content", "jump to content")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeSkipNavigation(links);

        result.Should().BeNull();
    }

    [Fact]
    public void HasBypassNavigationLink_ShouldReturnNoViolation()
    {
        var links = new List<SkipLinkInfo>
        {
            new("#main", "bypass navigation")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeSkipNavigation(links);

        result.Should().BeNull();
    }

    [Fact]
    public void NoAnchorLinks_ShouldReturnViolation()
    {
        var links = new List<SkipLinkInfo>();

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeSkipNavigation(links);

        result.Should().NotBeNull();
        result!.RuleId.Should().Be("skip-navigation-missing");
        result.Impact.Should().Be("serious");
    }

    [Fact]
    public void AnchorLinksWithoutSkipKeyword_ShouldReturnViolation()
    {
        var links = new List<SkipLinkInfo>
        {
            new("#section1", "go to section 1"),
            new("#contact", "contact us"),
            new("#top", "back to top")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeSkipNavigation(links);

        result.Should().NotBeNull();
        result!.RuleId.Should().Be("skip-navigation-missing");
    }

    [Fact]
    public void ViolationDescription_ShouldMentionWCAG241()
    {
        var links = new List<SkipLinkInfo>();

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeSkipNavigation(links);

        result!.Description.Should().Contain("WCAG 2.4.1");
    }
}
