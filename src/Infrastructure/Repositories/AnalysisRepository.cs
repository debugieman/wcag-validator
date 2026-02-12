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

    public async Task<AnalysisRequest?> GetByIdAsync(Guid id)
    {
        return await _context.AnalysisRequests
            .Include(r => r.Results)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<IEnumerable<AnalysisRequest>> GetAllAsync()
    {
        return await _context.AnalysisRequests
            .Include(r => r.Results)
            .ToListAsync();
    }

    public async Task<IEnumerable<AnalysisRequest>> GetByStatusAsync(AnalysisStatus status)
    {
        return await _context.AnalysisRequests
            .Include(r => r.Results)
            .Where(r => r.Status == status)
            .ToListAsync();
    }

    public async Task AddAsync(AnalysisRequest request)
    {
        await _context.AnalysisRequests.AddAsync(request);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(AnalysisRequest request)
    {
        var entry = _context.Entry(request);
        if (entry.State == EntityState.Detached)
            _context.AnalysisRequests.Update(request);

        await _context.SaveChangesAsync();
    }
}
