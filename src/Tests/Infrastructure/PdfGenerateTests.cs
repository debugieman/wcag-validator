using FluentAssertions;
using WcagAnalyzer.Application.Features.Analysis.Queries;
using WcagAnalyzer.Infrastructure.Services;

namespace WcagAnalyzer.Tests.Infrastructure;

// Exercises PdfReportGenerator.Generate() end-to-end — every other Pdf*Tests file in this
// project tests an internal helper (CalculateScore, GroupByPage, BuildConclusionText, ...) in
// isolation, or mocks IPdfReportGenerator entirely. Nobody actually assembles the full QuestPDF
// document. QuestPDF throws at render time ("content exceeds available area") when layout
// doesn't fit, which a pure data-transform unit test can never catch — only running the real
// Document.Create(...).GeneratePdf() pipeline can.
public class PdfGenerateTests
{
    private readonly PdfReportGenerator _generator = new();

    private static AnalysisResultDto Result(
        string ruleId,
        string impact,
        string description = "Default description",
        string? htmlElement = "<div>example</div>",
        string? helpUrl = "https://example.com/help",
        string? pageUrl = null) =>
        new(ruleId, impact, description, htmlElement, helpUrl, pageUrl);

    private static GetAnalysisByIdResult Analysis(
        IEnumerable<AnalysisResultDto> results,
        bool deepScan = false,
        string status = "Completed",
        string? errorMessage = null) =>
        new(
            Id: Guid.NewGuid(),
            Url: "https://example.com",
            Email: "user@example.com",
            Status: status,
            DeepScan: deepScan,
            CreatedAt: DateTime.UtcNow.AddMinutes(-5),
            CompletedAt: DateTime.UtcNow,
            ErrorMessage: errorMessage,
            Results: results);

    private static void ShouldBeAValidPdf(byte[] pdfBytes)
    {
        pdfBytes.Should().NotBeNull();
        pdfBytes.Length.Should().BeGreaterThan(1000);

        var header = System.Text.Encoding.ASCII.GetString(pdfBytes, 0, 5);
        header.Should().Be("%PDF-");
    }

    [Fact]
    public void NoViolations_ShouldProduceValidPdf()
    {
        var analysis = Analysis([]);

        var pdfBytes = _generator.Generate(analysis);

        ShouldBeAValidPdf(pdfBytes);
    }

    [Fact]
    public void KnownAndUnknownRules_AcrossAllImpactLevels_ShouldProduceValidPdf()
    {
        var analysis = Analysis(
        [
            Result("color-contrast", "critical"),
            Result("image-alt", "critical"),
            Result("link-name", "serious"),
            Result("region", "moderate"),
            Result("touch-target-too-small", "minor"),
            // Not present in FriendlyNames/WcagCriteria/RuleDescriptions/FixGuidance —
            // exercises every GetValueOrDefault fallback path in the real render pipeline.
            Result("some-brand-new-axe-rule", "serious", description: "A custom description for an unmapped rule."),
        ]);

        var pdfBytes = _generator.Generate(analysis);

        ShouldBeAValidPdf(pdfBytes);
    }

    [Fact]
    public void DeepScanAcrossMultiplePages_ShouldProduceValidPdf()
    {
        var analysis = Analysis(
        [
            Result("color-contrast", "critical", pageUrl: "https://example.com/"),
            Result("image-alt",      "serious",  pageUrl: "https://example.com/"),
            Result("link-name",      "serious",  pageUrl: "https://example.com/about"),
            Result("region",         "moderate", pageUrl: "https://example.com/about"),
            Result("label",          "minor",    pageUrl: "https://example.com/contact"),
        ],
        deepScan: true);

        var pdfBytes = _generator.Generate(analysis);

        ShouldBeAValidPdf(pdfBytes);
    }

    [Fact]
    public void DeepScanWithNullPageUrl_ShouldGroupUnderEmptyKeyWithoutThrowing()
    {
        // GroupByPage keys on `r.PageUrl ?? ""` — a deep-scan result with no PageUrl must
        // still render under a fallback "" group instead of throwing a null-key exception.
        var analysis = Analysis(
        [
            Result("color-contrast", "critical", pageUrl: "https://example.com/"),
            Result("image-alt",      "serious",  pageUrl: null),
        ],
        deepScan: true);

        var pdfBytes = _generator.Generate(analysis);

        ShouldBeAValidPdf(pdfBytes);
    }

    [Fact]
    public void HighOccurrenceCountForSingleRule_ShouldProduceValidPdf()
    {
        // GroupedRule.Count > 5 switches ComposeSection into the "appears N times" warning
        // branch instead of rendering individual HTML examples — exercise that path for real.
        var repeated = Enumerable.Range(0, 12)
            .Select(i => Result("color-contrast", "critical", htmlElement: $"<div class=\"item-{i}\">text</div>"));

        var analysis = Analysis(repeated);

        var pdfBytes = _generator.Generate(analysis);

        ShouldBeAValidPdf(pdfBytes);
    }

    [Fact]
    public void ManyDistinctCriticalRules_ShouldProduceValidPdf()
    {
        // Enough unique critical-impact rules to populate Top Priorities, both impact-grouped
        // sections, and multiple WCAG-criterion groups in the same render.
        var results = Enumerable.Range(0, 10)
            .Select(i => Result($"custom-critical-rule-{i}", "critical", description: $"Issue number {i}"));

        var analysis = Analysis(results);

        var pdfBytes = _generator.Generate(analysis);

        ShouldBeAValidPdf(pdfBytes);
    }

    [Fact]
    public void LongHtmlElementAndDescription_ShouldNotOverflowLayout()
    {
        // Guards against QuestPDF's "content exceeds available area" exception, which only
        // ever surfaces when real, unusually long content is actually laid out.
        var longHtml = "<div class=\"very-long-class-name-that-keeps-going-and-going\">" +
            string.Concat(Enumerable.Repeat("some inline text content ", 40)) + "</div>";
        var longDescription = string.Concat(Enumerable.Repeat(
            "This is an unusually long rule description used to stress-test PDF layout. ", 15));

        var analysis = Analysis(
        [
            Result("unmapped-long-rule", "critical", description: longDescription, htmlElement: longHtml),
        ]);

        var act = () => _generator.Generate(analysis);

        act.Should().NotThrow();
        ShouldBeAValidPdf(act());
    }

    [Fact]
    public void AllOptionalFieldsNull_ShouldProduceValidPdf()
    {
        var analysis = Analysis(
        [
            Result("color-contrast", "critical", htmlElement: null, helpUrl: null, pageUrl: null),
        ]);

        var pdfBytes = _generator.Generate(analysis);

        ShouldBeAValidPdf(pdfBytes);
    }

    [Fact]
    public void FailedStatusWithPartialResults_ShouldProduceValidPdf()
    {
        // Program.cs serves the report for Status == "Failed" when partial results exist
        // (crawl died partway through a deep scan) — the generator itself is status-agnostic,
        // but this is the real shape it receives in that case.
        var analysis = Analysis(
            [Result("color-contrast", "critical")],
            status: "Failed",
            errorMessage: "Crawl timed out after 3 subpages.");

        var pdfBytes = _generator.Generate(analysis);

        ShouldBeAValidPdf(pdfBytes);
    }
}
