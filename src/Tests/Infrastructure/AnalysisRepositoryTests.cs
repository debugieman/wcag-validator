using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WcagAnalyzer.Domain.Entities;
using WcagAnalyzer.Domain.Enums;
using WcagAnalyzer.Infrastructure.Data;
using WcagAnalyzer.Infrastructure.Repositories;

namespace WcagAnalyzer.Tests.Infrastructure;

public class AnalysisRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _context;
    private readonly AnalysisRepository _repository;

    public AnalysisRepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();

        _repository = new AnalysisRepository(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task AddAsync_ShouldPersistAnalysisRequest()
    {
        var request = new AnalysisRequest
        {
            Id = Guid.NewGuid(),
            Url = "https://example.com",
            Status = AnalysisStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(request);

        var saved = await _context.AnalysisRequests.FindAsync(request.Id);
        saved.Should().NotBeNull();
        saved!.Url.Should().Be("https://example.com");
        saved.Status.Should().Be(AnalysisStatus.Pending);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ShouldReturnRequest()
    {
        var request = new AnalysisRequest
        {
            Id = Guid.NewGuid(),
            Url = "https://example.com",
            Status = AnalysisStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        await _context.AnalysisRequests.AddAsync(request);
        await _context.SaveChangesAsync();

        var result = await _repository.GetByIdAsync(request.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(request.Id);
        result.Url.Should().Be("https://example.com");
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ShouldReturnNull()
    {
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_EmptyDatabase_ShouldReturnEmptyList()
    {
        var result = await _repository.GetAllAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_WithData_ShouldReturnAllRequests()
    {
        var request1 = new AnalysisRequest
        {
            Id = Guid.NewGuid(),
            Url = "https://example1.com",
            Status = AnalysisStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        var request2 = new AnalysisRequest
        {
            Id = Guid.NewGuid(),
            Url = "https://example2.com",
            Status = AnalysisStatus.Completed,
            CreatedAt = DateTime.UtcNow
        };
        await _context.AnalysisRequests.AddRangeAsync(request1, request2);
        await _context.SaveChangesAsync();

        var result = await _repository.GetAllAsync();

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task UpdateAsync_ShouldChangeStatus()
    {
        var request = new AnalysisRequest
        {
            Id = Guid.NewGuid(),
            Url = "https://example.com",
            Status = AnalysisStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        await _context.AnalysisRequests.AddAsync(request);
        await _context.SaveChangesAsync();

        request.Status = AnalysisStatus.Completed;
        request.CompletedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(request);

        var updated = await _context.AnalysisRequests.FindAsync(request.Id);
        updated!.Status.Should().Be(AnalysisStatus.Completed);
        updated.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByStatusAsync_ShouldReturnOnlyMatchingStatus()
    {
        var pending = new AnalysisRequest
        {
            Id = Guid.NewGuid(),
            Url = "https://pending.com",
            Status = AnalysisStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        var completed = new AnalysisRequest
        {
            Id = Guid.NewGuid(),
            Url = "https://completed.com",
            Status = AnalysisStatus.Completed,
            CreatedAt = DateTime.UtcNow
        };
        var failed = new AnalysisRequest
        {
            Id = Guid.NewGuid(),
            Url = "https://failed.com",
            Status = AnalysisStatus.Failed,
            CreatedAt = DateTime.UtcNow
        };
        await _context.AnalysisRequests.AddRangeAsync(pending, completed, failed);
        await _context.SaveChangesAsync();

        var result = await _repository.GetByStatusAsync(AnalysisStatus.Pending);

        result.Should().HaveCount(1);
        result.First().Url.Should().Be("https://pending.com");
    }

    [Fact]
    public async Task GetByStatusAsync_NoMatches_ShouldReturnEmptyList()
    {
        var request = new AnalysisRequest
        {
            Id = Guid.NewGuid(),
            Url = "https://example.com",
            Status = AnalysisStatus.Completed,
            CreatedAt = DateTime.UtcNow
        };
        await _context.AnalysisRequests.AddAsync(request);
        await _context.SaveChangesAsync();

        var result = await _repository.GetByStatusAsync(AnalysisStatus.Pending);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByStatusAsync_ShouldIncludeResults()
    {
        var requestId = Guid.NewGuid();
        var request = new AnalysisRequest
        {
            Id = requestId,
            Url = "https://example.com",
            Status = AnalysisStatus.Completed,
            CreatedAt = DateTime.UtcNow,
            Results = new List<AnalysisResult>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    AnalysisRequestId = requestId,
                    RuleId = "image-alt",
                    Impact = "critical",
                    Description = "Images must have alternate text"
                }
            }
        };
        await _context.AnalysisRequests.AddAsync(request);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var result = await _repository.GetByStatusAsync(AnalysisStatus.Completed);

        result.Should().HaveCount(1);
        result.First().Results.Should().HaveCount(1);
        result.First().Results.First().RuleId.Should().Be("image-alt");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldIncludeResults()
    {
        var requestId = Guid.NewGuid();
        var request = new AnalysisRequest
        {
            Id = requestId,
            Url = "https://example.com",
            Status = AnalysisStatus.Completed,
            CreatedAt = DateTime.UtcNow,
            Results = new List<AnalysisResult>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    AnalysisRequestId = requestId,
                    RuleId = "color-contrast",
                    Impact = "serious",
                    Description = "Elements must have sufficient color contrast"
                }
            }
        };
        await _context.AnalysisRequests.AddAsync(request);
        await _context.SaveChangesAsync();

        _context.ChangeTracker.Clear();

        var result = await _repository.GetByIdAsync(requestId);

        result.Should().NotBeNull();
        result!.Results.Should().HaveCount(1);
        result.Results.First().RuleId.Should().Be("color-contrast");
    }
}
