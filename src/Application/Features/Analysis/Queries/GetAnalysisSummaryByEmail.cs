using MediatR;
using WcagAnalyzer.Application.Services;
using WcagAnalyzer.Domain.Repositories;

namespace WcagAnalyzer.Application.Features.Analysis.Queries;

public record GetAnalysisSummaryByEmailQuery(string Email) : IRequest<AnalysisSummaryResult?>;

public record AnalysisSummaryResult(
    Guid Id,
    string Status,
    int Score,
    int Critical,
    int Serious,
    int Moderate,
    int Minor);

public class GetAnalysisSummaryByEmailHandler : IRequestHandler<GetAnalysisSummaryByEmailQuery, AnalysisSummaryResult?>
{
    private readonly IAnalysisRepository _repository;

    public GetAnalysisSummaryByEmailHandler(IAnalysisRepository repository)
    {
        _repository = repository;
    }

    public async Task<AnalysisSummaryResult?> Handle(GetAnalysisSummaryByEmailQuery request, CancellationToken cancellationToken)
    {
        var analysis = await _repository.GetLatestByEmailAsync(request.Email, cancellationToken);
        if (analysis is null)
            return null;

        var results = analysis.Results.ToList();
        var status  = analysis.Status.ToString();

        var critical = results.Count(r => r.Impact == "critical");
        var serious  = results.Count(r => r.Impact == "serious");
        var moderate = results.Count(r => r.Impact == "moderate");
        var minor    = results.Count(r => r.Impact == "minor");
        var score    = status == "Completed"
            ? AccessibilityScorer.Calculate(results.Select(r => (r.RuleId, r.Impact)))
            : 0;

        return new AnalysisSummaryResult(analysis.Id, status, score, critical, serious, moderate, minor);
    }
}
