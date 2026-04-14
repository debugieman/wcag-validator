using FluentAssertions;
using WcagAnalyzer.Application.Features.Analysis.Queries;
using WcagAnalyzer.Infrastructure.Services;

namespace WcagAnalyzer.Tests.Infrastructure;

public class PdfScoreTests
{
    private static AnalysisResultDto Result(string ruleId, string impact) =>
        new(ruleId, impact, "desc", null, null, null);

    [Fact]
    public void NoViolations_ShouldReturnPerfectScore()
    {
        var score = PdfReportGenerator.CalculateScore([]);

        score.Should().Be(100);
    }

    [Fact]
    public void OneCriticalRule_ShouldDeductSignificantlyLessThanBefore()
    {
        var results = new List<AnalysisResultDto> { Result("color-contrast", "critical") };

        var score = PdfReportGenerator.CalculateScore(results);

        // log2(2)/log2(112) * 40 ≈ 6 points deducted → score ~94
        score.Should().BeInRange(90, 98);
    }

    [Fact]
    public void OneSerousRule_ShouldDeductLessThanCritical()
    {
        var serious = PdfReportGenerator.CalculateScore(
            new List<AnalysisResultDto> { Result("image-alt", "serious") });

        var critical = PdfReportGenerator.CalculateScore(
            new List<AnalysisResultDto> { Result("color-contrast", "critical") });

        serious.Should().BeGreaterThan(critical);
    }

    [Fact]
    public void OneModerateRule_ShouldDeductLessThanSerious()
    {
        var moderate = PdfReportGenerator.CalculateScore(
            new List<AnalysisResultDto> { Result("label", "moderate") });

        var serious = PdfReportGenerator.CalculateScore(
            new List<AnalysisResultDto> { Result("image-alt", "serious") });

        moderate.Should().BeGreaterThan(serious);
    }

    [Fact]
    public void MultipleOccurrencesSameRule_ShouldDeductOnlyOnce()
    {
        var oneViolation = PdfReportGenerator.CalculateScore(
            new List<AnalysisResultDto> { Result("color-contrast", "critical") });

        var manyOccurrences = PdfReportGenerator.CalculateScore(
            Enumerable.Range(0, 10).Select(_ => Result("color-contrast", "critical")).ToList());

        oneViolation.Should().Be(manyOccurrences);
    }

    [Fact]
    public void LogarithmicScaling_TenRulesHurtLessThanTenTimeOneRule()
    {
        // 1 critical rule deduction
        var oneRule = 100 - PdfReportGenerator.CalculateScore(
            new List<AnalysisResultDto> { Result("rule-1", "critical") });

        // 10 unique critical rules deduction
        var tenRules = 100 - PdfReportGenerator.CalculateScore(
            Enumerable.Range(1, 10).Select(i => Result($"rule-{i}", "critical")).ToList());

        // logarithmic: 10 rules should cost less than 10× one rule
        tenRules.Should().BeLessThan(oneRule * 10);
    }

    [Fact]
    public void WikipediaScenario_ShouldScoreAbove70()
    {
        // Realistic scenario: 3 critical, 5 serious, 4 moderate, 3 minor unique rules
        var results = new List<AnalysisResultDto>
        {
            Result("color-contrast",   "critical"),
            Result("image-alt",        "critical"),
            Result("button-name",      "critical"),
            Result("link-name",        "serious"),
            Result("html-has-lang",    "serious"),
            Result("document-title",   "serious"),
            Result("meta-viewport",    "serious"),
            Result("label",            "serious"),
            Result("region",           "moderate"),
            Result("landmark-unique",  "moderate"),
            Result("list",             "moderate"),
            Result("definition-list",  "moderate"),
            Result("tabindex",         "minor"),
            Result("duplicate-id",     "minor"),
            Result("scrollable-region-focusable", "minor"),
        };

        var score = PdfReportGenerator.CalculateScore(results);

        score.Should().BeGreaterThanOrEqualTo(70);
    }

    [Fact]
    public void GoodSiteScenario_ShouldScoreAbove85()
    {
        // Well-built site: 1 critical, 2 serious, 2 moderate
        var results = new List<AnalysisResultDto>
        {
            Result("color-contrast", "critical"),
            Result("image-alt",      "serious"),
            Result("label",          "serious"),
            Result("region",         "moderate"),
            Result("list",           "moderate"),
        };

        var score = PdfReportGenerator.CalculateScore(results);

        score.Should().BeGreaterThanOrEqualTo(85);
    }

    [Fact]
    public void BadSiteScenario_ShouldScoreBelowGoodSite()
    {
        var goodSite = PdfReportGenerator.CalculateScore(new List<AnalysisResultDto>
        {
            Result("color-contrast", "critical"),
            Result("image-alt",      "serious"),
        });

        var badSite = PdfReportGenerator.CalculateScore(
            Enumerable.Range(1, 12).Select(i => Result($"rule-{i}", "critical")).ToList());

        badSite.Should().BeLessThan(goodSite);
    }

    [Fact]
    public void ScoreShouldNeverGoBelowZero()
    {
        // Extreme case: 50 unique critical rules
        var results = Enumerable.Range(1, 50)
            .Select(i => Result($"rule-{i}", "critical"))
            .ToList();

        var score = PdfReportGenerator.CalculateScore(results);

        score.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void UnknownImpact_ShouldNotAffectScore()
    {
        var withUnknown = PdfReportGenerator.CalculateScore(
            new List<AnalysisResultDto> { Result("some-rule", "unknown") });

        withUnknown.Should().Be(100);
    }
}
