using WcagAnalyzer.Domain.Repositories;

namespace WcagAnalyzer.Application.Services;

public class AnalysisRateLimiter : IRateLimiter
{
    public const int CooldownHours = 24;
    public const int MaxPages = 5;

    private readonly IAnalysisRepository _repository;

    public AnalysisRateLimiter(IAnalysisRepository repository)
    {
        _repository = repository;
    }

    public int MaxPagesPerCrawl => MaxPages;

    public async Task<bool> IsDomainAllowedAsync(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || string.IsNullOrEmpty(uri.Host))
            return true;

        var domain = StripWww(uri.Host);
        var since = DateTime.UtcNow.AddHours(-CooldownHours);
        var exists = await _repository.ExistsByDomainSinceAsync(domain, since);
        return !exists;
    }

    internal static string ExtractDomain(string url)
    {
        var uri = new Uri(url);
        return StripWww(uri.Host);
    }

    private static string StripWww(string host)
    {
        if (host.StartsWith("www."))
            return host[4..];
        return host;
    }
}
