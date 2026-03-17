using FluentAssertions;
using WcagAnalyzer.Infrastructure.Services;

namespace WcagAnalyzer.Tests.Infrastructure;

public class ErrorIdentificationTests
{
    [Fact]
    public void NoRequiredInputs_ShouldReturnNoViolations()
    {
        var result = PlaywrightAccessibilityAnalyzer.AnalyzeErrorIdentification(new List<RequiredInputInfo>());

        result.Should().BeEmpty();
    }

    [Fact]
    public void ValidFieldNotMarkedInvalid_ShouldReturnNoViolations()
    {
        var inputs = new List<RequiredInputInfo>
        {
            new("input", "", "", HasErrorMessage: false, "<input required>")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeErrorIdentification(inputs);

        result.Should().BeEmpty();
    }

    [Fact]
    public void InvalidFieldWithErrorMessage_ShouldReturnNoViolations()
    {
        var inputs = new List<RequiredInputInfo>
        {
            new("input", "true", "email-error", HasErrorMessage: true, "<input aria-invalid=\"true\" aria-describedby=\"email-error\">")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeErrorIdentification(inputs);

        result.Should().BeEmpty();
    }

    [Fact]
    public void InvalidFieldWithoutErrorMessage_ShouldReturnViolation()
    {
        var inputs = new List<RequiredInputInfo>
        {
            new("input", "true", "", HasErrorMessage: false, "<input aria-invalid=\"true\">")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeErrorIdentification(inputs);

        result.Should().HaveCount(1);
        result[0].RuleId.Should().Be("error-missing-description");
        result[0].Impact.Should().Be("serious");
    }

    [Fact]
    public void InvalidFieldWithDescribedByButNoElement_ShouldReturnViolation()
    {
        var inputs = new List<RequiredInputInfo>
        {
            new("input", "true", "nonexistent-id", HasErrorMessage: false, "<input aria-invalid=\"true\" aria-describedby=\"nonexistent-id\">")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeErrorIdentification(inputs);

        result.Should().HaveCount(1);
        result[0].RuleId.Should().Be("error-missing-description");
    }

    [Fact]
    public void ViolationDescription_ShouldContainTagAndWCAG331()
    {
        var inputs = new List<RequiredInputInfo>
        {
            new("input", "true", "", HasErrorMessage: false, "<input aria-invalid=\"true\">")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeErrorIdentification(inputs);

        result[0].Description.Should().Contain("input");
        result[0].Description.Should().Contain("WCAG 3.3.1");
    }
}
