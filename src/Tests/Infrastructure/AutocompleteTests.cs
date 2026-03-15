using FluentAssertions;
using WcagAnalyzer.Infrastructure.Services;

namespace WcagAnalyzer.Tests.Infrastructure;

public class AutocompleteTests
{
    [Fact]
    public void NoInputs_ShouldReturnNoViolations()
    {
        var result = PlaywrightAccessibilityAnalyzer.AnalyzeAutocomplete(new List<AutocompleteInfo>());

        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData("name")]
    [InlineData("email")]
    [InlineData("given-name")]
    [InlineData("family-name")]
    [InlineData("tel")]
    public void InputWithValidAutocomplete_ShouldReturnNoViolations(string autocomplete)
    {
        var inputs = new List<AutocompleteInfo>
        {
            new(autocomplete, "email", "<input type=\"email\" autocomplete=\"email\">")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeAutocomplete(inputs);

        result.Should().BeEmpty();
    }

    [Fact]
    public void InputWithoutAutocomplete_ShouldReturnViolation()
    {
        var inputs = new List<AutocompleteInfo>
        {
            new("", "email", "<input type=\"email\" name=\"email\">")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeAutocomplete(inputs);

        result.Should().HaveCount(1);
        result[0].RuleId.Should().Be("autocomplete-missing");
        result[0].Impact.Should().Be("serious");
    }

    [Fact]
    public void InputWithAutocompleteOff_ShouldReturnViolation()
    {
        var inputs = new List<AutocompleteInfo>
        {
            new("off", "name", "<input type=\"text\" name=\"name\" autocomplete=\"off\">")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeAutocomplete(inputs);

        result.Should().HaveCount(1);
        result[0].RuleId.Should().Be("autocomplete-missing");
    }

    [Fact]
    public void ViolationDescription_ShouldContainFieldNameAndWCAG135()
    {
        var inputs = new List<AutocompleteInfo>
        {
            new("", "email", "<input type=\"email\" name=\"email\">")
        };

        var result = PlaywrightAccessibilityAnalyzer.AnalyzeAutocomplete(inputs);

        result[0].Description.Should().Contain("email");
        result[0].Description.Should().Contain("WCAG 1.3.5");
    }
}
