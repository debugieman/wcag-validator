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
    [InlineData("https://example.com:3000/path", "example.com")]
    [InlineData("https://example.com/path#section", "example.com")]
    [InlineData("https://example.com/path?q=1#top", "example.com")]
    public void ExtractDomain_ShouldReturnExpectedDomain(string url, string expected)
    {
        var domain = AnalysisRateLimiter.ExtractDomain(url);

        domain.Should().Be(expected);
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("")]
    [InlineData("://invalid")]
    public async Task IsDomainAllowedAsync_InvalidOrMalformedUrl_ShouldReturnTrue(string url)
    {
        // Invalid URLs bypass rate limiting — no domain can be extracted
        var result = await _rateLimiter.IsDomainAllowedAsync(url);

        result.Should().BeTrue();
        _repoMock.Verify(r => r.ExistsByDomainSinceAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task IsDomainAllowedAsync_ShouldPassCorrectDomainToRepository()
    {
        _repoMock.Setup(r => r.ExistsByDomainSinceAsync(It.IsAny<string>(), It.IsAny<DateTime>()))
            .ReturnsAsync(false);

        await _rateLimiter.IsDomainAllowedAsync("https://www.example.com/some/page");

        _repoMock.Verify(r => r.ExistsByDomainSinceAsync("example.com", It.IsAny<DateTime>()), Times.Once);
    }

    [Fact]
    public async Task IsDomainAllowedAsync_ShouldPassSinceDateExactly24HoursAgo()
    {
        DateTime? capturedSince = null;
        _repoMock
            .Setup(r => r.ExistsByDomainSinceAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Callback<string, DateTime, CancellationToken>((_, since, _) => capturedSince = since)
            .ReturnsAsync(false);

        var before = DateTime.UtcNow.AddHours(-AnalysisRateLimiter.CooldownHours);
        await _rateLimiter.IsDomainAllowedAsync("https://example.com");
        var after = DateTime.UtcNow.AddHours(-AnalysisRateLimiter.CooldownHours);

        capturedSince.Should().NotBeNull();
        capturedSince!.Value.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void CooldownHours_ShouldBe24()
    {
        AnalysisRateLimiter.CooldownHours.Should().Be(24);
    }

    [Fact]
    public async Task IsDomainAllowedAsync_AnalysisOlderThan24Hours_ShouldReturnTrue()
    {
        // Repository returns false = no analysis within the window → domain allowed
        _repoMock.Setup(r => r.ExistsByDomainSinceAsync("example.com", It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _rateLimiter.IsDomainAllowedAsync("https://example.com");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsDomainAllowedAsync_AnalysisWithinLast24Hours_ShouldReturnFalse()
    {
        // Repository returns true = analysis found within the window → domain blocked
        _repoMock.Setup(r => r.ExistsByDomainSinceAsync("example.com", It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _rateLimiter.IsDomainAllowedAsync("https://example.com");

        result.Should().BeFalse();
    }
}
