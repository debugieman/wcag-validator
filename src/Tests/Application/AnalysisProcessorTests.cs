using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using WcagAnalyzer.Application.Models;
using WcagAnalyzer.Application.Services;
using WcagAnalyzer.Domain.Entities;
using WcagAnalyzer.Domain.Enums;
using WcagAnalyzer.Infrastructure.Data;
using WcagAnalyzer.Infrastructure.Repositories;

namespace WcagAnalyzer.Tests.Application;

public class AnalysisProcessorTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _context;
    private readonly AnalysisRepository _repository;
    private readonly Mock<IAccessibilityAnalyzer> _analyzerMock;
    private readonly AnalysisProcessor _processor;

    public AnalysisProcessorTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();

        _repository = new AnalysisRepository(_context);
        _analyzerMock = new Mock<IAccessibilityAnalyzer>();
        _processor = new AnalysisProcessor(_repository, _analyzerMock.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task ProcessAsync_ShouldChangeStatusToCompleted()
    {
        var request = CreatePendingRequest();
        await _repository.AddAsync(request);
        _context.ChangeTracker.Clear();

        _analyzerMock.Setup(a => a.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AccessibilityViolation>());

        await _processor.ProcessAsync(request.Id, CancellationToken.None);

        _context.ChangeTracker.Clear();
        var updated = await _repository.GetByIdAsync(request.Id);
        updated!.Status.Should().Be(AnalysisStatus.Completed);
        updated.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ProcessAsync_ShouldMapViolationsToResults()
    {
        var request = CreatePendingRequest();
        await _repository.AddAsync(request);
        _context.ChangeTracker.Clear();

        var violations = new List<AccessibilityViolation>
        {
            new()
            {
                RuleId = "color-contrast",
                Impact = "serious",
                Description = "Elements must have sufficient color contrast",
                HelpUrl = "https://dequeuniversity.com/rules/axe/4.10/color-contrast",
                HtmlElement = "<p style=\"color: #aaa\">Low contrast</p>"
            },
            new()
            {
                RuleId = "image-alt",
                Impact = "critical",
                Description = "Images must have alternate text",
                HelpUrl = "https://dequeuniversity.com/rules/axe/4.10/image-alt",
                HtmlElement = "<img src=\"photo.jpg\">"
            }
        };

        _analyzerMock.Setup(a => a.AnalyzeAsync(request.Url, It.IsAny<CancellationToken>()))
            .ReturnsAsync(violations);

        await _processor.ProcessAsync(request.Id, CancellationToken.None);

        _context.ChangeTracker.Clear();
        var updated = await _repository.GetByIdAsync(request.Id);
        updated!.Results.Should().HaveCount(2);
        updated.Results.Should().Contain(r => r.RuleId == "color-contrast" && r.Impact == "serious");
        updated.Results.Should().Contain(r => r.RuleId == "image-alt" && r.HtmlElement == "<img src=\"photo.jpg\">");
    }

    [Fact]
    public async Task ProcessAsync_WhenAnalyzerThrows_ShouldSetStatusFailed()
    {
        var request = CreatePendingRequest();
        await _repository.AddAsync(request);
        _context.ChangeTracker.Clear();

        _analyzerMock.Setup(a => a.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Navigation timeout"));

        await _processor.ProcessAsync(request.Id, CancellationToken.None);

        _context.ChangeTracker.Clear();
        var updated = await _repository.GetByIdAsync(request.Id);
        updated!.Status.Should().Be(AnalysisStatus.Failed);
        updated.ErrorMessage.Should().Contain("Navigation timeout");
    }

    [Fact]
    public async Task ProcessAsync_NoViolations_ShouldCompleteWithEmptyResults()
    {
        var request = CreatePendingRequest();
        await _repository.AddAsync(request);
        _context.ChangeTracker.Clear();

        _analyzerMock.Setup(a => a.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AccessibilityViolation>());

        await _processor.ProcessAsync(request.Id, CancellationToken.None);

        _context.ChangeTracker.Clear();
        var updated = await _repository.GetByIdAsync(request.Id);
        updated!.Status.Should().Be(AnalysisStatus.Completed);
        updated.Results.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessAsync_NonExistingId_ShouldNotThrow()
    {
        var act = async () => await _processor.ProcessAsync(Guid.NewGuid(), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ProcessAsync_WhenCancelled_ShouldThrow()
    {
        var request = CreatePendingRequest();
        await _repository.AddAsync(request);
        _context.ChangeTracker.Clear();

        _analyzerMock.Setup(a => a.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var act = async () => await _processor.ProcessAsync(request.Id, CancellationToken.None);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    private static AnalysisRequest CreatePendingRequest() => new()
    {
        Id = Guid.NewGuid(),
        Url = "https://example.com",
        Status = AnalysisStatus.Pending,
        CreatedAt = DateTime.UtcNow
    };
}
