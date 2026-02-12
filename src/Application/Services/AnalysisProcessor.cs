using WcagAnalyzer.Domain.Entities;
using WcagAnalyzer.Domain.Enums;
using WcagAnalyzer.Domain.Repositories;

namespace WcagAnalyzer.Application.Services;

public class AnalysisProcessor : IAnalysisProcessor
{
    private readonly IAnalysisRepository _repository;

    public AnalysisProcessor(IAnalysisRepository repository)
    {
        _repository = repository;
    }

    public async Task ProcessAsync(Guid analysisId, CancellationToken cancellationToken)
    {
        var request = await _repository.GetByIdAsync(analysisId);
        if (request is null)
            return;

        request.Status = AnalysisStatus.InProgress;
        await _repository.UpdateAsync(request);

        try
        {
            // Stub: simulate processing work
            await Task.Delay(2000, cancellationToken);

            request.Results.Add(new AnalysisResult
            {
                AnalysisRequestId = analysisId,
                RuleId = "color-contrast",
                Impact = "serious",
                Description = "Elements must have sufficient color contrast"
            });

            request.Status = AnalysisStatus.Completed;
            request.CompletedAt = DateTime.UtcNow;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            request.Status = AnalysisStatus.Failed;
            request.ErrorMessage = ex.Message;
        }

        await _repository.UpdateAsync(request);
    }
}
