using FluentAssertions;
using WcagAnalyzer.Application.Features.Analysis.Queries;
using WcagAnalyzer.Infrastructure.Services;

namespace WcagAnalyzer.Tests.Infrastructure;

public class PdfScoreTests
{
    private static AnalysisResultDto Result(string ruleId, string impact) =>
        new(ruleId, impact, "desc", null, null);

    [Fact]
    public void NoViolations_ShouldReturnPerfectScore()
    {
        var score = PdfReportGenerator.CalculateScore([]);

        score.Should().Be(100);
    }

    [Fact]
    public void OneCriticalRule_ShouldDeduct10Points()
    {
        var results = new List<AnalysisResultDto> { Result("color-contrast", "critical") };

        var score = PdfReportGenerator.CalculateScore(results);

        score.Should().Be(90);
    }

    [Fact]
    public void OneSerousRule_ShouldDeduct7Points()
    {
        var results = new List<AnalysisResultDto> { Result("image-alt", "serious") };

        var score = PdfReportGenerator.CalculateScore(results);

        score.Should().Be(93);
    }

    [Fact]
    public void OneModerateRule_ShouldDeduct4Points()
    {
        var results = new List<AnalysisResultDto> { Result("label", "moderate") };

        var score = PdfReportGenerator.CalculateScore(results);

        score.Should().Be(96);
    }

    [Fact]
    public void OneMinorRule_ShouldDeduct1Point()
    {
        var results = new List<AnalysisResultDto> { Result("region", "minor") };

        var score = PdfReportGenerator.CalculateScore(results);

        score.Should().Be(99);
    }

    [Fact]
    public void MultipleOccurrencesSameRule_ShouldDeductOnlyOnce()
    {
        // 5 occurrences of the same rule — should count as one deduction
        var results = Enumerable.Range(0, 5)
            .Select(_ => Result("color-contrast", "critical"))
            .ToList();

        var score = PdfReportGenerator.CalculateScore(results);

        score.Should().Be(90);
    }

    [Fact]
    public void MultipleUniqueRules_ShouldDeductForEach()
    {
        // critical(-10) + serious(-7) + moderate(-4) + minor(-1) = -22 => 78
        var results = new List<AnalysisResultDto>
        {
            Result("color-contrast", "critical"),
            Result("image-alt",      "serious"),
            Result("label",          "moderate"),
            Result("region",         "minor")
        };

        var score = PdfReportGenerator.CalculateScore(results);

        score.Should().Be(78);
    }

    [Fact]
    public void ManyViolations_ScoreShouldNotGoBelowZero()
    {
        // 15 critical rules × 10 = 150 deductions → should clamp to 0
        var results = Enumerable.Range(0, 15)
            .Select(i => Result($"rule-{i}", "critical"))
            .ToList();

        var score = PdfReportGenerator.CalculateScore(results);

        score.Should().Be(0);
    }

    [Fact]
    public void UnknownImpact_ShouldDeductZeroPoints()
    {
        var results = new List<AnalysisResultDto> { Result("some-rule", "unknown") };

        var score = PdfReportGenerator.CalculateScore(results);

        score.Should().Be(100);
    }

    [Theory]
    [InlineData("critical", 90)]
    [InlineData("serious",  93)]
    [InlineData("moderate", 96)]
    [InlineData("minor",    99)]
    public void SingleRule_ScoreMatchesExpectedDeduction(string impact, int expectedScore)
    {
        var results = new List<AnalysisResultDto> { Result("any-rule", impact) };

        var score = PdfReportGenerator.CalculateScore(results);

        score.Should().Be(expectedScore);
    }
}
