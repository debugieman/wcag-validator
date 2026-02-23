namespace WcagAnalyzer.Application.Services;

public interface IAnalysisQueue
{
    ValueTask EnqueueAsync(Guid analysisId, CancellationToken cancellationToken = default);
    ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken);
}
