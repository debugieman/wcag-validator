using FluentAssertions;
using WcagAnalyzer.Application.Features.Analysis.Queries;
using WcagAnalyzer.Infrastructure.Services;

namespace WcagAnalyzer.Tests.Infrastructure;

public class PdfCoverSummaryTests
{
    private static AnalysisResultDto R(string ruleId, string impact, int count = 1) =>
        new(ruleId, impact, "desc", null, null, null);

    private static List<AnalysisResultDto> Many(string ruleId, string impact, int count) =>
        Enumerable.Range(0, count).Select(_ => R(ruleId, impact)).ToList();

    [Fact]
    public void NoViolations_ShouldReturnPositiveMessage()
    {
        var summary = PdfReportGenerator.BuildCoverSummary(100, []);

        summary.Should().Contain("No accessibility violations");
    }

    [Fact]
    public void GoodScore_ShouldMentionScoreAndEAA()
    {
        var results = new List<AnalysisResultDto> { R("color-contrast", "minor") };

        var summary = PdfReportGenerator.BuildCoverSummary(92, results);

        summary.Should().Contain("92/100");
        summary.Should().Contain("EAA");
    }

    [Fact]
    public void AverageScore_ShouldMentionCriticalCount()
    {
        var results = Many("color-contrast", "critical", 5)
            .Concat(Many("image-alt", "serious", 3))
            .ToList();

        var summary = PdfReportGenerator.BuildCoverSummary(65, results);

        summary.Should().Contain("critical");
        summary.Should().Contain("EAA");
    }

    [Fact]
    public void PoorScore_ShouldMentionImmediateRemediation()
    {
        var results = Enumerable.Range(1, 10)
            .Select(i => R($"rule-{i}", "critical"))
            .ToList();

        var summary = PdfReportGenerator.BuildCoverSummary(30, results);

        summary.Should().ContainAny("Immediate", "immediate");
        summary.Should().Contain("EAA");
    }

    [Fact]
    public void ShouldIncludeTopRuleFriendlyName()
    {
        var results = new List<AnalysisResultDto>
        {
            R("color-contrast", "critical"),
            R("image-alt",      "serious"),
        };

        var summary = PdfReportGenerator.BuildCoverSummary(72, results);

        summary.Should().ContainAny("Low Color Contrast", "Missing Image Alt Text");
    }

    [Fact]
    public void ShouldNeverBeEmpty()
    {
        var summary = PdfReportGenerator.BuildCoverSummary(0, []);

        summary.Should().NotBeNullOrWhiteSpace();
    }
}
