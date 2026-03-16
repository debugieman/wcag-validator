using FluentAssertions;
using WcagAnalyzer.Infrastructure.Services;

namespace WcagAnalyzer.Tests.Infrastructure;

public class FieldsetLegendTests
{
    [Fact]
    public void NoFieldsets_ShouldReturnNoViolations()
    {
        var result = PlaywrightAccessibilityAnalyzer.AnalyzeFieldsetLegend(new List<FieldsetInfo>());

        result.Should().BeEmpty();
    }

    [Fact]
    public void FieldsetWithLegend_ShouldReturnNoViolations()
    {
        var fieldsets = new List<FieldsetInfo>
        {
            new(HasLegend: true, LegendText: "Shipping address", HasAriaLabel: false, HasAriaLabelledBy: false, Html: "<fieldset><legend>Shipping address</legend>")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeFieldsetLegend(fieldsets);

        result.Should().BeEmpty();
    }

    [Fact]
    public void FieldsetWithAriaLabel_ShouldReturnNoViolations()
    {
        var fieldsets = new List<FieldsetInfo>
        {
            new(HasLegend: false, LegendText: "", HasAriaLabel: true, HasAriaLabelledBy: false, Html: "<fieldset aria-label=\"Payment details\">")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeFieldsetLegend(fieldsets);

        result.Should().BeEmpty();
    }

    [Fact]
    public void FieldsetWithAriaLabelledBy_ShouldReturnNoViolations()
    {
        var fieldsets = new List<FieldsetInfo>
        {
            new(HasLegend: false, LegendText: "", HasAriaLabel: false, HasAriaLabelledBy: true, Html: "<fieldset aria-labelledby=\"section-title\">")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeFieldsetLegend(fieldsets);

        result.Should().BeEmpty();
    }

    [Fact]
    public void FieldsetWithEmptyLegend_ShouldReturnViolation()
    {
        var fieldsets = new List<FieldsetInfo>
        {
            new(HasLegend: true, LegendText: "", HasAriaLabel: false, HasAriaLabelledBy: false, Html: "<fieldset><legend></legend>")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeFieldsetLegend(fieldsets);

        result.Should().HaveCount(1);
        result[0].RuleId.Should().Be("fieldset-missing-legend");
        result[0].Impact.Should().Be("serious");
    }

    [Fact]
    public void FieldsetWithNoLegendOrAria_ShouldReturnViolation()
    {
        var fieldsets = new List<FieldsetInfo>
        {
            new(HasLegend: false, LegendText: "", HasAriaLabel: false, HasAriaLabelledBy: false, Html: "<fieldset>")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeFieldsetLegend(fieldsets);

        result.Should().HaveCount(1);
        result[0].RuleId.Should().Be("fieldset-missing-legend");
    }

    [Fact]
    public void ViolationDescription_ShouldMentionWCAG131()
    {
        var fieldsets = new List<FieldsetInfo>
        {
            new(HasLegend: false, LegendText: "", HasAriaLabel: false, HasAriaLabelledBy: false, Html: "<fieldset>")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeFieldsetLegend(fieldsets);

        result[0].Description.Should().Contain("WCAG 1.3.1");
    }
}
