using WcagAnalyzer.Domain.Entities;

namespace WcagAnalyzer.Domain.Repositories;

public interface IAnalysisRepository
{
    Task<AnalysisRequest?> GetByIdAsync(Guid id);
    Task<IEnumerable<AnalysisRequest>> GetAllAsync();
    Task AddAsync(AnalysisRequest request);
    Task UpdateAsync(AnalysisRequest request);
}
