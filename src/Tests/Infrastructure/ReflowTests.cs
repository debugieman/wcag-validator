using FluentAssertions;
using WcagAnalyzer.Infrastructure.Services;

namespace WcagAnalyzer.Tests.Infrastructure;

public class ReflowTests
{
    [Fact]
    public void NoHorizontalScroll_ShouldReturnNull()
    {
        var result = PlaywrightAccessibilityAnalyzer.AnalyzeReflow(scrollWidth: 320, clientWidth: 320);

        result.Should().BeNull();
    }

    [Fact]
    public void ScrollWidthLessThanClient_ShouldReturnNull()
    {
        var result = PlaywrightAccessibilityAnalyzer.AnalyzeReflow(scrollWidth: 300, clientWidth: 320);

        result.Should().BeNull();
    }

    [Fact]
    public void ScrollWidthGreaterThanClient_ShouldReturnViolation()
    {
        var result = PlaywrightAccessibilityAnalyzer.AnalyzeReflow(scrollWidth: 800, clientWidth: 320);

        result.Should().NotBeNull();
        result!.RuleId.Should().Be("reflow-horizontal-scroll");
        result.Impact.Should().Be("critical");
    }

    [Fact]
    public void ViolationDescription_ShouldMentionWCAG1410()
    {
        var result = PlaywrightAccessibilityAnalyzer.AnalyzeReflow(scrollWidth: 500, clientWidth: 320);

        result!.Description.Should().Contain("WCAG 1.4.10");
    }
}
