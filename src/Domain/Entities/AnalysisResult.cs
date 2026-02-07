namespace WcagAnalyzer.Domain.Entities;

public class AnalysisResult
{
    public Guid Id { get; set; }
    public Guid AnalysisRequestId { get; set; }
    public string RuleId { get; set; } = string.Empty;
    public string Impact { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? HtmlElement { get; set; }
    public string? HelpUrl { get; set; }

    public AnalysisRequest AnalysisRequest { get; set; } = null!;
}
