using FluentAssertions;
using Moq;
using WcagAnalyzer.Application.Features.Analysis.Commands;
using WcagAnalyzer.Application.Services;
using WcagAnalyzer.Domain.Entities;
using WcagAnalyzer.Domain.Repositories;

namespace WcagAnalyzer.Tests.Application;

public class CreateAnalysisHandlerTests
{
    private readonly Mock<IRateLimiter> _rateLimiterMock;
    private readonly Mock<IAnalysisRepository> _repositoryMock;
    private readonly Mock<IAnalysisQueue> _queueMock;
    private readonly CreateAnalysisHandler _handler;

    public CreateAnalysisHandlerTests()
    {
        _rateLimiterMock = new Mock<IRateLimiter>();
        _repositoryMock = new Mock<IAnalysisRepository>();
        _queueMock = new Mock<IAnalysisQueue>();
        _handler = new CreateAnalysisHandler(_rateLimiterMock.Object, _repositoryMock.Object, _queueMock.Object);
    }

    [Fact]
    public async Task Handle_WhenAllowed_ShouldReturnResultWithPendingStatus()
    {
        _rateLimiterMock.Setup(r => r.IsDomainAllowedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new CreateAnalysisCommand("https://example.com", "user@example.com", false);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Status.Should().Be("Pending");
        result.IsRateLimited.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenAllowed_ShouldReturnCorrectUrlAndEmail()
    {
        _rateLimiterMock.Setup(r => r.IsDomainAllowedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new CreateAnalysisCommand("https://example.com", "user@example.com", false);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Url.Should().Be("https://example.com");
        result.Email.Should().Be("user@example.com");
    }

    [Fact]
    public async Task Handle_WhenAllowed_ShouldReturnNonEmptyId()
    {
        _rateLimiterMock.Setup(r => r.IsDomainAllowedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new CreateAnalysisCommand("https://example.com", "user@example.com", false);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Handle_WhenAllowed_ShouldSaveToRepositoryAndEnqueue()
    {
        _rateLimiterMock.Setup(r => r.IsDomainAllowedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new CreateAnalysisCommand("https://example.com", "user@example.com", false);

        await _handler.Handle(command, CancellationToken.None);

        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<AnalysisRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        _queueMock.Verify(q => q.EnqueueAsync(It.IsAny<Guid>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenAllowed_DeepScan_ShouldPassDeepScanFlag()
    {
        _rateLimiterMock.Setup(r => r.IsDomainAllowedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        AnalysisRequest? savedRequest = null;
        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<AnalysisRequest>(), It.IsAny<CancellationToken>()))
            .Callback<AnalysisRequest, CancellationToken>((req, _) => savedRequest = req);

        var command = new CreateAnalysisCommand("https://example.com", "user@example.com", DeepScan: true);

        await _handler.Handle(command, CancellationToken.None);

        savedRequest!.DeepScan.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenRateLimited_ShouldReturnIsRateLimitedTrue()
    {
        _rateLimiterMock.Setup(r => r.IsDomainAllowedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var command = new CreateAnalysisCommand("https://example.com", "user@example.com", false);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsRateLimited.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenRateLimited_ShouldNotSaveOrEnqueue()
    {
        _rateLimiterMock.Setup(r => r.IsDomainAllowedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var command = new CreateAnalysisCommand("https://example.com", "user@example.com", false);

        await _handler.Handle(command, CancellationToken.None);

        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<AnalysisRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        _queueMock.Verify(q => q.EnqueueAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenRateLimited_ShouldReturnEmptyId()
    {
        _rateLimiterMock.Setup(r => r.IsDomainAllowedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var command = new CreateAnalysisCommand("https://example.com", "user@example.com", false);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Id.Should().Be(Guid.Empty);
    }
}
