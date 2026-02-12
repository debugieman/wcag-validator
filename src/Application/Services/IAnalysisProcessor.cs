namespace WcagAnalyzer.Application.Services;

public interface IAnalysisProcessor
{
    Task ProcessAsync(Guid analysisId, CancellationToken cancellationToken);
}
