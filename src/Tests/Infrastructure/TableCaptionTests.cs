using FluentAssertions;
using WcagAnalyzer.Infrastructure.Services;

namespace WcagAnalyzer.Tests.Infrastructure;

public class TableCaptionTests
{
    [Fact]
    public void NoTables_ShouldReturnNoViolations()
    {
        var result = PlaywrightAccessibilityAnalyzer.AnalyzeTableCaption(new List<TableInfo>());

        result.Should().BeEmpty();
    }

    [Fact]
    public void TableWithCaption_ShouldReturnNoViolations()
    {
        var tables = new List<TableInfo>
        {
            new(HasCaption: true, HasSummary: false, HasAriaLabel: false, HasAriaLabelledBy: false, Html: "<table><caption>Sales data</caption>")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeTableCaption(tables);

        result.Should().BeEmpty();
    }

    [Fact]
    public void TableWithAriaLabel_ShouldReturnNoViolations()
    {
        var tables = new List<TableInfo>
        {
            new(HasCaption: false, HasSummary: false, HasAriaLabel: true, HasAriaLabelledBy: false, Html: "<table aria-label=\"Sales data\">")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeTableCaption(tables);

        result.Should().BeEmpty();
    }

    [Fact]
    public void TableWithAriaLabelledBy_ShouldReturnNoViolations()
    {
        var tables = new List<TableInfo>
        {
            new(HasCaption: false, HasSummary: false, HasAriaLabel: false, HasAriaLabelledBy: true, Html: "<table aria-labelledby=\"title\">")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeTableCaption(tables);

        result.Should().BeEmpty();
    }

    [Fact]
    public void TableWithNoAccessibleName_ShouldReturnViolation()
    {
        var tables = new List<TableInfo>
        {
            new(HasCaption: false, HasSummary: false, HasAriaLabel: false, HasAriaLabelledBy: false, Html: "<table>")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeTableCaption(tables);

        result.Should().HaveCount(1);
        result[0].RuleId.Should().Be("table-missing-caption");
        result[0].Impact.Should().Be("moderate");
    }

    [Fact]
    public void MultipleTables_ShouldReturnViolationForEachWithoutCaption()
    {
        var tables = new List<TableInfo>
        {
            new(HasCaption: true, HasSummary: false, HasAriaLabel: false, HasAriaLabelledBy: false, Html: "<table><caption>OK</caption>"),
            new(HasCaption: false, HasSummary: false, HasAriaLabel: false, HasAriaLabelledBy: false, Html: "<table>"),
            new(HasCaption: false, HasSummary: false, HasAriaLabel: false, HasAriaLabelledBy: false, Html: "<table>")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeTableCaption(tables);

        result.Should().HaveCount(2);
    }

    [Fact]
    public void ViolationDescription_ShouldMentionWCAG131()
    {
        var tables = new List<TableInfo>
        {
            new(HasCaption: false, HasSummary: false, HasAriaLabel: false, HasAriaLabelledBy: false, Html: "<table>")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeTableCaption(tables);

        result[0].Description.Should().Contain("WCAG 1.3.1");
    }
}
