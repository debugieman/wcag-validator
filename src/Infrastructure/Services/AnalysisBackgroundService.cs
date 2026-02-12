using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WcagAnalyzer.Application.Services;

namespace WcagAnalyzer.Infrastructure.Services;

public class AnalysisBackgroundService : BackgroundService
{
    private readonly IAnalysisQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AnalysisBackgroundService> _logger;

    public AnalysisBackgroundService(
        IAnalysisQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<AnalysisBackgroundService> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AnalysisBackgroundService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var analysisId = await _queue.DequeueAsync(stoppingToken);
                _logger.LogInformation("Processing analysis {AnalysisId}", analysisId);

                using var scope = _scopeFactory.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<IAnalysisProcessor>();
                await processor.ProcessAsync(analysisId, stoppingToken);

                _logger.LogInformation("Completed analysis {AnalysisId}", analysisId);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing analysis");
            }
        }

        _logger.LogInformation("AnalysisBackgroundService stopped");
    }
}
