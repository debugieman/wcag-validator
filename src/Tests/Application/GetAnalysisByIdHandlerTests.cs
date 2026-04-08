using FluentAssertions;
using Moq;
using WcagAnalyzer.Application.Features.Analysis.Queries;
using WcagAnalyzer.Domain.Entities;
using WcagAnalyzer.Domain.Enums;
using WcagAnalyzer.Domain.Repositories;

namespace WcagAnalyzer.Tests.Application;

public class GetAnalysisByIdHandlerTests
{
    private readonly Mock<IAnalysisRepository> _repositoryMock;
    private readonly GetAnalysisByIdHandler _handler;

    public GetAnalysisByIdHandlerTests()
    {
        _repositoryMock = new Mock<IAnalysisRepository>();
        _handler = new GetAnalysisByIdHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WhenNotFound_ShouldReturnNull()
    {
        _repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AnalysisRequest?)null);

        var result = await _handler.Handle(new GetAnalysisByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenFound_ShouldMapBasicFields()
    {
        var request = CreateRequest();
        _repositoryMock.Setup(r => r.GetByIdAsync(request.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);

        var result = await _handler.Handle(new GetAnalysisByIdQuery(request.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(request.Id);
        result.Url.Should().Be(request.Url);
        result.Email.Should().Be(request.Email);
        result.Status.Should().Be("Completed");
        result.DeepScan.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenFound_ShouldMapResults()
    {
        var request = CreateRequest();
        request.Results.Add(new AnalysisResult
        {
            RuleId = "color-contrast",
            Impact = "serious",
            Description = "Low contrast",
            HtmlElement = "<p>text</p>",
            HelpUrl = "https://example.com/help",
            PageUrl = "https://example.com/about"
        });

        _repositoryMock.Setup(r => r.GetByIdAsync(request.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);

        var result = await _handler.Handle(new GetAnalysisByIdQuery(request.Id), CancellationToken.None);

        result!.Results.Should().HaveCount(1);
        var dto = result.Results.First();
        dto.RuleId.Should().Be("color-contrast");
        dto.Impact.Should().Be("serious");
        dto.Description.Should().Be("Low contrast");
        dto.HtmlElement.Should().Be("<p>text</p>");
        dto.HelpUrl.Should().Be("https://example.com/help");
        dto.PageUrl.Should().Be("https://example.com/about");
    }

    [Fact]
    public async Task Handle_WhenFound_WithNoResults_ShouldReturnEmptyResults()
    {
        var request = CreateRequest();
        _repositoryMock.Setup(r => r.GetByIdAsync(request.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);

        var result = await _handler.Handle(new GetAnalysisByIdQuery(request.Id), CancellationToken.None);

        result!.Results.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenFound_DeepScan_ShouldMapDeepScanFlag()
    {
        var request = CreateRequest(deepScan: true);
        _repositoryMock.Setup(r => r.GetByIdAsync(request.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);

        var result = await _handler.Handle(new GetAnalysisByIdQuery(request.Id), CancellationToken.None);

        result!.DeepScan.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenFound_ShouldMapCompletedAt()
    {
        var completedAt = DateTime.UtcNow;
        var request = CreateRequest();
        request.CompletedAt = completedAt;

        _repositoryMock.Setup(r => r.GetByIdAsync(request.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);

        var result = await _handler.Handle(new GetAnalysisByIdQuery(request.Id), CancellationToken.None);

        result!.CompletedAt.Should().BeCloseTo(completedAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Handle_WhenFound_WithErrorMessage_ShouldMapErrorMessage()
    {
        var request = CreateRequest(status: AnalysisStatus.Failed);
        request.ErrorMessage = "Navigation timeout";

        _repositoryMock.Setup(r => r.GetByIdAsync(request.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);

        var result = await _handler.Handle(new GetAnalysisByIdQuery(request.Id), CancellationToken.None);

        result!.Status.Should().Be("Failed");
        result.ErrorMessage.Should().Be("Navigation timeout");
    }

    private static AnalysisRequest CreateRequest(bool deepScan = false, AnalysisStatus status = AnalysisStatus.Completed) =>
        new()
        {
            Id = Guid.NewGuid(),
            Url = "https://example.com",
            Email = "user@example.com",
            Status = status,
            DeepScan = deepScan,
            CreatedAt = DateTime.UtcNow,
            Results = []
        };
}
