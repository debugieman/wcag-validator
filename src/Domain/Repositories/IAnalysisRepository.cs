using WcagAnalyzer.Domain.Entities;
using WcagAnalyzer.Domain.Enums;

namespace WcagAnalyzer.Domain.Repositories;

public interface IAnalysisRepository
{
    Task<AnalysisRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<AnalysisRequest>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<AnalysisRequest>> GetByStatusAsync(AnalysisStatus status, CancellationToken cancellationToken = default);
    Task AddAsync(AnalysisRequest request, CancellationToken cancellationToken = default);
    Task UpdateAsync(AnalysisRequest request, CancellationToken cancellationToken = default);
    Task<bool> ExistsByDomainSinceAsync(string domain, DateTime since, CancellationToken cancellationToken = default);
}
