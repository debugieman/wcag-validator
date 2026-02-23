namespace WcagAnalyzer.Application.Services;

public interface IRateLimiter
{
    Task<bool> IsDomainAllowedAsync(string url, CancellationToken cancellationToken = default);
    int MaxPagesPerCrawl { get; }
}
