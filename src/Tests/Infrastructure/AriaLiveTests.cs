using FluentAssertions;
using WcagAnalyzer.Infrastructure.Services;

namespace WcagAnalyzer.Tests.Infrastructure;

public class AriaLiveTests
{
    [Fact]
    public void NoAlertElements_ShouldReturnNull()
    {
        var result = PlaywrightAccessibilityAnalyzer.AnalyzeAriaLive(hasAlertElements: false, hasLiveRegion: false);

        result.Should().BeNull();
    }

    [Fact]
    public void AlertElementsWithAriaLive_ShouldReturnNull()
    {
        var result = PlaywrightAccessibilityAnalyzer.AnalyzeAriaLive(hasAlertElements: true, hasLiveRegion: true);

        result.Should().BeNull();
    }

    [Fact]
    public void AlertElementsWithoutAriaLive_ShouldReturnViolation()
    {
        var result = PlaywrightAccessibilityAnalyzer.AnalyzeAriaLive(hasAlertElements: true, hasLiveRegion: false);

        result.Should().NotBeNull();
        result!.RuleId.Should().Be("aria-live-missing");
        result.Impact.Should().Be("serious");
    }

    [Fact]
    public void ViolationDescription_ShouldMentionWCAG413()
    {
        var result = PlaywrightAccessibilityAnalyzer.AnalyzeAriaLive(hasAlertElements: true, hasLiveRegion: false);

        result!.Description.Should().Contain("WCAG 4.1.3");
    }
}
