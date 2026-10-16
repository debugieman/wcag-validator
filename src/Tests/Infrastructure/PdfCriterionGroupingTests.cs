using FluentAssertions;
using WcagAnalyzer.Application.Features.Analysis.Queries;
using WcagAnalyzer.Infrastructure.Services;

namespace WcagAnalyzer.Tests.Infrastructure;

public class PdfCriterionGroupingTests
{
    private static AnalysisResultDto Result(string ruleId, string impact) =>
        new(ruleId, impact, "desc", null, null, null);

    [Fact]
    public void EmptyList_ShouldReturnEmptyGroups()
    {
        var groups = PdfReportGenerator.GroupByCriterion([]);

        groups.Should().BeEmpty();
    }

    [Fact]
    public void SingleViolation_ShouldReturnOneCriterionGroup()
    {
        var results = new List<AnalysisResultDto> { Result("color-contrast", "serious") };

        var groups = PdfReportGenerator.GroupByCriterion(results);

        groups.Should().HaveCount(1);
        groups[0].Criterion.Should().Be("WCAG 1.4.3");
        groups[0].CriterionName.Should().Be("Contrast (Minimum)");
        groups[0].Rules.Should().HaveCount(1);
    }

    [Fact]
    public void ViolationsWithSameCriterion_ShouldBeGroupedTogether()
    {
        var results = new List<AnalysisResultDto>
        {
            Result("input-missing-label", "critical"),
            Result("label", "serious"),
            Result("heading-level-skipped", "moderate"),
        };

        var groups = PdfReportGenerator.GroupByCriterion(results);

        var criterion131 = groups.SingleOrDefault(g => g.Criterion == "WCAG 1.3.1");
        criterion131.Should().NotBeNull();
        criterion131!.Rules.Should().HaveCount(3);
    }

    [Fact]
    public void ViolationsWithDifferentCriteria_ShouldBeInSeparateGroups()
    {
        var results = new List<AnalysisResultDto>
        {
            Result("color-contrast", "serious"),
            Result("image-alt", "critical"),
            Result("html-has-lang", "serious"),
        };

        var groups = PdfReportGenerator.GroupByCriterion(results);

        groups.Should().HaveCount(3);
        groups.Select(g => g.Criterion).Should().Contain(["WCAG 1.4.3", "WCAG 1.1.1", "WCAG 3.1.1"]);
    }

    [Fact]
    public void GroupsShouldBeOrderedByCriterionCode()
    {
        var results = new List<AnalysisResultDto>
        {
            Result("html-has-lang", "serious"),
            Result("color-contrast", "serious"),
            Result("image-alt", "critical"),
        };

        var groups = PdfReportGenerator.GroupByCriterion(results);

        groups[0].Criterion.Should().Be("WCAG 1.1.1");
        groups[1].Criterion.Should().Be("WCAG 1.4.3");
        groups[2].Criterion.Should().Be("WCAG 3.1.1");
    }

    [Fact]
    public void DuplicateRules_ShouldBeCountedCorrectly()
    {
        var results = new List<AnalysisResultDto>
        {
            Result("color-contrast", "serious"),
            Result("color-contrast", "serious"),
            Result("color-contrast", "serious"),
        };

        var groups = PdfReportGenerator.GroupByCriterion(results);

        groups.Should().HaveCount(1);
        groups[0].Rules.Should().HaveCount(1);
        groups[0].Rules[0].Rule.Count.Should().Be(3);
    }

    [Fact]
    public void RulesWithinCriterionGroup_ShouldBeOrderedByImpactSeverity()
    {
        var results = new List<AnalysisResultDto>
        {
            Result("heading-level-skipped", "moderate"),
            Result("input-missing-label", "critical"),
            Result("label", "serious"),
        };

        var groups = PdfReportGenerator.GroupByCriterion(results);
        var criterion131 = groups.Single(g => g.Criterion == "WCAG 1.3.1");

        criterion131.Rules[0].Impact.Should().Be("critical");
        criterion131.Rules[1].Impact.Should().Be("serious");
        criterion131.Rules[2].Impact.Should().Be("moderate");
    }
}
