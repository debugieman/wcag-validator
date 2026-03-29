using FluentAssertions;
using WcagAnalyzer.Infrastructure.Services;

namespace WcagAnalyzer.Tests.Infrastructure;

public class PdfTruncateHtmlTests
{
    [Fact]
    public void ShortHtml_ShouldReturnUnchanged()
    {
        var html = "<button>Click me</button>";

        var result = PdfReportGenerator.TruncateHtml(html);

        result.Should().Be(html);
    }

    [Fact]
    public void HtmlExactly120Chars_ShouldReturnUnchanged()
    {
        var html = new string('a', 120);

        var result = PdfReportGenerator.TruncateHtml(html);

        result.Should().Be(html);
        result.Should().NotEndWith("...");
    }

    [Fact]
    public void HtmlOver120Chars_ShouldTruncateAndAppendEllipsis()
    {
        var html = new string('a', 150);

        var result = PdfReportGenerator.TruncateHtml(html);

        result.Should().HaveLength(123); // 120 + "..."
        result.Should().EndWith("...");
    }

    [Fact]
    public void HtmlOver120Chars_ShouldKeepFirst120Characters()
    {
        var html = new string('a', 100) + new string('b', 50);

        var result = PdfReportGenerator.TruncateHtml(html);

        result[..100].Should().Be(new string('a', 100));
        result[100..120].Should().Be(new string('b', 20));
        result.Should().EndWith("...");
    }

    [Fact]
    public void EmptyString_ShouldReturnEmpty()
    {
        var result = PdfReportGenerator.TruncateHtml("");

        result.Should().BeEmpty();
    }
}
