namespace Velvet.Async;

public sealed class VelvetTimer : IDisposable
{
    private readonly PeriodicTimer _timer;

    public VelvetTimer(TimeSpan period) {
        _timer = new PeriodicTimer(period);
    }

    public async VelvetTask<bool> WaitForNextTickAsync(CancellationToken cancellationToken = default) {
        try {
            return await _timer.WaitForNextTickAsync(cancellationToken);
        }
        finally {
            await VelvetTask.SwitchToMainThread(cancellationToken: cancellationToken);
        }
    }

    public void Dispose() {
        GC.SuppressFinalize(this);
        _timer.Dispose();
    }

    ~VelvetTimer() => Dispose();
}