namespace WcagAnalyzer.Application.Services;

public interface IRateLimiter
{
    Task<bool> IsDomainAllowedAsync(string url);
    int MaxPagesPerCrawl { get; }
}
