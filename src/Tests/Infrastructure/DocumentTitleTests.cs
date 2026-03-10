using FluentAssertions;
using WcagAnalyzer.Infrastructure.Services;

namespace WcagAnalyzer.Tests.Infrastructure;

public class DocumentTitleTests
{
    [Fact]
    public void MissingTitle_ShouldReturnViolation()
    {
        var result = PlaywrightAccessibilityAnalyzer.AnalyzeDocumentTitle(new DocumentTitleInfo(""));

        result.Should().NotBeNull();
        result!.RuleId.Should().Be("document-title-missing");
        result.Impact.Should().Be("serious");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyOrWhitespaceTitle_ShouldReturnViolation(string title)
    {
        var result = PlaywrightAccessibilityAnalyzer.AnalyzeDocumentTitle(new DocumentTitleInfo(title));

        result.Should().NotBeNull();
        result!.RuleId.Should().Be("document-title-missing");
    }

    [Theory]
    [InlineData("My Page")]
    [InlineData("WCAG Validator")]
    [InlineData("Home")]
    public void ValidTitle_ShouldReturnNull(string title)
    {
        var result = PlaywrightAccessibilityAnalyzer.AnalyzeDocumentTitle(new DocumentTitleInfo(title));

        result.Should().BeNull();
    }

    [Fact]
    public void ViolationDescription_ShouldMentionWCAG242()
    {
        var result = PlaywrightAccessibilityAnalyzer.AnalyzeDocumentTitle(new DocumentTitleInfo(""));

        result!.Description.Should().Contain("WCAG 2.4.2");
    }
}
