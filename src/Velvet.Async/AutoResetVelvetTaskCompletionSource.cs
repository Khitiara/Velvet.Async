using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Velvet.Async;

public class AutoResetVelvetTaskCompletionSource : IVelvetTaskSource
{
    private static readonly ObjectPool<AutoResetVelvetTaskCompletionSource> Pool = new(() =>
        new AutoResetVelvetTaskCompletionSource());

    private ManualResetVelvetTaskSourceCore<AsyncUnit> _core;

    private AutoResetVelvetTaskCompletionSource() { }

    [DebuggerHidden]
    public static AutoResetVelvetTaskCompletionSource Create() => Pool.Allocate();

    [DebuggerHidden]
    public static AutoResetVelvetTaskCompletionSource CreateFromCanceled(CancellationToken cancellationToken,
        out short token) {
        AutoResetVelvetTaskCompletionSource source = Create();
        source.TrySetCanceled(cancellationToken);
        token = source._core.Version;
        return source;
    }

    [DebuggerHidden]
    public static AutoResetVelvetTaskCompletionSource CreateFromException(Exception exception, out short token) {
        AutoResetVelvetTaskCompletionSource source = Create();
        source.TrySetException(exception);
        token = source._core.Version;
        return source;
    }

    [DebuggerHidden]
    public static AutoResetVelvetTaskCompletionSource CreateCompleted(out short token) {
        AutoResetVelvetTaskCompletionSource source = Create();
        source.TrySetResult();
        token = source._core.Version;
        return source;
    }

    public VelvetTask Task {
        [DebuggerHidden] get => new(this, _core.Version);
    }

    [DebuggerHidden]
    public bool TrySetResult() => _core.TrySetResult(AsyncUnit.Default);

    [DebuggerHidden]
    public bool TrySetCanceled(CancellationToken cancellationToken = default) =>
        _core.TrySetCanceled(cancellationToken);

    [DebuggerHidden]
    public bool TrySetException(Exception exception) => _core.TrySetException(exception);

    [DebuggerHidden]
    public void GetResult(short token) {
        try {
            _core.GetResult(token);
        }
        finally {
            TryReturn();
        }
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