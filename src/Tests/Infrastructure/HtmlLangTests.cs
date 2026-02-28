using FluentAssertions;
using WcagAnalyzer.Infrastructure.Services;

namespace WcagAnalyzer.Tests.Infrastructure;

public class HtmlLangTests
{
    [Fact]
    public void MissingLang_ShouldReturnViolation()
    {
        var result = PlaywrightAccessibilityAnalyzer.AnalyzeHtmlLang("");

        result.Should().NotBeNull();
        result!.RuleId.Should().Be("html-missing-lang");
        result.Impact.Should().Be("serious");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void EmptyOrWhitespaceLang_ShouldReturnViolation(string? lang)
    {
        var result = PlaywrightAccessibilityAnalyzer.AnalyzeHtmlLang(lang!);

        result.Should().NotBeNull();
        result!.RuleId.Should().Be("html-missing-lang");
    }

    [Theory]
    [InlineData("en")]
    [InlineData("pl")]
    [InlineData("de")]
    [InlineData("en-US")]
    [InlineData("zh-Hans")]
    public void ValidLang_ShouldReturnNull(string lang)
    {
        var result = PlaywrightAccessibilityAnalyzer.AnalyzeHtmlLang(lang);

        result.Should().BeNull();
    }

    [Fact]
    public void ViolationDescription_ShouldMentionHtmlElement()
    {
        var result = PlaywrightAccessibilityAnalyzer.AnalyzeHtmlLang("");

        result!.Description.Should().Contain("<html>");
    }
}
