using FluentAssertions;
using WcagAnalyzer.Infrastructure.Services;

namespace WcagAnalyzer.Tests.Infrastructure;

public class SelectTextareaLabelTests
{
    [Fact]
    public void NoElements_ShouldReturnNoViolations()
    {
        var result = PlaywrightAccessibilityAnalyzer.AnalyzeSelectTextareaLabels(new List<SelectTextareaInfo>());

        result.Should().BeEmpty();
    }

    [Fact]
    public void SelectWithLabel_ShouldReturnNoViolations()
    {
        var elements = new List<SelectTextareaInfo>
        {
            new("select", "", "", HasLabel: true, "<select id=\"country\">")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeSelectTextareaLabels(elements);

        result.Should().BeEmpty();
    }

    [Fact]
    public void TextareaWithAriaLabel_ShouldReturnNoViolations()
    {
        var elements = new List<SelectTextareaInfo>
        {
            new("textarea", "Your message", "", HasLabel: false, "<textarea aria-label=\"Your message\">")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeSelectTextareaLabels(elements);

        result.Should().BeEmpty();
    }

    [Fact]
    public void SelectWithAriaLabelledBy_ShouldReturnNoViolations()
    {
        var elements = new List<SelectTextareaInfo>
        {
            new("select", "", "country-label", HasLabel: false, "<select aria-labelledby=\"country-label\">")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeSelectTextareaLabels(elements);

        result.Should().BeEmpty();
    }

    [Fact]
    public void SelectWithoutLabel_ShouldReturnViolation()
    {
        var elements = new List<SelectTextareaInfo>
        {
            new("select", "", "", HasLabel: false, "<select>")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeSelectTextareaLabels(elements);

        result.Should().HaveCount(1);
        result[0].RuleId.Should().Be("select-textarea-missing-label");
        result[0].Impact.Should().Be("critical");
    }

    [Fact]
    public void TextareaWithoutLabel_ShouldReturnViolation()
    {
        var elements = new List<SelectTextareaInfo>
        {
            new("textarea", "", "", HasLabel: false, "<textarea>")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeSelectTextareaLabels(elements);

        result.Should().HaveCount(1);
        result[0].RuleId.Should().Be("select-textarea-missing-label");
    }

    [Fact]
    public void ViolationDescription_ShouldContainTagAndWCAG131()
    {
        var elements = new List<SelectTextareaInfo>
        {
            new("select", "", "", HasLabel: false, "<select>")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeSelectTextareaLabels(elements);

        result[0].Description.Should().Contain("select");
        result[0].Description.Should().Contain("WCAG 1.3.1");
    }
}
