using FluentAssertions;
using WcagAnalyzer.Application.Features.Analysis.Queries;
using WcagAnalyzer.Infrastructure.Services;

namespace WcagAnalyzer.Tests.Infrastructure;

public class PdfGroupByPageTests
{
    private static AnalysisResultDto Result(string ruleId, string? pageUrl) =>
        new(ruleId, "serious", "desc", null, null, pageUrl);

    [Fact]
    public void EmptyList_ShouldReturnEmpty()
    {
        var result = PdfReportGenerator.GroupByPage([]);

        result.Should().BeEmpty();
    }

    [Fact]
    public void SinglePage_ShouldReturnOneGroup()
    {
        var items = new List<AnalysisResultDto>
        {
            Result("color-contrast", "https://example.com"),
            Result("image-alt",      "https://example.com")
        };

        var result = PdfReportGenerator.GroupByPage(items);

        result.Should().HaveCount(1);
        result[0].Key.Should().Be("https://example.com");
        result[0].Value.Should().HaveCount(2);
    }

    [Fact]
    public void MultiplePages_ShouldReturnSeparateGroups()
    {
        var items = new List<AnalysisResultDto>
        {
            Result("color-contrast", "https://example.com/about"),
            Result("image-alt",      "https://example.com/"),
            Result("label",          "https://example.com/about")
        };

        var result = PdfReportGenerator.GroupByPage(items);

        result.Should().HaveCount(2);
        result.Select(g => g.Key).Should().BeEquivalentTo(
            ["https://example.com/", "https://example.com/about"]);
    }

    [Fact]
    public void Pages_ShouldBeOrderedAlphabetically()
    {
        var items = new List<AnalysisResultDto>
        {
            Result("rule-1", "https://example.com/zebra"),
            Result("rule-2", "https://example.com/alpha"),
            Result("rule-3", "https://example.com/middle")
        };

        var result = PdfReportGenerator.GroupByPage(items);

        result.Select(g => g.Key).Should().ContainInOrder(
            "https://example.com/alpha",
            "https://example.com/middle",
            "https://example.com/zebra");
    }

    [Fact]
    public void NullPageUrl_ShouldBeGroupedUnderEmptyString()
    {
        var items = new List<AnalysisResultDto>
        {
            Result("color-contrast", null),
            Result("image-alt",      null)
        };

        var result = PdfReportGenerator.GroupByPage(items);

        result.Should().HaveCount(1);
        result[0].Key.Should().Be("");
        result[0].Value.Should().HaveCount(2);
    }

    [Fact]
    public void MixedNullAndNonNullPageUrl_ShouldGroupSeparately()
    {
        var items = new List<AnalysisResultDto>
        {
            Result("rule-1", "https://example.com"),
            Result("rule-2", null),
            Result("rule-3", "https://example.com")
        };

        var result = PdfReportGenerator.GroupByPage(items);

        result.Should().HaveCount(2);
        result.Should().Contain(g => g.Key == "https://example.com" && g.Value.Count == 2);
        result.Should().Contain(g => g.Key == "" && g.Value.Count == 1);
    }

    [Fact]
    public void ResultsWithinGroup_ShouldPreserveAllProperties()
    {
        var items = new List<AnalysisResultDto>
        {
            new("color-contrast", "critical", "Low contrast", "<p>text</p>", "https://help.com", "https://example.com")
        };

        var result = PdfReportGenerator.GroupByPage(items);

        var item = result[0].Value[0];
        item.RuleId.Should().Be("color-contrast");
        item.Impact.Should().Be("critical");
        item.HtmlElement.Should().Be("<p>text</p>");
    }
}
