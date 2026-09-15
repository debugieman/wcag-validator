using FluentAssertions;
using Moq;
using WcagAnalyzer.Application.Features.Analysis.Queries;
using WcagAnalyzer.Domain.Entities;
using WcagAnalyzer.Domain.Enums;
using WcagAnalyzer.Domain.Repositories;

namespace WcagAnalyzer.Tests.Application;

public class GetAnalysisSummaryByEmailHandlerTests
{
    private readonly Mock<IAnalysisRepository> _repositoryMock;
    private readonly GetAnalysisSummaryByEmailHandler _handler;

    public GetAnalysisSummaryByEmailHandlerTests()
    {
        _repositoryMock = new Mock<IAnalysisRepository>();
        _handler = new GetAnalysisSummaryByEmailHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WhenEmailNotFound_ShouldReturnNull()
    {
        _repositoryMock
            .Setup(r => r.GetLatestByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AnalysisRequest?)null);

        var result = await _handler.Handle(
            new GetAnalysisSummaryByEmailQuery("unknown@example.com"), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenStatusPending_ShouldReturnScoreZero()
    {
        var request = CreateRequest(AnalysisStatus.Pending);
        request.Results.Add(new AnalysisResult { RuleId = "color-contrast", Impact = "serious" });

        _repositoryMock
            .Setup(r => r.GetLatestByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);

        var result = await _handler.Handle(
            new GetAnalysisSummaryByEmailQuery(request.Email), CancellationToken.None);

        result!.Score.Should().Be(0);
        result.Status.Should().Be("Pending");
    }

    [Fact]
    public async Task Handle_WhenCompleted_ShouldReturnNonZeroScore()
    {
        var request = CreateRequest(AnalysisStatus.Completed);

        _repositoryMock
            .Setup(r => r.GetLatestByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);

        var result = await _handler.Handle(
            new GetAnalysisSummaryByEmailQuery(request.Email), CancellationToken.None);

        result!.Score.Should().BeGreaterThan(0);
        result.Status.Should().Be("Completed");
    }

    [Fact]
    public async Task Handle_WhenCompleted_WithNoViolations_ShouldReturnPerfectScore()
    {
        var request = CreateRequest(AnalysisStatus.Completed);

        _repositoryMock
            .Setup(r => r.GetLatestByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);

        var result = await _handler.Handle(
            new GetAnalysisSummaryByEmailQuery(request.Email), CancellationToken.None);

        result!.Score.Should().Be(100);
    }

    [Fact]
    public async Task Handle_ShouldCountViolationsByImpact()
    {
        var request = CreateRequest(AnalysisStatus.Completed);
        request.Results.Add(new AnalysisResult { RuleId = "image-alt",        Impact = "critical" });
        request.Results.Add(new AnalysisResult { RuleId = "button-name",      Impact = "critical" });
        request.Results.Add(new AnalysisResult { RuleId = "color-contrast",   Impact = "serious"  });
        request.Results.Add(new AnalysisResult { RuleId = "heading-level-skipped", Impact = "moderate" });
        request.Results.Add(new AnalysisResult { RuleId = "touch-target-too-small", Impact = "minor"  });

        _repositoryMock
            .Setup(r => r.GetLatestByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);

        var result = await _handler.Handle(
            new GetAnalysisSummaryByEmailQuery(request.Email), CancellationToken.None);

        result!.Critical.Should().Be(2);
        result.Serious.Should().Be(1);
        result.Moderate.Should().Be(1);
        result.Minor.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ShouldReturnCorrectId()
    {
        var request = CreateRequest(AnalysisStatus.Completed);

        _repositoryMock
            .Setup(r => r.GetLatestByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);

        var result = await _handler.Handle(
            new GetAnalysisSummaryByEmailQuery(request.Email), CancellationToken.None);

        result!.Id.Should().Be(request.Id);
    }

    [Fact]
    public async Task Handle_WhenFailed_ShouldReturnScoreZero()
    {
        var request = CreateRequest(AnalysisStatus.Failed);

        _repositoryMock
            .Setup(r => r.GetLatestByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);

        var result = await _handler.Handle(
            new GetAnalysisSummaryByEmailQuery(request.Email), CancellationToken.None);

        result!.Score.Should().Be(0);
        result.Status.Should().Be("Failed");
    }

    private static AnalysisRequest CreateRequest(AnalysisStatus status) =>
        new()
        {
            Id        = Guid.NewGuid(),
            Url       = "https://example.com",
            Email     = "user@example.com",
            Status    = status,
            CreatedAt = DateTime.UtcNow,
            Results   = []
        };
}
