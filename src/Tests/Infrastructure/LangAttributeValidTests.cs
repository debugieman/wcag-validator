using FluentAssertions;
using WcagAnalyzer.Infrastructure.Services;

namespace WcagAnalyzer.Tests.Infrastructure;

public class LangAttributeValidTests
{
    [Theory]
    [InlineData("en")]
    [InlineData("pl")]
    [InlineData("de")]
    [InlineData("en-US")]
    [InlineData("zh-Hans")]
    [InlineData("sr-Latn-RS")]
    public void ValidLangTag_ShouldReturnNull(string lang)
    {
        var result = PlaywrightAccessibilityAnalyzer.AnalyzeLangAttributeValid(lang);

        result.Should().BeNull();
    }

    [Fact]
    public void EmptyLang_ShouldReturnNull()
    {
        var result = PlaywrightAccessibilityAnalyzer.AnalyzeLangAttributeValid("");

        result.Should().BeNull();
    }

    [Theory]
    [InlineData("english")]
    [InlineData("polish")]
    [InlineData("123")]
    [InlineData("e")]
    public void InvalidLangTag_ShouldReturnViolation(string lang)
    {
        var result = PlaywrightAccessibilityAnalyzer.AnalyzeLangAttributeValid(lang);

        result.Should().NotBeNull();
        result!.RuleId.Should().Be("lang-attribute-invalid");
        result.Impact.Should().Be("serious");
    }

    [Fact]
    public void ViolationDescription_ShouldContainInvalidValue()
    {
        var result = PlaywrightAccessibilityAnalyzer.AnalyzeLangAttributeValid("polish");

        result!.Description.Should().Contain("polish");
    }

    [Fact]
    public void ViolationDescription_ShouldMentionWCAG311()
    {
        var result = PlaywrightAccessibilityAnalyzer.AnalyzeLangAttributeValid("english");

        result!.Description.Should().Contain("WCAG 3.1.1");
    }
}
