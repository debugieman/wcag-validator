using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using Resend;
using WcagAnalyzer.Infrastructure.Services;

namespace WcagAnalyzer.Tests.Infrastructure;

public class ResendEmailSenderTests
{
    private readonly Mock<IResend> _resendMock;
    private readonly Mock<IConfiguration> _configMock;
    private EmailMessage? _capturedMessage;

    public ResendEmailSenderTests()
    {
        _resendMock = new Mock<IResend>();
        _configMock = new Mock<IConfiguration>();

        _resendMock
            .Setup(r => r.EmailSendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Callback<EmailMessage, CancellationToken>((msg, _) => _capturedMessage = msg)
            .ReturnsAsync(new ResendResponse<Guid>(Guid.NewGuid(), null!));
    }

    private ResendEmailSender CreateSender(string? configuredFrom = "WCAG Analyzer <noreply@wcag-analyzer.com>")
    {
        _configMock.Setup(c => c["Resend:From"]).Returns(configuredFrom);
        return new ResendEmailSender(_resendMock.Object, _configMock.Object);
    }

    [Fact]
    public async Task SendAnalysisReportAsync_ShouldSendToRecipient()
    {
        var sender = CreateSender();

        await sender.SendAnalysisReportAsync("user@example.com", "https://example.com", 90, [1, 2, 3]);

        _capturedMessage!.To.Should().ContainSingle().Which.ToString().Should().Be("user@example.com");
    }

    [Fact]
    public async Task SendAnalysisReportAsync_ShouldUseConfiguredFromAddress()
    {
        var sender = CreateSender(configuredFrom: "Custom Sender <custom@example.com>");

        await sender.SendAnalysisReportAsync("user@example.com", "https://example.com", 90, [1, 2, 3]);

        _capturedMessage!.From.ToString().Should().Be("Custom Sender <custom@example.com>");
    }

    [Fact]
    public async Task SendAnalysisReportAsync_WhenFromNotConfigured_ShouldFallBackToDefault()
    {
        var sender = CreateSender(configuredFrom: null);

        await sender.SendAnalysisReportAsync("user@example.com", "https://example.com", 90, [1, 2, 3]);

        _capturedMessage!.From.ToString().Should().Be("WCAG Analyzer <noreply@wcag-analyzer.com>");
    }

    [Fact]
    public async Task SendAnalysisReportAsync_ShouldIncludeSiteUrlInSubject()
    {
        var sender = CreateSender();

        await sender.SendAnalysisReportAsync("user@example.com", "https://example.com", 90, [1, 2, 3]);

        _capturedMessage!.Subject.Should().Contain("https://example.com");
    }

    [Fact]
    public async Task SendAnalysisReportAsync_ShouldAttachPdfAsBase64()
    {
        var sender = CreateSender();
        byte[] pdfBytes = [1, 2, 3, 4];

        await sender.SendAnalysisReportAsync("user@example.com", "https://example.com", 90, pdfBytes);

        var attachment = _capturedMessage!.Attachments.Should().ContainSingle().Subject;
        attachment.ContentType.Should().Be("application/pdf");
        attachment.Content.ToString().Should().Be(Convert.ToBase64String(pdfBytes));
    }

    [Theory]
    [InlineData(80, "Good")]
    [InlineData(100, "Good")]
    [InlineData(79, "Needs Improvement")]
    [InlineData(50, "Needs Improvement")]
    [InlineData(49, "Poor")]
    [InlineData(0, "Poor")]
    public async Task SendAnalysisReportAsync_ShouldLabelScoreCorrectlyInHtmlBody(int score, string expectedLabel)
    {
        var sender = CreateSender();

        await sender.SendAnalysisReportAsync("user@example.com", "https://example.com", score, [1, 2, 3]);

        _capturedMessage!.HtmlBody.Should().Contain(expectedLabel);
        _capturedMessage!.HtmlBody.Should().Contain($">{score}<");
    }

    [Fact]
    public async Task SendAnalysisReportAsync_WhenResendThrows_ShouldPropagate()
    {
        _resendMock
            .Setup(r => r.EmailSendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Resend API unavailable"));
        var sender = CreateSender();

        var act = () => sender.SendAnalysisReportAsync("user@example.com", "https://example.com", 90, [1, 2, 3]);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
