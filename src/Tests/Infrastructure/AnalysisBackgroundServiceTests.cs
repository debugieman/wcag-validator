using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using WcagAnalyzer.Application.Services;
using WcagAnalyzer.Infrastructure.Services;

namespace WcagAnalyzer.Tests.Infrastructure;

public class AnalysisBackgroundServiceTests
{
    private readonly Mock<IAnalysisProcessor> _processorMock;
    private readonly AnalysisQueue _queue;
    private readonly AnalysisBackgroundService _service;

    public AnalysisBackgroundServiceTests()
    {
        _queue = new AnalysisQueue();
        _processorMock = new Mock<IAnalysisProcessor>();

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(sp => sp.GetService(typeof(IAnalysisProcessor)))
            .Returns(_processorMock.Object);

        var scope = new Mock<IServiceScope>();
        scope.Setup(s => s.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(f => f.CreateScope()).Returns(scope.Object);

        var logger = new Mock<ILogger<AnalysisBackgroundService>>();

        _service = new AnalysisBackgroundService(_queue, scopeFactory.Object, logger.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldProcessEnqueuedItem()
    {
        var id = Guid.NewGuid();
        await _queue.EnqueueAsync(id);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        _ = _service.StartAsync(cts.Token);

        await Task.Delay(500);
        await _service.StopAsync(CancellationToken.None);

        _processorMock.Verify(p => p.ProcessAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldProcessMultipleItems()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        await _queue.EnqueueAsync(id1);
        await _queue.EnqueueAsync(id2);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        _ = _service.StartAsync(cts.Token);

        await Task.Delay(500);
        await _service.StopAsync(CancellationToken.None);

        _processorMock.Verify(p => p.ProcessAsync(id1, It.IsAny<CancellationToken>()), Times.Once);
        _processorMock.Verify(p => p.ProcessAsync(id2, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenProcessorThrows_ShouldContinueProcessing()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        _processorMock.Setup(p => p.ProcessAsync(id1, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("processing failed"));

        await _queue.EnqueueAsync(id1);
        await _queue.EnqueueAsync(id2);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        _ = _service.StartAsync(cts.Token);

        await Task.Delay(500);
        await _service.StopAsync(CancellationToken.None);

        _processorMock.Verify(p => p.ProcessAsync(id2, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCancelled_ShouldStop()
    {
        using var cts = new CancellationTokenSource();
        _ = _service.StartAsync(cts.Token);

        await Task.Delay(200);
        cts.Cancel();
        await _service.StopAsync(CancellationToken.None);

        _processorMock.Verify(p => p.ProcessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
