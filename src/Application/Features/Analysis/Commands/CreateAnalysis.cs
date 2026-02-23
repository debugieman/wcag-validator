using MediatR;
using WcagAnalyzer.Application.Services;
using WcagAnalyzer.Domain.Entities;
using WcagAnalyzer.Domain.Enums;
using WcagAnalyzer.Domain.Repositories;

namespace WcagAnalyzer.Application.Features.Analysis.Commands;

public record CreateAnalysisCommand(string Url, string Email) : IRequest<CreateAnalysisResult>;

public record CreateAnalysisResult(
    Guid Id,
    string Url,
    string Email,
    string Status,
    DateTime CreatedAt,
    bool IsRateLimited);

public class CreateAnalysisHandler : IRequestHandler<CreateAnalysisCommand, CreateAnalysisResult>
{
    private readonly IRateLimiter _rateLimiter;
    private readonly IAnalysisRepository _repository;
    private readonly IAnalysisQueue _queue;

    public CreateAnalysisHandler(IRateLimiter rateLimiter, IAnalysisRepository repository, IAnalysisQueue queue)
    {
        _rateLimiter = rateLimiter;
        _repository = repository;
        _queue = queue;
    }

    public async Task<CreateAnalysisResult> Handle(CreateAnalysisCommand request, CancellationToken cancellationToken)
    {
        if (!await _rateLimiter.IsDomainAllowedAsync(request.Url, cancellationToken))
        {
            return new CreateAnalysisResult(Guid.Empty, request.Url, request.Email, string.Empty, default, IsRateLimited: true);
        }

        var analysis = new AnalysisRequest
        {
            Id = Guid.NewGuid(),
            Url = request.Url,
            Email = request.Email,
            Status = AnalysisStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(analysis, cancellationToken);
        await _queue.EnqueueAsync(analysis.Id);

        return new CreateAnalysisResult(
            analysis.Id,
            analysis.Url,
            analysis.Email,
            analysis.Status.ToString(),
            analysis.CreatedAt,
            IsRateLimited: false);
    }
}
