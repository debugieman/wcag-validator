using FluentAssertions;
using WcagAnalyzer.Infrastructure.Services;

namespace WcagAnalyzer.Tests.Infrastructure;

public class SvgImageTests
{
    [Fact]
    public void SvgWithAriaLabel_ShouldReturnNoViolations()
    {
        var svgs = new List<SvgInfo>
        {
            new("Chart showing sales data", "", "", false, "<svg aria-label=\"Chart showing sales data\">")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeSvgImages(svgs);

        result.Should().BeEmpty();
    }

    [Fact]
    public void SvgWithAriaLabelledBy_ShouldReturnNoViolations()
    {
        var svgs = new List<SvgInfo>
        {
            new("", "chart-label", "", false, "<svg aria-labelledby=\"chart-label\">")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeSvgImages(svgs);

        result.Should().BeEmpty();
    }

    [Fact]
    public void SvgWithRoleImgAndTitle_ShouldReturnNoViolations()
    {
        var svgs = new List<SvgInfo>
        {
            new("", "", "img", true, "<svg role=\"img\"><title>Logo</title>")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeSvgImages(svgs);

        result.Should().BeEmpty();
    }

    [Fact]
    public void SvgWithRoleImgButNoTitle_ShouldReturnViolation()
    {
        var svgs = new List<SvgInfo>
        {
            new("", "", "img", false, "<svg role=\"img\">")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeSvgImages(svgs);

        result.Should().HaveCount(1);
        result[0].RuleId.Should().Be("svg-image-missing-alt");
    }

    [Fact]
    public void SvgWithNoAccessibility_ShouldReturnViolation()
    {
        var svgs = new List<SvgInfo>
        {
            new("", "", "", false, "<svg>")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeSvgImages(svgs);

        result.Should().HaveCount(1);
        result[0].RuleId.Should().Be("svg-image-missing-alt");
        result[0].Impact.Should().Be("serious");
    }

    [Fact]
    public void MultipleSvgs_ShouldReturnViolationForEachMissing()
    {
        var svgs = new List<SvgInfo>
        {
            new("Logo", "", "", false, "<svg aria-label=\"Logo\">"),
            new("", "", "", false, "<svg>"),
            new("", "", "", false, "<svg>")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeSvgImages(svgs);

        result.Should().HaveCount(2);
    }

    [Fact]
    public void EmptySvgList_ShouldReturnNoViolations()
    {
        var result = PlaywrightAccessibilityAnalyzer.AnalyzeSvgImages(new List<SvgInfo>());

        result.Should().BeEmpty();
    }

    [Fact]
    public void ViolationDescription_ShouldMentionWCAG111()
    {
        var svgs = new List<SvgInfo>
        {
            new("", "", "", false, "<svg>")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeSvgImages(svgs);

        result[0].Description.Should().Contain("WCAG 1.1.1");
    }
}
