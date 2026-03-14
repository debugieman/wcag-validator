using FluentAssertions;
using WcagAnalyzer.Infrastructure.Services;

namespace WcagAnalyzer.Tests.Infrastructure;

public class ReducedMotionTests
{
    [Fact]
    public void NoAnimations_ShouldReturnNull()
    {
        var result = PlaywrightAccessibilityAnalyzer.AnalyzeReducedMotion(hasAnimationsWithoutReducedMotion: false);

        result.Should().BeNull();
    }

    [Fact]
    public void AnimationsWithReducedMotionSupport_ShouldReturnNull()
    {
        var result = PlaywrightAccessibilityAnalyzer.AnalyzeReducedMotion(hasAnimationsWithoutReducedMotion: false);

        result.Should().BeNull();
    }

    [Fact]
    public void AnimationsWithoutReducedMotion_ShouldReturnViolation()
    {
        var result = PlaywrightAccessibilityAnalyzer.AnalyzeReducedMotion(hasAnimationsWithoutReducedMotion: true);

        result.Should().NotBeNull();
        result!.RuleId.Should().Be("animation-reduced-motion-missing");
        result.Impact.Should().Be("moderate");
    }

    [Fact]
    public void ViolationDescription_ShouldMentionWCAG233()
    {
        var result = PlaywrightAccessibilityAnalyzer.AnalyzeReducedMotion(hasAnimationsWithoutReducedMotion: true);

        result!.Description.Should().Contain("WCAG 2.3.3");
    }
}
