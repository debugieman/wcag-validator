using WcagAnalyzer.Application.Features.Analysis.Queries;
using WcagAnalyzer.Domain.Entities;
using WcagAnalyzer.Domain.Enums;
using WcagAnalyzer.Domain.Repositories;

namespace WcagAnalyzer.Application.Services;

public class AnalysisProcessor : IAnalysisProcessor
{
    private readonly IAnalysisRepository _repository;
    private readonly IAccessibilityAnalyzer _analyzer;
    private readonly IPdfReportGenerator _pdfGenerator;
    private readonly IEmailSender _emailSender;

    public AnalysisProcessor(
        IAnalysisRepository repository,
        IAccessibilityAnalyzer analyzer,
        IPdfReportGenerator pdfGenerator,
        IEmailSender emailSender)
    {
        _repository = repository;
        _analyzer = analyzer;
        _pdfGenerator = pdfGenerator;
        _emailSender = emailSender;
    }

    public async Task ProcessAsync(Guid analysisId, CancellationToken cancellationToken)
    {
        var request = await _repository.GetByIdAsync(analysisId, cancellationToken);
        if (request is null)
            return;

        request.Status = AnalysisStatus.InProgress;
        await _repository.UpdateAsync(request, cancellationToken);

        try
        {
            var violations = await _analyzer.AnalyzeAsync(request.Url, request.DeepScan, cancellationToken);

            foreach (var violation in violations)
            {
                request.Results.Add(new AnalysisResult
                {
                    AnalysisRequestId = analysisId,
                    RuleId = violation.RuleId,
                    Impact = violation.Impact,
                    Description = violation.Description,
                    HelpUrl = violation.HelpUrl,
                    HtmlElement = violation.HtmlElement,
                    PageUrl = violation.PageUrl
                });
            }

            request.Status = AnalysisStatus.Completed;
            request.CompletedAt = DateTime.UtcNow;
            await _repository.UpdateAsync(request, cancellationToken);

            var analysisResult = MapToResult(request);
            var pdfBytes = _pdfGenerator.Generate(analysisResult);
            var score = _pdfGenerator.CalculateScore(analysisResult);
            await _emailSender.SendAnalysisReportAsync(request.Email, request.Url, score, pdfBytes, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            request.Status = AnalysisStatus.Failed;
            request.ErrorMessage = ex.Message;
            await _repository.UpdateAsync(request, cancellationToken);
        }
    }

    private static GetAnalysisByIdResult MapToResult(AnalysisRequest request) =>
        new(
            request.Id,
            request.Url,
            request.Email,
            request.Status.ToString(),
            request.DeepScan,
            request.CreatedAt,
            request.CompletedAt,
            request.ErrorMessage,
            request.Results.Select(r => new AnalysisResultDto(
                r.RuleId,
                r.Impact,
                r.Description,
                r.HtmlElement,
                r.HelpUrl,
                r.PageUrl))
        );
}
