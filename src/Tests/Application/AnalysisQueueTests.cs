using FluentAssertions;
using WcagAnalyzer.Application.Services;

namespace WcagAnalyzer.Tests.Application;

public class AnalysisQueueTests
{
    [Fact]
    public async Task EnqueueAndDequeue_ShouldReturnSameId()
    {
        var queue = new AnalysisQueue();
        var id = Guid.NewGuid();

        await queue.EnqueueAsync(id);
        var result = await queue.DequeueAsync(CancellationToken.None);

        result.Should().Be(id);
    }

    [Fact]
    public async Task Dequeue_ShouldMaintainFifoOrder()
    {
        var queue = new AnalysisQueue();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var id3 = Guid.NewGuid();

        await queue.EnqueueAsync(id1);
        await queue.EnqueueAsync(id2);
        await queue.EnqueueAsync(id3);

        var result1 = await queue.DequeueAsync(CancellationToken.None);
        var result2 = await queue.DequeueAsync(CancellationToken.None);
        var result3 = await queue.DequeueAsync(CancellationToken.None);

        result1.Should().Be(id1);
        result2.Should().Be(id2);
        result3.Should().Be(id3);
    }

    [Fact]
    public async Task Dequeue_WhenCancelled_ShouldThrow()
    {
        var queue = new AnalysisQueue();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await queue.DequeueAsync(cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task Dequeue_ShouldWaitUntilItemEnqueued()
    {
        var queue = new AnalysisQueue();
        var id = Guid.NewGuid();
        var dequeued = Guid.Empty;

        var dequeueTask = Task.Run(async () =>
        {
            dequeued = await queue.DequeueAsync(CancellationToken.None);
        });

        await Task.Delay(100);
        dequeueTask.IsCompleted.Should().BeFalse();

        await queue.EnqueueAsync(id);
        await dequeueTask;

        dequeued.Should().Be(id);
    }
}
