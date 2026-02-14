using System.Text.Json;
using FluentAssertions;
using WcagAnalyzer.Infrastructure.Services;

namespace WcagAnalyzer.Tests.Infrastructure;

public class ParseViolationsTests
{
    [Fact]
    public void ParseViolations_SingleViolationSingleNode_ShouldReturnOneResult()
    {
        var json = JsonSerializer.Deserialize<JsonElement>("""
        {
            "violations": [
                {
                    "id": "color-contrast",
                    "impact": "serious",
                    "description": "Elements must have sufficient color contrast",
                    "helpUrl": "https://dequeuniversity.com/rules/axe/4.10/color-contrast",
                    "nodes": [
                        { "html": "<p style=\"color:#ccc\">Low contrast</p>" }
                    ]
                }
            ]
        }
        """);

        var result = PlaywrightAccessibilityAnalyzer.ParseViolations(json);

        result.Should().HaveCount(1);
        result[0].RuleId.Should().Be("color-contrast");
        result[0].Impact.Should().Be("serious");
        result[0].Description.Should().Be("Elements must have sufficient color contrast");
        result[0].HelpUrl.Should().Be("https://dequeuniversity.com/rules/axe/4.10/color-contrast");
        result[0].HtmlElement.Should().Contain("color:#ccc");
    }

    [Fact]
    public void ParseViolations_SingleViolationMultipleNodes_ShouldReturnOneResultPerNode()
    {
        var json = JsonSerializer.Deserialize<JsonElement>("""
        {
            "violations": [
                {
                    "id": "image-alt",
                    "impact": "critical",
                    "description": "Images must have alternate text",
                    "nodes": [
                        { "html": "<img src=\"a.jpg\">" },
                        { "html": "<img src=\"b.jpg\">" },
                        { "html": "<img src=\"c.jpg\">" }
                    ]
                }
            ]
        }
        """);

        var result = PlaywrightAccessibilityAnalyzer.ParseViolations(json);

        result.Should().HaveCount(3);
        result.Should().AllSatisfy(v =>
        {
            v.RuleId.Should().Be("image-alt");
            v.Impact.Should().Be("critical");
        });
        result[0].HtmlElement.Should().Contain("a.jpg");
        result[1].HtmlElement.Should().Contain("b.jpg");
        result[2].HtmlElement.Should().Contain("c.jpg");
    }

    [Fact]
    public void ParseViolations_MultipleViolations_ShouldReturnAll()
    {
        var json = JsonSerializer.Deserialize<JsonElement>("""
        {
            "violations": [
                {
                    "id": "color-contrast",
                    "impact": "serious",
                    "description": "Color contrast",
                    "nodes": [{ "html": "<p>text</p>" }]
                },
                {
                    "id": "image-alt",
                    "impact": "critical",
                    "description": "Image alt",
                    "nodes": [{ "html": "<img>" }]
                }
            ]
        }
        """);

        var result = PlaywrightAccessibilityAnalyzer.ParseViolations(json);

        result.Should().HaveCount(2);
        result.Should().Contain(v => v.RuleId == "color-contrast");
        result.Should().Contain(v => v.RuleId == "image-alt");
    }

    [Fact]
    public void ParseViolations_NoViolations_ShouldReturnEmptyList()
    {
        var json = JsonSerializer.Deserialize<JsonElement>("""
        {
            "violations": []
        }
        """);

        var result = PlaywrightAccessibilityAnalyzer.ParseViolations(json);

        result.Should().BeEmpty();
    }

    [Fact]
    public void ParseViolations_MissingViolationsProperty_ShouldReturnEmptyList()
    {
        var json = JsonSerializer.Deserialize<JsonElement>("""
        {
            "passes": []
        }
        """);

        var result = PlaywrightAccessibilityAnalyzer.ParseViolations(json);

        result.Should().BeEmpty();
    }

    [Fact]
    public void ParseViolations_MissingOptionalFields_ShouldUseDefaults()
    {
        var json = JsonSerializer.Deserialize<JsonElement>("""
        {
            "violations": [
                {
                    "id": "some-rule",
                    "nodes": [{ }]
                }
            ]
        }
        """);

        var result = PlaywrightAccessibilityAnalyzer.ParseViolations(json);

        result.Should().HaveCount(1);
        result[0].RuleId.Should().Be("some-rule");
        result[0].Impact.Should().BeEmpty();
        result[0].Description.Should().BeEmpty();
        result[0].HelpUrl.Should().BeNull();
        result[0].HtmlElement.Should().BeNull();
    }

    [Fact]
    public void ParseViolations_ViolationWithoutNodes_ShouldStillCreateResult()
    {
        var json = JsonSerializer.Deserialize<JsonElement>("""
        {
            "violations": [
                {
                    "id": "document-title",
                    "impact": "serious",
                    "description": "Documents must have a title"
                }
            ]
        }
        """);

        var result = PlaywrightAccessibilityAnalyzer.ParseViolations(json);

        result.Should().HaveCount(1);
        result[0].RuleId.Should().Be("document-title");
        result[0].HtmlElement.Should().BeNull();
    }
}
