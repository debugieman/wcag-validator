using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
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
        _processor = new AnalysisProcessor(_repository);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task ProcessAsync_ShouldChangeStatusToCompleted()
    {
        var request = new AnalysisRequest
        {
            Id = Guid.NewGuid(),
            Url = "https://example.com",
            Status = AnalysisStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        await _repository.AddAsync(request);
        _context.ChangeTracker.Clear();

        await _processor.ProcessAsync(request.Id, CancellationToken.None);

        _context.ChangeTracker.Clear();
        var updated = await _repository.GetByIdAsync(request.Id);
        updated!.Status.Should().Be(AnalysisStatus.Completed);
        updated.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ProcessAsync_ShouldAddAnalysisResult()
    {
        var request = new AnalysisRequest
        {
            Id = Guid.NewGuid(),
            Url = "https://example.com",
            Status = AnalysisStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        await _repository.AddAsync(request);
        _context.ChangeTracker.Clear();

        await _processor.ProcessAsync(request.Id, CancellationToken.None);

        _context.ChangeTracker.Clear();
        var updated = await _repository.GetByIdAsync(request.Id);
        updated!.Results.Should().HaveCount(1);
        updated.Results.First().RuleId.Should().Be("color-contrast");
    }

    [Fact]
    public async Task ProcessAsync_NonExistingId_ShouldNotThrow()
    {
        var act = async () => await _processor.ProcessAsync(Guid.NewGuid(), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ProcessAsync_WhenCancelled_ShouldThrowAndNotComplete()
    {
        var request = new AnalysisRequest
        {
            Id = Guid.NewGuid(),
            Url = "https://example.com",
            Status = AnalysisStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        await _repository.AddAsync(request);
        _context.ChangeTracker.Clear();

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await _processor.ProcessAsync(request.Id, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
