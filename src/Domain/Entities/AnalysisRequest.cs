using WcagAnalyzer.Domain.Enums;

namespace WcagAnalyzer.Domain.Entities;

public class AnalysisRequest
{
    public Guid Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public AnalysisStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }

    public ICollection<AnalysisResult> Results { get; set; } = new List<AnalysisResult>();
}
