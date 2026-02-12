using WcagAnalyzer.Domain.Entities;
using WcagAnalyzer.Domain.Enums;

namespace WcagAnalyzer.Domain.Repositories;

public interface IAnalysisRepository
{
    Task<AnalysisRequest?> GetByIdAsync(Guid id);
    Task<IEnumerable<AnalysisRequest>> GetAllAsync();
    Task<IEnumerable<AnalysisRequest>> GetByStatusAsync(AnalysisStatus status);
    Task AddAsync(AnalysisRequest request);
    Task UpdateAsync(AnalysisRequest request);
}
