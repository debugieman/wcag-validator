using MediatR;
using WcagAnalyzer.Domain.Repositories;

namespace WcagAnalyzer.Application.Features.Analysis.Queries;

public record GetAllAnalysesQuery : IRequest<IEnumerable<GetAllAnalysesResult>>;

public record GetAllAnalysesResult(
    Guid Id,
    string Url,
    string Email,
    string Status,
    DateTime CreatedAt);

public class GetAllAnalysesHandler : IRequestHandler<GetAllAnalysesQuery, IEnumerable<GetAllAnalysesResult>>
{
    private readonly IAnalysisRepository _repository;

    public GetAllAnalysesHandler(IAnalysisRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<GetAllAnalysesResult>> Handle(GetAllAnalysesQuery request, CancellationToken cancellationToken)
    {
        var all = await _repository.GetAllAsync(cancellationToken);
        return all.Select(a => new GetAllAnalysesResult(
            a.Id,
            a.Url,
            a.Email,
            a.Status.ToString(),
            a.CreatedAt));
    }
}
