using FluentAssertions;
using WcagAnalyzer.Infrastructure.Services;

namespace WcagAnalyzer.Tests.Infrastructure;

public class MetaViewportTests
{
    [Fact]
    public void NoViewportMeta_ShouldReturnNull()
    {
        var result = PlaywrightAccessibilityAnalyzer.AnalyzeMetaViewport("");

        result.Should().BeNull();
    }

    [Theory]
    [InlineData("width=device-width, initial-scale=1")]
    [InlineData("width=device-width, initial-scale=1.0")]
    [InlineData("width=device-width")]
    public void ViewportWithoutZoomDisabled_ShouldReturnNull(string content)
    {
        var result = PlaywrightAccessibilityAnalyzer.AnalyzeMetaViewport(content);

        result.Should().BeNull();
    }

    [Theory]
    [InlineData("width=device-width, user-scalable=no")]
    [InlineData("width=device-width, initial-scale=1, user-scalable=no")]
    public void ViewportWithUserScalableNo_ShouldReturnViolation(string content)
    {
        var result = PlaywrightAccessibilityAnalyzer.AnalyzeMetaViewport(content);

        result.Should().NotBeNull();
        result!.RuleId.Should().Be("meta-viewport-zoom-disabled");
        result.Impact.Should().Be("critical");
    }

    [Theory]
    [InlineData("width=device-width, user-scalable=0")]
    [InlineData("width=device-width, initial-scale=1, user-scalable=0")]
    public void ViewportWithUserScalableZero_ShouldReturnViolation(string content)
    {
        var result = PlaywrightAccessibilityAnalyzer.AnalyzeMetaViewport(content);

        result.Should().NotBeNull();
        result!.RuleId.Should().Be("meta-viewport-zoom-disabled");
    }

    [Fact]
    public void ViolationDescription_ShouldMentionWCAG144()
    {
        var result = PlaywrightAccessibilityAnalyzer.AnalyzeMetaViewport("user-scalable=no");

        result!.Description.Should().Contain("WCAG 1.4.4");
    }
}
