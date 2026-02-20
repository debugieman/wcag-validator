using FluentAssertions;
using Moq;
using WcagAnalyzer.Application.Services;
using WcagAnalyzer.Domain.Repositories;

namespace WcagAnalyzer.Tests.Application;

public class AnalysisRateLimiterTests
{
    private readonly Mock<IAnalysisRepository> _repoMock;
    private readonly AnalysisRateLimiter _rateLimiter;

    public AnalysisRateLimiterTests()
    {
        _repoMock = new Mock<IAnalysisRepository>();
        _rateLimiter = new AnalysisRateLimiter(_repoMock.Object);
    }

    [Fact]
    public async Task IsDomainAllowedAsync_NoRecentAnalysis_ShouldReturnTrue()
    {
        _repoMock.Setup(r => r.ExistsByDomainSinceAsync(It.IsAny<string>(), It.IsAny<DateTime>()))
            .ReturnsAsync(false);

        var result = await _rateLimiter.IsDomainAllowedAsync("https://example.com");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsDomainAllowedAsync_RecentAnalysisExists_ShouldReturnFalse()
    {
        _repoMock.Setup(r => r.ExistsByDomainSinceAsync("example.com", It.IsAny<DateTime>()))
            .ReturnsAsync(true);

        var result = await _rateLimiter.IsDomainAllowedAsync("https://example.com");

        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("https://www.example.com/page", "example.com")]
    [InlineData("https://example.com", "example.com")]
    [InlineData("https://sub.example.com/path?q=1", "sub.example.com")]
    [InlineData("http://www.test.org/", "test.org")]
    public void ExtractDomain_ShouldReturnExpectedDomain(string url, string expected)
    {
        var domain = AnalysisRateLimiter.ExtractDomain(url);

        domain.Should().Be(expected);
    }

    [Fact]
    public void MaxPagesPerCrawl_ShouldReturn5()
    {
        _rateLimiter.MaxPagesPerCrawl.Should().Be(5);
    }

    [Fact]
    public async Task IsDomainAllowedAsync_ShouldPassCorrectDomainToRepository()
    {
        _repoMock.Setup(r => r.ExistsByDomainSinceAsync(It.IsAny<string>(), It.IsAny<DateTime>()))
            .ReturnsAsync(false);

        await _rateLimiter.IsDomainAllowedAsync("https://www.example.com/some/page");

        _repoMock.Verify(r => r.ExistsByDomainSinceAsync("example.com", It.IsAny<DateTime>()), Times.Once);
    }
}
