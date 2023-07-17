namespace Velvet.Async;

public readonly partial struct VelvetTask
{
    private static VelvetTask Create<TPromise, TArg>(TArg arg, CancellationToken cancellationToken)
        where TPromise : IPromise<TArg> => new(TPromise.Create(arg, cancellationToken, out short tok), tok);

    private interface IPromise<in TArg>
    {
        public static abstract IVelvetTaskSource Create(TArg arg, CancellationToken cancellationToken,
            out short token);
    }

    private abstract class BasicPromise : IVelvetTaskSource
    {
        protected ManualResetVelvetTaskSourceCore<object> Core;

        public abstract void MoveNext();
        protected abstract void TryReturn();


        public void GetResult(short token) {
            try {
                Core.GetResult(token);
            }
            finally {
                TryReturn();
            }
        }

        public VelvetTaskStatus GetStatus(short token) => Core.GetStatus(token);

        public VelvetTaskStatus UnsafeGetStatus() => Core.UnsafeGetStatus();

        public void OnCompleted(Action<object?> continuation, object? state, short token) =>
            Core.OnCompleted(continuation, state, token);
    }
}