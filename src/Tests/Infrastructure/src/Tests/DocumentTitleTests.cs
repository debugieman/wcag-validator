using FluentAssertions;
using WcagAnalyzer.Infrastructure.Services;

namespace WcagAnalyzer.Tests.Infrastructure;

public class DocumentTitleTests
{
    [Fact]
    public void MissingTitle_ShouldReturnViolation()
    {
        var titleInfo = new DocumentTitleInfo("");
        var result = PlaywrightAccessibilityAnalyzer.AnalyzeDocumentTitle(titleInfo);
        result.Should().NotBeNull();
        result!.RuleId.Should().Be("document-title-missing");
        
    }

    [Fact]
    public void WhitespaceTitle_ShouldReturnViolation()
    {
        var titleInfo = new DocumentTitleInfo(" ");
        var result = PlaywrightAccessibilityAnalyzer.AnalyzeDocumentTitle(titleInfo);
        result.Should().NotBeNull();
        result!.RuleId.Should().Be("document-title-missing");
        
    }

     [Fact]
    public void ValidDocumentTitle_ShouldReturnNull()
    {    
        
        var result = PlaywrightAccessibilityAnalyzer.AnalyzeDocumentTitle( new DocumentTitleInfo("My Page"));
        result.Should().BeNull();
    }

    private string DocumentTitleInfo(string v)
    {
        throw new NotImplementedException();
    }
}
