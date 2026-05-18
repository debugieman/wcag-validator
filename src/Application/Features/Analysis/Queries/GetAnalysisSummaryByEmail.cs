using MediatR;
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
        var score    = status == "Completed" ? CalculateScore(results) : 0;

        return new AnalysisSummaryResult(analysis.Id, status, score, critical, serious, moderate, minor);
    }

    private static int CalculateScore(IEnumerable<Domain.Entities.AnalysisResult> results)
    {
        const int totalRules = 111;

        var uniqueByImpact = results
            .GroupBy(r => r.RuleId)
            .GroupBy(g => g.First().Impact)
            .ToDictionary(g => g.Key, g => g.Count());

        int critical = uniqueByImpact.GetValueOrDefault("critical", 0);
        int serious  = uniqueByImpact.GetValueOrDefault("serious",  0);
        int moderate = uniqueByImpact.GetValueOrDefault("moderate", 0);
        int minor    = uniqueByImpact.GetValueOrDefault("minor",    0);

        double logBase = Math.Log2(totalRules + 1);
        double penalty =
            40 * Math.Log2(1 + critical)  / logBase +
            25 * Math.Log2(1 + serious)   / logBase +
            12 * Math.Log2(1 + moderate)  / logBase +
             4 * Math.Log2(1 + minor)     / logBase;

        return (int)Math.Round(Math.Max(0, 100 - penalty));
    }
}
