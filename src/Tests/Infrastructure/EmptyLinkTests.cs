using FluentAssertions;
using WcagAnalyzer.Infrastructure.Services;

namespace WcagAnalyzer.Tests.Infrastructure;

public class EmptyLinkTests
{
    [Fact]
    public void LinkWithText_ShouldReturnNoViolations()
    {
        var links = new List<LinkInfo>
        {
            new("Read more", "", "", "<a href=\"/article\">Read more</a>")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeEmptyLinks(links);

        result.Should().BeEmpty();
    }

    [Fact]
    public void LinkWithAriaLabel_ShouldReturnNoViolations()
    {
        var links = new List<LinkInfo>
        {
            new("", "Read full article about accessibility", "", "<a href=\"/article\" aria-label=\"Read full article about accessibility\">")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeEmptyLinks(links);

        result.Should().BeEmpty();
    }

    [Fact]
    public void LinkWithAriaLabelledBy_ShouldReturnNoViolations()
    {
        var links = new List<LinkInfo>
        {
            new("", "", "article-title", "<a href=\"/article\" aria-labelledby=\"article-title\">")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeEmptyLinks(links);

        result.Should().BeEmpty();
    }

    [Fact]
    public void EmptyLink_ShouldReturnViolation()
    {
        var links = new List<LinkInfo>
        {
            new("", "", "", "<a href=\"/article\"></a>")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeEmptyLinks(links);

        result.Should().HaveCount(1);
        result[0].RuleId.Should().Be("link-empty");
        result[0].Impact.Should().Be("serious");
    }

    [Fact]
    public void MultipleLinks_ShouldReturnViolationForEachEmpty()
    {
        var links = new List<LinkInfo>
        {
            new("Home", "", "", "<a href=\"/\">Home</a>"),
            new("", "", "", "<a href=\"/about\"></a>"),
            new("", "", "", "<a href=\"/contact\"></a>")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeEmptyLinks(links);

        result.Should().HaveCount(2);
    }

    [Fact]
    public void EmptyLinkList_ShouldReturnNoViolations()
    {
        var result = PlaywrightAccessibilityAnalyzer.AnalyzeEmptyLinks(new List<LinkInfo>());

        result.Should().BeEmpty();
    }

    [Fact]
    public void ViolationDescription_ShouldMentionWCAG244()
    {
        var links = new List<LinkInfo>
        {
            new("", "", "", "<a href=\"/\"></a>")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeEmptyLinks(links);

        result[0].Description.Should().Contain("WCAG 2.4.4");
    }
}
