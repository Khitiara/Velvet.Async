using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Velvet.Async;

public readonly partial struct VelvetTask
{
    public static YieldAwaitable Yield(VelvetPlatformLoopStep timing = VelvetPlatformLoopStep.Update) =>
        // optimized for single continuation
        new(timing);


    public static VelvetTask Yield(VelvetPlatformLoopStep timing, CancellationToken cancellationToken) =>
        Create<YieldPromise, VelvetPlatformLoopStep>(timing, cancellationToken);

    private sealed class YieldPromise : BasicPromise, IPromise<VelvetPlatformLoopStep>
    {
        private static readonly ObjectPool<YieldPromise> Pool = new(() => new YieldPromise());

        private CancellationToken _cancellationToken;

        private YieldPromise() { }

        public static IVelvetTaskSource Create(VelvetPlatformLoopStep timing, CancellationToken cancellationToken,
            out short token) {
            if (cancellationToken.IsCancellationRequested) {
                return AutoResetVelvetTaskCompletionSource.CreateFromCanceled(cancellationToken, out token);
            }

            YieldPromise result = Pool.Allocate();


            result._cancellationToken = cancellationToken;

            VelvetTaskScheduler.Schedule(timing, result.MoveNext);

            token = result.Core.Version;
            return result;
        }

        public override void MoveNext() {
            if (_cancellationToken.IsCancellationRequested) {
                Core.TrySetCanceled(_cancellationToken);
                return;
            }

            Core.TrySetResult(null!);
        }

        protected override void TryReturn() {
            Core.Reset();
            _cancellationToken = default;
            Pool.Free(this);
        }
    }
}

public readonly struct YieldAwaitable
{
    private readonly VelvetPlatformLoopStep _timing;

    public YieldAwaitable(VelvetPlatformLoopStep timing) {
        this._timing = timing;
    }

    public Awaiter GetAwaiter() {
        return new Awaiter(_timing);
    }

    public VelvetTask ToVelvetTask() {
        return VelvetTask.Yield(_timing, CancellationToken.None);
    }

    public readonly struct Awaiter : ICriticalNotifyCompletion
    {
        private readonly VelvetPlatformLoopStep timing;

        public Awaiter(VelvetPlatformLoopStep timing) {
            this.timing = timing;
        }

        public bool IsCompleted => false;

        public void GetResult() { }

        public void OnCompleted(Action continuation) {
            VelvetTaskScheduler.Schedule(timing, continuation);
        }

        public void UnsafeOnCompleted(Action continuation) {
            VelvetTaskScheduler.Schedule(timing, continuation);
        }
    }
}