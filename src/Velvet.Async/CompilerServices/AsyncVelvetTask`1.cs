using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Velvet.Async.CompilerServices;

internal sealed class AsyncVelvetTask<TStateMachine> : IStateMachineRunnerPromise, IVelvetTaskSource
    where TStateMachine : IAsyncStateMachine
{
    private static readonly ObjectPool<AsyncVelvetTask<TStateMachine>> Pool = new(() =>
        new AsyncVelvetTask<TStateMachine>());

    public Action MoveNext { get; }

    private TStateMachine?                              _stateMachine;
    private ManualResetVelvetTaskSourceCore<AsyncUnit> _core;

    private AsyncVelvetTask() {
        MoveNext = Run;
    }

    public static void SetStateMachine(ref TStateMachine stateMachine,
        out IStateMachineRunnerPromise runnerPromiseFieldRef) {
        AsyncVelvetTask<TStateMachine> result = Pool.Allocate();

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

    public VelvetTask Task {
        [DebuggerHidden] get => new VelvetTask(this, _core.Version);
    }

    [DebuggerHidden]
    public void SetResult() {
        _core.TrySetResult(AsyncUnit.Default);
    }

    [DebuggerHidden]
    public void SetException(Exception exception) {
        _core.TrySetException(exception);
    }

    [DebuggerHidden]
    public void GetResult(short token) {
        try {
            _core.GetResult(token);
        }
        finally {
            Free();
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
}