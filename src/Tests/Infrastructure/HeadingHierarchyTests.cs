using FluentAssertions;
using WcagAnalyzer.Infrastructure.Services;

namespace WcagAnalyzer.Tests.Infrastructure;

public class HeadingHierarchyTests
{
    [Fact]
    public void ValidHierarchy_ShouldReturnNoViolations()
    {
        var headings = new List<HeadingInfo>
        {
            new(1, "<h1>Title</h1>"),
            new(2, "<h2>Section</h2>"),
            new(3, "<h3>Subsection</h3>"),
            new(2, "<h2>Another section</h2>")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeHeadingHierarchy(headings);

        result.Should().BeEmpty();
    }

    [Fact]
    public void SkippedLevel_ShouldReturnViolation()
    {
        var headings = new List<HeadingInfo>
        {
            new(1, "<h1>Title</h1>"),
            new(3, "<h3>Skipped h2</h3>")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeHeadingHierarchy(headings);

        result.Should().ContainSingle(v => v.RuleId == "heading-level-skipped");
        result[0].HtmlElement.Should().Contain("<h3>");
        result[0].Impact.Should().Be("moderate");
    }

    [Fact]
    public void MultipleH1_ShouldReturnViolationsOnExtraH1s()
    {
        var headings = new List<HeadingInfo>
        {
            new(1, "<h1>First</h1>"),
            new(1, "<h1>Second</h1>"),
            new(1, "<h1>Third</h1>")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeHeadingHierarchy(headings);

        var multipleH1 = result.Where(v => v.RuleId == "heading-multiple-h1").ToList();
        multipleH1.Should().HaveCount(2);
        multipleH1[0].HtmlElement.Should().Contain("Second");
        multipleH1[1].HtmlElement.Should().Contain("Third");
    }

    [Fact]
    public void MissingH1_ShouldReturnViolation()
    {
        var headings = new List<HeadingInfo>
        {
            new(2, "<h2>Section</h2>"),
            new(3, "<h3>Subsection</h3>")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeHeadingHierarchy(headings);

        result.Should().Contain(v => v.RuleId == "heading-missing-h1");
        var missing = result.First(v => v.RuleId == "heading-missing-h1");
        missing.HtmlElement.Should().BeNull();
    }

    [Fact]
    public void FirstHeadingNotH1_ShouldReturnViolation()
    {
        var headings = new List<HeadingInfo>
        {
            new(2, "<h2>Not an h1</h2>"),
            new(1, "<h1>Title</h1>")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeHeadingHierarchy(headings);

        result.Should().Contain(v => v.RuleId == "heading-first-not-h1");
        var firstNotH1 = result.First(v => v.RuleId == "heading-first-not-h1");
        firstNotH1.HtmlElement.Should().Contain("<h2>");
    }

    [Fact]
    public void EmptyPage_ShouldReturnNoViolations()
    {
        var headings = new List<HeadingInfo>();

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeHeadingHierarchy(headings);

        result.Should().BeEmpty();
    }

    [Fact]
    public void MultipleIssuesCombined_ShouldReturnAllViolations()
    {
        // h2 first (not h1), then h4 (skips h3), no h1 at all
        var headings = new List<HeadingInfo>
        {
            new(2, "<h2>First</h2>"),
            new(4, "<h4>Skipped</h4>")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeHeadingHierarchy(headings);

        result.Should().Contain(v => v.RuleId == "heading-first-not-h1");
        result.Should().Contain(v => v.RuleId == "heading-level-skipped");
        result.Should().Contain(v => v.RuleId == "heading-missing-h1");
        result.Should().HaveCount(3);
    }

    [Fact]
    public void GoingBackToHigherLevel_ShouldNotBeViolation()
    {
        // h1 → h2 → h3 → h2 (going back up is fine)
        var headings = new List<HeadingInfo>
        {
            new(1, "<h1>Title</h1>"),
            new(2, "<h2>Section A</h2>"),
            new(3, "<h3>Detail</h3>"),
            new(2, "<h2>Section B</h2>")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeHeadingHierarchy(headings);

        result.Should().BeEmpty();
    }

    [Fact]
    public void SingleH1Only_ShouldReturnNoViolations()
    {
        var headings = new List<HeadingInfo>
        {
            new(1, "<h1>Only heading</h1>")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeHeadingHierarchy(headings);

        result.Should().BeEmpty();
    }
}
