using FluentAssertions;
using Moq;
using WcagAnalyzer.Application.Features.Analysis.Queries;
using WcagAnalyzer.Domain.Entities;
using WcagAnalyzer.Domain.Enums;
using WcagAnalyzer.Domain.Repositories;

namespace WcagAnalyzer.Tests.Application;

public class GetAllAnalysesHandlerTests
{
    private readonly Mock<IAnalysisRepository> _repositoryMock;
    private readonly GetAllAnalysesHandler _handler;

    public GetAllAnalysesHandlerTests()
    {
        _repositoryMock = new Mock<IAnalysisRepository>();
        _handler = new GetAllAnalysesHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WhenNoAnalyses_ShouldReturnEmptyList()
    {
        _repositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _handler.Handle(new GetAllAnalysesQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldMapAllFields()
    {
        var request = new AnalysisRequest
        {
            Id        = Guid.NewGuid(),
            Url       = "https://example.com",
            Email     = "user@example.com",
            Status    = AnalysisStatus.Completed,
            CreatedAt = new DateTime(2025, 6, 1, 12, 0, 0, DateTimeKind.Utc),
            Results   = []
        };

        _repositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([request]);

        var result = (await _handler.Handle(new GetAllAnalysesQuery(), CancellationToken.None)).ToList();

        result.Should().HaveCount(1);
        result[0].Id.Should().Be(request.Id);
        result[0].Url.Should().Be("https://example.com");
        result[0].Email.Should().Be("user@example.com");
        result[0].Status.Should().Be("Completed");
        result[0].CreatedAt.Should().Be(request.CreatedAt);
    }

    [Fact]
    public async Task Handle_ShouldReturnAllAnalyses()
    {
        var requests = Enumerable.Range(1, 5).Select(i => new AnalysisRequest
        {
            Id        = Guid.NewGuid(),
            Url       = $"https://example{i}.com",
            Email     = $"user{i}@example.com",
            Status    = AnalysisStatus.Completed,
            CreatedAt = DateTime.UtcNow,
            Results   = []
        }).ToList();

        _repositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(requests);

        var result = await _handler.Handle(new GetAllAnalysesQuery(), CancellationToken.None);

        result.Should().HaveCount(5);
    }

    [Fact]
    public async Task Handle_ShouldMapStatusAsString()
    {
        foreach (var (status, expected) in new[]
        {
            (AnalysisStatus.Pending,   "Pending"),
            (AnalysisStatus.Completed, "Completed"),
            (AnalysisStatus.Failed,    "Failed"),
        })
        {
            var request = new AnalysisRequest
            {
                Id = Guid.NewGuid(), Url = "https://example.com",
                Email = "u@e.com", Status = status,
                CreatedAt = DateTime.UtcNow, Results = []
            };

            _repositoryMock
                .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync([request]);

            var result = (await _handler.Handle(new GetAllAnalysesQuery(), CancellationToken.None)).Single();

            result.Status.Should().Be(expected);
        }
    }
}
