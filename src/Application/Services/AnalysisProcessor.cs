using WcagAnalyzer.Domain.Entities;
using WcagAnalyzer.Domain.Enums;
using WcagAnalyzer.Domain.Repositories;

namespace WcagAnalyzer.Application.Services;

public class AnalysisProcessor : IAnalysisProcessor
{
    private readonly IAnalysisRepository _repository;
    private readonly IAccessibilityAnalyzer _analyzer;

    public AnalysisProcessor(IAnalysisRepository repository, IAccessibilityAnalyzer analyzer)
    {
        _repository = repository;
        _analyzer = analyzer;
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
            var violations = await _analyzer.AnalyzeAsync(request.Url, cancellationToken);

            foreach (var violation in violations)
            {
                request.Results.Add(new AnalysisResult
                {
                    AnalysisRequestId = analysisId,
                    RuleId = violation.RuleId,
                    Impact = violation.Impact,
                    Description = violation.Description,
                    HelpUrl = violation.HelpUrl,
                    HtmlElement = violation.HtmlElement
                });
            }

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
