using FluentAssertions;
using WcagAnalyzer.Application.Features.Analysis.Queries;
using WcagAnalyzer.Infrastructure.Services;

namespace WcagAnalyzer.Tests.Infrastructure;

public class PdfGroupingTests
{
    private static AnalysisResultDto Result(string ruleId, string impact, string? html = null) =>
        new(ruleId, impact, "desc", html, null);

    [Fact]
    public void EmptyList_ShouldReturnEmptyGroups()
    {
        var groups = PdfReportGenerator.GroupRules([]);

        groups.Should().BeEmpty();
    }

    [Fact]
    public void SingleViolation_ShouldReturnOneGroup()
    {
        var items = new List<AnalysisResultDto> { Result("color-contrast", "serious") };

        var groups = PdfReportGenerator.GroupRules(items);

        groups.Should().HaveCount(1);
        groups[0].RuleId.Should().Be("color-contrast");
        groups[0].Count.Should().Be(1);
    }

    [Fact]
    public void MultipleOccurrencesSameRule_ShouldGroupIntoOne()
    {
        var items = Enumerable.Range(0, 10)
            .Select(_ => Result("color-contrast", "serious"))
            .ToList();

        var groups = PdfReportGenerator.GroupRules(items);

        groups.Should().HaveCount(1);
        groups[0].Count.Should().Be(10);
    }

    [Fact]
    public void DifferentRules_ShouldReturnSeparateGroups()
    {
        var items = new List<AnalysisResultDto>
        {
            Result("color-contrast", "serious"),
            Result("image-alt",      "critical"),
            Result("label",          "moderate")
        };

        var groups = PdfReportGenerator.GroupRules(items);

        groups.Should().HaveCount(3);
        groups.Select(g => g.RuleId).Should().BeEquivalentTo(["color-contrast", "image-alt", "label"]);
    }

    [Fact]
    public void FiveOrFewerOccurrences_ShouldIncludeExamples()
    {
        var items = Enumerable.Range(0, 5)
            .Select(i => Result("color-contrast", "serious", $"<p id='{i}'>text</p>"))
            .ToList();

        var groups = PdfReportGenerator.GroupRules(items);

        groups[0].Count.Should().Be(5);
        groups[0].Examples.Should().HaveCount(2); // max 2 examples
    }

    [Fact]
    public void MoreThanFiveOccurrences_ShouldStillHaveCountCorrect()
    {
        var items = Enumerable.Range(0, 20)
            .Select(i => Result("color-contrast", "serious", $"<p id='{i}'>text</p>"))
            .ToList();

        var groups = PdfReportGenerator.GroupRules(items);

        groups[0].Count.Should().Be(20);
        groups[0].Examples.Should().HaveCount(2); // still max 2
    }

    [Fact]
    public void NullHtmlElements_ShouldBeExcludedFromExamples()
    {
        var items = new List<AnalysisResultDto>
        {
            Result("color-contrast", "serious", null),
            Result("color-contrast", "serious", null),
            Result("color-contrast", "serious", "<p>text</p>")
        };

        var groups = PdfReportGenerator.GroupRules(items);

        groups[0].Count.Should().Be(3);
        groups[0].Examples.Should().HaveCount(1);
        groups[0].Examples[0].Should().Be("<p>text</p>");
    }

    [Fact]
    public void GroupUsesDescriptionFromFirstOccurrence()
    {
        var items = new List<AnalysisResultDto>
        {
            new("color-contrast", "serious", "First description", null, null),
            new("color-contrast", "serious", "Second description", null, null)
        };

        var groups = PdfReportGenerator.GroupRules(items);

        groups[0].Description.Should().Be("First description");
    }
}
