using System.Threading.Channels;

namespace WcagAnalyzer.Application.Services;

public class AnalysisQueue : IAnalysisQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>();

    public async ValueTask EnqueueAsync(Guid analysisId)
    {
        await _channel.Writer.WriteAsync(analysisId);
    }

    public async ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken)
    {
        return await _channel.Reader.ReadAsync(cancellationToken);
    }
}
