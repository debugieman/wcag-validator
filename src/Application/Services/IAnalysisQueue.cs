namespace WcagAnalyzer.Application.Services;

public interface IAnalysisQueue
{
    ValueTask EnqueueAsync(Guid analysisId);
    ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken);
}
