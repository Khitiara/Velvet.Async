using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace Velvet.Async.CompilerServices;

[StructLayout(LayoutKind.Auto)]
public struct VelvetTaskAsyncMethodBuilder<T>
{
    private IStateMachineRunnerPromise<T>? _runnerPromise;
    private Exception?                     _exception;
    private T?                             _result;

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static VelvetTaskAsyncMethodBuilder<T> Create() => default;

    public VelvetTask<T> Task {
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _runnerPromise?.Task ?? (_exception != null
            ? VelvetTask.FromException<T>(_exception)
            : VelvetTask.FromResult(_result!));
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetException(Exception exception) {
        if (_runnerPromise == null) {
            _exception = exception;
        } else {
            _runnerPromise.SetException(exception);
        }
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetResult(T result) {
        if (_runnerPromise == null) {
            this._result = result;
        } else {
            _runnerPromise.SetResult(result);
        }
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : INotifyCompletion
        where TStateMachine : IAsyncStateMachine {
        if (_runnerPromise == null) {
            AsyncVelvetTask<TStateMachine, T>.SetStateMachine(ref stateMachine, out _runnerPromise);
        }

        awaiter.OnCompleted(_runnerPromise.MoveNext);
    }

    [DebuggerHidden]
    [SecuritySafeCritical]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : ICriticalNotifyCompletion
        where TStateMachine : IAsyncStateMachine {
        if (_runnerPromise == null) {
            AsyncVelvetTask<TStateMachine, T>.SetStateMachine(ref stateMachine, out _runnerPromise);
        }

        awaiter.UnsafeOnCompleted(_runnerPromise.MoveNext);
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Start<TStateMachine>(ref TStateMachine stateMachine)
        where TStateMachine : IAsyncStateMachine {
        stateMachine.MoveNext();
    }

    [DebuggerHidden]
    public void SetStateMachine(IAsyncStateMachine stateMachine) { }
}