using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Velvet.Async;

public class AutoResetVelvetTaskCompletionSource<T> : IVelvetTaskSource<T>
{
    private static readonly ObjectPool<AutoResetVelvetTaskCompletionSource<T>> Pool = new(() =>
        new AutoResetVelvetTaskCompletionSource<T>());

    private ManualResetVelvetTaskSourceCore<T> _core;

    private AutoResetVelvetTaskCompletionSource() { }

    [DebuggerHidden]
    public static AutoResetVelvetTaskCompletionSource<T> Create() => Pool.Allocate();

    [DebuggerHidden]
    public static AutoResetVelvetTaskCompletionSource<T> CreateFromCanceled(CancellationToken cancellationToken,
        out short token) {
        AutoResetVelvetTaskCompletionSource<T> source = Create();
        source.TrySetCanceled(cancellationToken);
        token = source._core.Version;
        return source;
    }

    [DebuggerHidden]
    public static AutoResetVelvetTaskCompletionSource<T> CreateFromException(Exception exception, out short token) {
        AutoResetVelvetTaskCompletionSource<T> source = Create();
        source.TrySetException(exception);
        token = source._core.Version;
        return source;
    }

    [DebuggerHidden]
    public static AutoResetVelvetTaskCompletionSource<T> CreateFromResult(T result, out short token) {
        AutoResetVelvetTaskCompletionSource<T> source = Create();
        source.TrySetResult(result);
        token = source._core.Version;
        return source;
    }

    public VelvetTask<T> Task {
        [DebuggerHidden] get => new(this, _core.Version);
    }

    [DebuggerHidden]
    public bool TrySetResult(T result) => _core.TrySetResult(result);

    [DebuggerHidden]
    public bool TrySetCanceled(CancellationToken cancellationToken = default) =>
        _core.TrySetCanceled(cancellationToken);

    [DebuggerHidden]
    public bool TrySetException(Exception exception) => _core.TrySetException(exception);

    [DebuggerHidden]
    public T GetResult(short token) {
        try {
            return _core.GetResult(token);
        }
        finally {
            TryReturn();
        }
    }

    [DebuggerHidden]
    void IVelvetTaskSource.GetResult(short token) {
        GetResult(token);
    }

    [DebuggerHidden]
    public VelvetTaskStatus GetStatus(short token) => _core.GetStatus(token);

    [DebuggerHidden]
    public VelvetTaskStatus UnsafeGetStatus() => _core.UnsafeGetStatus();

    [DebuggerHidden]
    public void OnCompleted(Action<object?> continuation, object? state, short token) {
        _core.OnCompleted(continuation, state, token);
    }

    [DebuggerHidden]
    private void TryReturn() {
        _core.Reset();
        Pool.Free(this);
    }
}