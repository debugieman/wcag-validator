namespace WcagAnalyzer.Application.Models;

public class AccessibilityViolation
{
    public string RuleId { get; set; } = string.Empty;
    public string Impact { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? HelpUrl { get; set; }
    public string? HtmlElement { get; set; }
}
