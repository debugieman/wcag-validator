namespace WcagAnalyzer.Application.Services;

public interface IEmailSender
{
    Task SendAnalysisReportAsync(string to, string siteUrl, int score, byte[] pdfBytes, CancellationToken cancellationToken = default);
}
