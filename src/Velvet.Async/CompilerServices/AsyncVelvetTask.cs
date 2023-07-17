using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Velvet.Async.CompilerServices;

internal sealed class AsyncVelvetTask<TStateMachine, T> : IStateMachineRunnerPromise<T>, IVelvetTaskSource<T>
    where TStateMachine : IAsyncStateMachine
{
    private static readonly ObjectPool<AsyncVelvetTask<TStateMachine, T>> Pool = new(() => new AsyncVelvetTask<TStateMachine, T>());

    public Action MoveNext { get; }

    private TStateMachine?                      _stateMachine;
    private ManualResetVelvetTaskSourceCore<T> _core;

    private AsyncVelvetTask() {
        MoveNext = Run;
    }

    public static void SetStateMachine(ref TStateMachine stateMachine,
        out IStateMachineRunnerPromise<T> runnerPromiseFieldRef) {
        AsyncVelvetTask<TStateMachine, T>? result = Pool.Allocate();

        runnerPromiseFieldRef = result; // set runner before copied.
        result._stateMachine = stateMachine; // copy struct StateMachine(in release build).
    }

    private void Free() {
        _core.Reset();
        _stateMachine = default;
        Pool.Free(this);
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Run() {
        _stateMachine?.MoveNext();
    }

    public VelvetTask<T> Task {
        [DebuggerHidden] get => new VelvetTask<T>(this, _core.Version);
    }

    [DebuggerHidden]
    public void SetResult(T result) {
        _core.TrySetResult(result);
    }

    [DebuggerHidden]
    public void SetException(Exception exception) {
        _core.TrySetException(exception);
    }

    [DebuggerHidden]
    public T GetResult(short token) {
        try {
            return _core.GetResult(token);
        }
        finally {
            Free();
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
}