using FluentAssertions;
using WcagAnalyzer.Infrastructure.Services;

namespace WcagAnalyzer.Tests.Infrastructure;

public class FormInputLabelTests
{
    [Fact]
    public void InputWithLabel_ShouldReturnNoViolation()
    {
        var inputs = new List<FormInputInfo>
        {
            new("username", "text", "", "", true, "<input id=\"username\" type=\"text\">")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeFormInputLabels(inputs);

        result.Should().BeEmpty();
    }

    [Fact]
    public void InputWithAriaLabel_ShouldReturnNoViolation()
    {
        var inputs = new List<FormInputInfo>
        {
            new("", "text", "Search", "", false, "<input type=\"text\" aria-label=\"Search\">")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeFormInputLabels(inputs);

        result.Should().BeEmpty();
    }

    [Fact]
    public void InputWithAriaLabelledBy_ShouldReturnNoViolation()
    {
        var inputs = new List<FormInputInfo>
        {
            new("", "email", "", "email-label", false, "<input type=\"email\" aria-labelledby=\"email-label\">")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeFormInputLabels(inputs);

        result.Should().BeEmpty();
    }

    [Fact]
    public void InputWithoutAnyLabel_ShouldReturnViolation()
    {
        var inputs = new List<FormInputInfo>
        {
            new("", "text", "", "", false, "<input type=\"text\">")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeFormInputLabels(inputs);

        result.Should().ContainSingle(v => v.RuleId == "input-missing-label");
        result[0].Impact.Should().Be("critical");
        result[0].HtmlElement.Should().Contain("<input");
    }

    [Fact]
    public void MultipleInputsWithMissingLabels_ShouldReturnViolationForEach()
    {
        var inputs = new List<FormInputInfo>
        {
            new("", "text", "", "", false, "<input type=\"text\">"),
            new("", "email", "", "", false, "<input type=\"email\">"),
            new("name", "text", "", "", true, "<input id=\"name\" type=\"text\">")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeFormInputLabels(inputs);

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(v => v.RuleId.Should().Be("input-missing-label"));
    }

    [Fact]
    public void EmptyInputList_ShouldReturnNoViolations()
    {
        var inputs = new List<FormInputInfo>();

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeFormInputLabels(inputs);

        result.Should().BeEmpty();
    }

    [Fact]
    public void ViolationDescription_ShouldMentionWCAG131()
    {
        var inputs = new List<FormInputInfo>
        {
            new("", "text", "", "", false, "<input type=\"text\">")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeFormInputLabels(inputs);

        result[0].Description.Should().Contain("WCAG 1.3.1");
    }

    [Fact]
    public void ViolationDescription_ShouldIncludeInputType()
    {
        var inputs = new List<FormInputInfo>
        {
            new("", "email", "", "", false, "<input type=\"email\">")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeFormInputLabels(inputs);

        result[0].Description.Should().Contain("email");
    }
}
