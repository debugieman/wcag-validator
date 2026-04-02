using Microsoft.Extensions.Configuration;
using Resend;
using WcagAnalyzer.Application.Services;

namespace WcagAnalyzer.Infrastructure.Services;

public class ResendEmailSender : IEmailSender
{
    private readonly IResend _resend;
    private readonly string _from;

    public ResendEmailSender(IResend resend, IConfiguration configuration)
    {
        _resend = resend;
        _from = configuration["Resend:From"] ?? "WCAG Analyzer <noreply@wcag-analyzer.com>";
    }

    public async Task SendAnalysisReportAsync(string to, string siteUrl, int score, byte[] pdfBytes, CancellationToken cancellationToken = default)
    {
        var scoreColor = score >= 80 ? "#388E3C" : score >= 50 ? "#F57C00" : "#D32F2F";
        var scoreLabel = score >= 80 ? "Good" : score >= 50 ? "Needs Improvement" : "Poor";

        var message = new EmailMessage
        {
            From = _from,
            To = [to],
            Subject = $"Your WCAG Accessibility Report — {siteUrl}",
            HtmlBody = $"""
                <!DOCTYPE html>
                <html>
                <body style="font-family: Arial, sans-serif; background: #f5f5f5; margin: 0; padding: 0;">
                  <div style="max-width: 600px; margin: 40px auto; background: white; border-radius: 8px; overflow: hidden;">

                    <div style="background: #1A237E; padding: 32px; text-align: center;">
                      <h1 style="color: white; margin: 0; font-size: 22px;">WCAG Accessibility Report</h1>
                      <p style="color: #9FA8DA; margin: 8px 0 0; font-size: 14px;">{siteUrl}</p>
                    </div>

                    <div style="padding: 32px; text-align: center;">
                      <p style="color: #546E7A; margin: 0 0 16px; font-size: 14px;">Your accessibility analysis is complete.</p>

                      <div style="display: inline-block; margin: 16px 0;">
                        <span style="font-size: 64px; font-weight: bold; color: {scoreColor};">{score}</span>
                        <span style="font-size: 18px; color: #90A4AE;">/100</span>
                        <p style="margin: 4px 0 0; font-size: 16px; font-weight: bold; color: {scoreColor};">{scoreLabel}</p>
                        <p style="margin: 2px 0 0; font-size: 12px; color: #90A4AE;">Accessibility Score</p>
                      </div>

                      <p style="color: #546E7A; font-size: 14px; margin: 24px 0 0;">
                        The full report with detailed findings is attached as a PDF.<br/>
                        Fix <strong>Critical</strong> and <strong>Serious</strong> issues first — they block access for users with disabilities.
                      </p>
                    </div>

                    <div style="background: #F8F9FF; padding: 16px 32px; text-align: center; border-top: 1px solid #E8EAF6;">
                      <p style="color: #90A4AE; font-size: 12px; margin: 0;">
                        wcag-analyzer.com · This report was generated automatically.
                      </p>
                    </div>

                  </div>
                </body>
                </html>
                """,
            Attachments =
            [
                new EmailAttachment
                {
                    Filename = $"wcag-report-{DateTime.UtcNow:yyyyMMdd}.pdf",
                    Content = Convert.ToBase64String(pdfBytes),
                    ContentType = "application/pdf"
                }
            ]
        };

        await _resend.EmailSendAsync(message, cancellationToken);
    }
}
