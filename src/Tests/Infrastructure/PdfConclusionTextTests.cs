using FluentAssertions;
using WcagAnalyzer.Application.Features.Analysis.Queries;
using WcagAnalyzer.Infrastructure.Services;

namespace WcagAnalyzer.Tests.Infrastructure;

public class PdfConclusionTextTests
{
    private const string Url = "https://example.com";

    private static AnalysisResultDto Result(string ruleId, string impact) =>
        new(ruleId, impact, "desc", null, null, null);

    [Theory]
    [InlineData(85, "is in good shape")]
    [InlineData(100, "is in good shape")]
    [InlineData(84, "reasonable accessibility baseline")]
    [InlineData(65, "reasonable accessibility baseline")]
    [InlineData(64, "significant accessibility barriers")]
    [InlineData(40, "significant accessibility barriers")]
    [InlineData(39, "serious accessibility problems")]
    [InlineData(0, "serious accessibility problems")]
    public void Opening_ShouldMatchScoreBracket(int score, string expectedPhrase)
    {
        var text = PdfReportGenerator.BuildConclusionText([], score, Url);

        text.Opening.Should().Contain(expectedPhrase);
    }

    [Fact]
    public void Opening_ShouldIncludeHostExtractedFromUrl()
    {
        var text = PdfReportGenerator.BuildConclusionText([], 90, "https://sub.example.com/page");

        text.Opening.Should().Contain("sub.example.com");
    }

    [Fact]
    public void Opening_ShouldFallBackToRawUrl_WhenUrlIsNotAbsolute()
    {
        var text = PdfReportGenerator.BuildConclusionText([], 90, "not-a-url");

        text.Opening.Should().Contain("not-a-url");
    }

    [Fact]
    public void LegalNote_WithCriticalOrSerious_ShouldWarnOfElevatedRisk()
    {
        var results = new List<AnalysisResultDto> { Result("color-contrast", "critical") };

        var text = PdfReportGenerator.BuildConclusionText(results, 70, Url);

        text.LegalNote.Should().Contain("elevated legal risk");
        text.LegalNote.Should().Contain("1 critical and 0 serious violation,");
    }

    [Fact]
    public void LegalNote_WithMultipleCriticalAndSerious_ShouldUsePluralViolations()
    {
        var results = new List<AnalysisResultDto>
        {
            Result("color-contrast", "critical"),
            Result("button-name", "serious"),
        };

        var text = PdfReportGenerator.BuildConclusionText(results, 70, Url);

        text.LegalNote.Should().Contain("1 critical and 1 serious violations,");
    }

    [Fact]
    public void LegalNote_NoCriticalOrSerious_BelowEightyFiveScore_ShouldMentionModerateIssues()
    {
        var results = new List<AnalysisResultDto> { Result("table-missing-caption", "moderate") };

        var text = PdfReportGenerator.BuildConclusionText(results, 80, Url);

        text.LegalNote.Should().Contain("1 moderate issue remaining");
    }

    [Fact]
    public void LegalNote_NoViolationsAtAll_ShouldReportLowExposure()
    {
        var text = PdfReportGenerator.BuildConclusionText([], 100, Url);

        text.LegalNote.Should().Contain("legal exposure under the EAA and ADA is low");
    }

    [Fact]
    public void NextSteps_NoViolations_ShouldSayNoActionsRequired()
    {
        var text = PdfReportGenerator.BuildConclusionText([], 100, Url);

        text.NextSteps.Should().Be("No immediate actions required. Keep monitoring your site as content changes.");
    }

    [Fact]
    public void NextSteps_ShouldListTopThreeIssuesByImpactThenCount()
    {
        var results = new List<AnalysisResultDto>
        {
            Result("color-contrast", "critical"),
            Result("button-name", "serious"),
            Result("button-name", "serious"),
            Result("link-name", "serious"),
            Result("table-missing-caption", "moderate"),
        };

        var text = PdfReportGenerator.BuildConclusionText(results, 60, Url);

        text.NextSteps.Should().StartWith("Focus your next sprint on: Low Color Contrast, Unlabelled Button, Unlabelled Link.");
    }

    [Fact]
    public void NextSteps_FiveOrFewerUniqueRules_ShouldUseShortPhrase()
    {
        var results = new List<AnalysisResultDto> { Result("color-contrast", "critical") };

        var text = PdfReportGenerator.BuildConclusionText(results, 60, Url);

        text.NextSteps.Should().Contain("These are the highest-impact issues on your site right now.");
    }

    [Fact]
    public void NextSteps_MoreThanFiveUniqueRules_ShouldMentionDeveloperTime()
    {
        var results = new List<AnalysisResultDto>
        {
            Result("color-contrast", "critical"),
            Result("button-name", "serious"),
            Result("link-name", "serious"),
            Result("table-missing-caption", "moderate"),
            Result("reflow-horizontal-scroll", "moderate"),
            Result("touch-target-too-small", "minor"),
        };

        var text = PdfReportGenerator.BuildConclusionText(results, 60, Url);

        text.NextSteps.Should().Contain("Across 6 unique rule violations, these will deliver the highest accessibility improvement per hour of developer time.");
    }
}
