using Microsoft.EntityFrameworkCore;
using WcagAnalyzer.Domain.Entities;
using WcagAnalyzer.Domain.Enums;
using WcagAnalyzer.Domain.Repositories;
using WcagAnalyzer.Infrastructure.Data;

namespace WcagAnalyzer.Infrastructure.Repositories;

public class AnalysisRepository : IAnalysisRepository
{
    private readonly AppDbContext _context;

    public AnalysisRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<AnalysisRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.AnalysisRequests
            .Include(r => r.Results)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<AnalysisRequest>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.AnalysisRequests
            .Include(r => r.Results)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AnalysisRequest>> GetByStatusAsync(AnalysisStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.AnalysisRequests
            .Include(r => r.Results)
            .Where(r => r.Status == status)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(AnalysisRequest request, CancellationToken cancellationToken = default)
    {
        await _context.AnalysisRequests.AddAsync(request, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(AnalysisRequest request, CancellationToken cancellationToken = default)
    {
        var entry = _context.Entry(request);
        if (entry.State == EntityState.Detached)
            _context.AnalysisRequests.Update(request);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsByDomainSinceAsync(string domain, DateTime since, CancellationToken cancellationToken = default)
    {
        return await _context.AnalysisRequests
            .AnyAsync(r => r.Url.Contains(domain) && r.CreatedAt >= since, cancellationToken);
    }
}
