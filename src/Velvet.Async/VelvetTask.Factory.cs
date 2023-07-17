using System.Runtime.ExceptionServices;

namespace Velvet.Async;

public readonly partial struct VelvetTask
{
    private static readonly VelvetTask CanceledVelvetTask =
        new Func<VelvetTask>(() => new VelvetTask(new CanceledResultSource(CancellationToken.None), 0))();

    private static class CanceledVelvetTaskCache<T>
    {
        public static readonly VelvetTask<T> Task;

        static CanceledVelvetTaskCache() {
            Task = new VelvetTask<T>(new CanceledResultSource<T>(CancellationToken.None), 0);
        }
    }

    public static readonly VelvetTask CompletedTask;

    public static VelvetTask FromException(Exception ex) =>
        ex is OperationCanceledException oce
            ? FromCanceled(oce.CancellationToken)
            : new VelvetTask(new ExceptionResultSource(ex), 0);

    public static VelvetTask<T> FromException<T>(Exception ex) =>
        ex is OperationCanceledException oce
            ? FromCanceled<T>(oce.CancellationToken)
            : new VelvetTask<T>(new ExceptionResultSource<T>(ex), 0);

    public static VelvetTask<T> FromResult<T>(T value) => new(value);

    public static VelvetTask FromCanceled(CancellationToken cancellationToken = default) => cancellationToken == CancellationToken.None ? CanceledVelvetTask : new VelvetTask(new CanceledResultSource(cancellationToken), 0);

    public static VelvetTask<T> FromCanceled<T>(CancellationToken cancellationToken = default) => cancellationToken == CancellationToken.None ? CanceledVelvetTaskCache<T>.Task : new VelvetTask<T>(new CanceledResultSource<T>(cancellationToken), 0);

    public static VelvetTask Create(Func<VelvetTask> factory) => factory();

    public static VelvetTask<T> Create<T>(Func<VelvetTask<T>> factory) => factory();

    // public static AsyncLazy Lazy(Func<VelvetTask> factory) => new AsyncLazy(factory);

    // public static AsyncLazy<T> Lazy<T>(Func<VelvetTask<T>> factory) => new AsyncLazy<T>(factory);

    /// <summary>
    /// helper of fire and forget void action.
    /// </summary>
    public static void Void(Func<VelvetVoidTask> asyncAction) {
        asyncAction().Forget();
    }

    /// <summary>
    /// helper of fire and forget void action.
    /// </summary>
    public static void Void(Func<CancellationToken, VelvetVoidTask> asyncAction,
        CancellationToken cancellationToken) {
        asyncAction(cancellationToken).Forget();
    }

    /// <summary>
    /// helper of fire and forget void action.
    /// </summary>
    public static void Void<T>(Func<T, VelvetVoidTask> asyncAction, T state) {
        asyncAction(state).Forget();
    }

    /// <summary>
    /// helper of create add VelvetTaskVoid to delegate.
    /// For example: FooAction = VelvetTask.Action(async () => { /* */ })
    /// </summary>
    public static Action Action(Func<VelvetVoidTask> asyncAction) => () => asyncAction().Forget();

    /// <summary>
    /// helper of create add VelvetTaskVoid to delegate.
    /// </summary>
    public static Action Action(Func<CancellationToken, VelvetVoidTask> asyncAction,
        CancellationToken cancellationToken) =>
        () => asyncAction(cancellationToken).Forget();

    /// <summary>
    /// Defer the task creation just before call await.
    /// </summary>
    public static VelvetTask Defer(Func<VelvetTask> factory) => new(new DeferPromise(factory), 0);

    /// <summary>
    /// Defer the task creation just before call await.
    /// </summary>
    public static VelvetTask<T> Defer<T>(Func<VelvetTask<T>> factory) =>
        new(new DeferPromise<T>(factory), 0);

    /// <summary>
    /// Never complete.
    /// </summary>
    public static VelvetTask Never(CancellationToken cancellationToken) => new VelvetTask<AsyncUnit>(new NeverPromise<AsyncUnit>(cancellationToken), 0);

    /// <summary>
    /// Never complete.
    /// </summary>
    public static VelvetTask<T> Never<T>(CancellationToken cancellationToken) => new(new NeverPromise<T>(cancellationToken), 0);

    private sealed class ExceptionResultSource : IVelvetTaskSource
    {
        private readonly ExceptionDispatchInfo _exception;
        private          bool                  _calledGet;

        public ExceptionResultSource(Exception exception) {
            _exception = ExceptionDispatchInfo.Capture(exception);
        }

        public void GetResult(short token) {
            if (!_calledGet) {
                _calledGet = true;
                GC.SuppressFinalize(this);
            }

            _exception.Throw();
        }

        public VelvetTaskStatus GetStatus(short token) => VelvetTaskStatus.Faulted;

        public VelvetTaskStatus UnsafeGetStatus() => VelvetTaskStatus.Faulted;

        public void OnCompleted(Action<object?> continuation, object? state, short token) {
            continuation(state);
        }

        ~ExceptionResultSource() {
            if (!_calledGet) {
                VelvetTaskScheduler.PublishUnobservedTaskException(_exception.SourceException);
            }
        }
    }

    private sealed class ExceptionResultSource<T> : IVelvetTaskSource<T>
    {
        private readonly ExceptionDispatchInfo _exception;
        private          bool                  _calledGet;

        public ExceptionResultSource(Exception exception) {
            _exception = ExceptionDispatchInfo.Capture(exception);
        }

        public T GetResult(short token) {
            if (!_calledGet) {
                _calledGet = true;
                GC.SuppressFinalize(this);
            }

            _exception.Throw();
            return default;
        }

        void IVelvetTaskSource.GetResult(short token) {
            if (!_calledGet) {
                _calledGet = true;
                GC.SuppressFinalize(this);
            }

            _exception.Throw();
        }

        public VelvetTaskStatus GetStatus(short token) => VelvetTaskStatus.Faulted;

        public VelvetTaskStatus UnsafeGetStatus() => VelvetTaskStatus.Faulted;

        public void OnCompleted(Action<object?> continuation, object? state, short token) {
            continuation(state);
        }

        ~ExceptionResultSource() {
            if (!_calledGet) {
                VelvetTaskScheduler.PublishUnobservedTaskException(_exception.SourceException);
            }
        }
    }

    private sealed class CanceledResultSource : IVelvetTaskSource
    {
        private readonly CancellationToken _cancellationToken;

        public CanceledResultSource(CancellationToken cancellationToken) {
            _cancellationToken = cancellationToken;
        }

        public void GetResult(short token) {
            throw new OperationCanceledException(_cancellationToken);
        }

        public VelvetTaskStatus GetStatus(short token) => VelvetTaskStatus.Canceled;

        public VelvetTaskStatus UnsafeGetStatus() => VelvetTaskStatus.Canceled;

        public void OnCompleted(Action<object?> continuation, object? state, short token) {
            continuation(state);
        }
    }

    private sealed class CanceledResultSource<T> : IVelvetTaskSource<T>
    {
        private readonly CancellationToken _cancellationToken;

        public CanceledResultSource(CancellationToken cancellationToken) {
            _cancellationToken = cancellationToken;
        }

        public T GetResult(short token) => throw new OperationCanceledException(_cancellationToken);

        void IVelvetTaskSource.GetResult(short token) {
            throw new OperationCanceledException(_cancellationToken);
        }

        public VelvetTaskStatus GetStatus(short token) => VelvetTaskStatus.Canceled;

        public VelvetTaskStatus UnsafeGetStatus() => VelvetTaskStatus.Canceled;

        public void OnCompleted(Action<object?> continuation, object? state, short token) {
            continuation(state);
        }
    }

    private sealed class DeferPromise : IVelvetTaskSource
    {
        private Func<VelvetTask>? _factory;
        private VelvetTask        _task;
        private Awaiter             _awaiter;

        public DeferPromise(Func<VelvetTask> factory) {
            _factory = factory;
        }

        public void GetResult(short token) {
            _awaiter.GetResult();
        }

        public VelvetTaskStatus GetStatus(short token) {
            if (Interlocked.Exchange(ref _factory, null) is { } f1) {
                _task = f1();
                _awaiter = _task.GetAwaiter();
            }

            return _task.Status;
        }

        public void OnCompleted(Action<object?> continuation, object? state, short token) {
            _awaiter.SourceOnCompleted(continuation, state);
        }

        public VelvetTaskStatus UnsafeGetStatus() => _task.Status;
    }

    private sealed class DeferPromise<T> : IVelvetTaskSource<T>
    {
        private Func<VelvetTask<T>>?  _factory;
        private VelvetTask<T>         _task;
        private VelvetTask<T>.Awaiter _awaiter;

        public DeferPromise(Func<VelvetTask<T>> factory) {
            _factory = factory;
        }

        public T GetResult(short token) => _awaiter.GetResult();

        void IVelvetTaskSource.GetResult(short token) {
            _awaiter.GetResult();
        }

        public VelvetTaskStatus GetStatus(short token) {
            if (Interlocked.Exchange(ref _factory, null) is { } f) {
                _task = f();
                _awaiter = _task.GetAwaiter();
            }

            return _task.Status;
        }

        public void OnCompleted(Action<object?> continuation, object? state, short token) {
            _awaiter.SourceOnCompleted(continuation, state);
        }

        public VelvetTaskStatus UnsafeGetStatus() => _task.Status;
    }

    private sealed class NeverPromise<T> : IVelvetTaskSource<T>
    {
        private readonly CancellationToken                   _cancellationToken;
        private          ManualResetVelvetTaskSourceCore<T> _core;

        public NeverPromise(CancellationToken cancellationToken) {
            _cancellationToken = cancellationToken;
            if (_cancellationToken.CanBeCanceled) {
                _cancellationToken.RegisterWithoutCaptureExecutionContext(CancellationCallback, this);
            }
        }

        private static void CancellationCallback(object? state) {
            NeverPromise<T> self = (NeverPromise<T>)state!;
            self._core.TrySetCanceled(self._cancellationToken);
        }

        public T GetResult(short token) => _core.GetResult(token);

        public VelvetTaskStatus GetStatus(short token) => _core.GetStatus(token);

        public VelvetTaskStatus UnsafeGetStatus() => _core.UnsafeGetStatus();

        public void OnCompleted(Action<object?> continuation, object? state, short token) {
            _core.OnCompleted(continuation, state, token);
        }

        void IVelvetTaskSource.GetResult(short token) {
            _core.GetResult(token);
        }
    }
}