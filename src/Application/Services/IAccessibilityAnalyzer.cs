using WcagAnalyzer.Application.Models;

namespace WcagAnalyzer.Application.Services;

public interface IAccessibilityAnalyzer
{
    Task<IReadOnlyList<AccessibilityViolation>> AnalyzeAsync(string url, CancellationToken cancellationToken);
}
