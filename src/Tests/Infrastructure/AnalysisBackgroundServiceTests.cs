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
        var tcs = new TaskCompletionSource();

        _processorMock.Setup(p => p.ProcessAsync(id, It.IsAny<CancellationToken>()))
            .Returns(() => { tcs.SetResult(); return Task.CompletedTask; });

        await _queue.EnqueueAsync(id);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        _ = _service.StartAsync(cts.Token);

        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await _service.StopAsync(CancellationToken.None);

        _processorMock.Verify(p => p.ProcessAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldProcessMultipleItems()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var tcs1 = new TaskCompletionSource();
        var tcs2 = new TaskCompletionSource();

        _processorMock.Setup(p => p.ProcessAsync(id1, It.IsAny<CancellationToken>()))
            .Returns(() => { tcs1.TrySetResult(); return Task.CompletedTask; });
        _processorMock.Setup(p => p.ProcessAsync(id2, It.IsAny<CancellationToken>()))
            .Returns(() => { tcs2.TrySetResult(); return Task.CompletedTask; });

        await _queue.EnqueueAsync(id1);
        await _queue.EnqueueAsync(id2);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        _ = _service.StartAsync(cts.Token);

        await Task.WhenAll(
            tcs1.Task.WaitAsync(TimeSpan.FromSeconds(5)),
            tcs2.Task.WaitAsync(TimeSpan.FromSeconds(5)));
        await _service.StopAsync(CancellationToken.None);

        _processorMock.Verify(p => p.ProcessAsync(id1, It.IsAny<CancellationToken>()), Times.Once);
        _processorMock.Verify(p => p.ProcessAsync(id2, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenProcessorThrows_ShouldContinueProcessing()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var tcs2 = new TaskCompletionSource();

        _processorMock.Setup(p => p.ProcessAsync(id1, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("processing failed"));
        _processorMock.Setup(p => p.ProcessAsync(id2, It.IsAny<CancellationToken>()))
            .Returns(() => { tcs2.TrySetResult(); return Task.CompletedTask; });

        await _queue.EnqueueAsync(id1);
        await _queue.EnqueueAsync(id2);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        _ = _service.StartAsync(cts.Token);

        await tcs2.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await _service.StopAsync(CancellationToken.None);

        _processorMock.Verify(p => p.ProcessAsync(id2, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCancelled_ShouldStop()
    {
        using var cts = new CancellationTokenSource();
        _ = _service.StartAsync(cts.Token);

        cts.Cancel();
        await _service.StopAsync(CancellationToken.None);

        _processorMock.Verify(p => p.ProcessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
