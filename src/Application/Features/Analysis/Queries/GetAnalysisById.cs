using MediatR;
using WcagAnalyzer.Domain.Repositories;

namespace WcagAnalyzer.Application.Features.Analysis.Queries;

public record GetAnalysisByIdQuery(Guid Id) : IRequest<GetAnalysisByIdResult?>;

public record AnalysisResultDto(
    string RuleId,
    string Impact,
    string Description,
    string? HtmlElement,
    string? HelpUrl,
    string? PageUrl);

public record GetAnalysisByIdResult(
    Guid Id,
    string Url,
    string Email,
    string Status,
    bool DeepScan,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    string? ErrorMessage,
    IEnumerable<AnalysisResultDto> Results);

public class GetAnalysisByIdHandler : IRequestHandler<GetAnalysisByIdQuery, GetAnalysisByIdResult?>
{
    private readonly IAnalysisRepository _repository;

    public GetAnalysisByIdHandler(IAnalysisRepository repository)
    {
        _repository = repository;
    }

    public async Task<GetAnalysisByIdResult?> Handle(GetAnalysisByIdQuery request, CancellationToken cancellationToken)
    {
        var analysis = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (analysis is null)
            return null;

        return new GetAnalysisByIdResult(
            analysis.Id,
            analysis.Url,
            analysis.Email,
            analysis.Status.ToString(),
            analysis.DeepScan,
            analysis.CreatedAt,
            analysis.CompletedAt,
            analysis.ErrorMessage,
            analysis.Results.Select(r => new AnalysisResultDto(
                r.RuleId,
                r.Impact,
                r.Description,
                r.HtmlElement,
                r.HelpUrl,
                r.PageUrl)));
    }
}
