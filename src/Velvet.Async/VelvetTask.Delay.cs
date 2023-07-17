using Microsoft.CodeAnalysis.PooledObjects;

namespace Velvet.Async;

public readonly partial struct VelvetTask
{
    internal static ulong    Frame;
    internal static TimeSpan TotalElapsed;

    public static VelvetTask WaitForNextFrame(VelvetPlatformLoopStep timing = VelvetPlatformLoopStep.Update,
        CancellationToken cancellationToken = default) =>
        Create<NextFramePromise, VelvetPlatformLoopStep>(timing, cancellationToken);

    public static VelvetTask Delay(TimeSpan timeout, CancellationToken cancellationToken = default) =>
        Create<DelayPromise, TimeSpan>(timeout, cancellationToken);

    private sealed class NextFramePromise : BasicPromise, IPromise<VelvetPlatformLoopStep>
    {
        private static readonly ObjectPool<NextFramePromise> Pool = new(() => new NextFramePromise());
        private                 CancellationToken            _cancellationToken;
        private                 ulong                        _currentFrame;
        private                 VelvetPlatformLoopStep     _timing;

        private NextFramePromise() { }

        public static IVelvetTaskSource Create(VelvetPlatformLoopStep timing, CancellationToken cancellationToken,
            out short token) {
            if (cancellationToken.IsCancellationRequested) {
                return AutoResetVelvetTaskCompletionSource.CreateFromCanceled(cancellationToken, out token);
            }

            NextFramePromise result = Pool.Allocate();
            result._cancellationToken = cancellationToken;
            result._currentFrame = Frame;
            result._timing = timing;

            VelvetTaskScheduler.Schedule(timing, result.MoveNext);

            token = result.Core.Version;
            return result;
        }

        public override void MoveNext() {
            if (_cancellationToken.IsCancellationRequested) {
                Core.TrySetCanceled(_cancellationToken);
                return;
            }

            if (_currentFrame == Frame) {
                VelvetTaskScheduler.Schedule(_timing, MoveNext);
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

    private sealed class DelayPromise : BasicPromise, IPromise<TimeSpan>
    {
        private static readonly ObjectPool<DelayPromise> Pool = new(() => new DelayPromise());
        private                 CancellationToken        _cancellationToken;
        private                 TimeSpan                 _target;

        private DelayPromise() { }

        public static IVelvetTaskSource Create(TimeSpan timeout, CancellationToken cancellationToken,
            out short token) {
            if (cancellationToken.IsCancellationRequested) {
                return AutoResetVelvetTaskCompletionSource.CreateFromCanceled(cancellationToken, out token);
            }

            DelayPromise result = Pool.Allocate();
            result._cancellationToken = cancellationToken;
            result._target = TotalElapsed + timeout;

            VelvetTaskScheduler.Schedule(VelvetPlatformLoopStep.Update, result.MoveNext);

            token = result.Core.Version;
            return result;
        }

        public override void MoveNext() {
            if (_cancellationToken.IsCancellationRequested) {
                Core.TrySetCanceled(_cancellationToken);
                return;
            }

            if (TotalElapsed < _target) {
                VelvetTaskScheduler.Schedule(VelvetPlatformLoopStep.Update, MoveNext);
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