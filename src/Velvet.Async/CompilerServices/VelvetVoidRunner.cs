using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Velvet.Async.CompilerServices;

internal sealed class VelvetVoidRunner<TStateMachine> : IAsyncStateMachineBox
    where TStateMachine : IAsyncStateMachine
{
    internal TStateMachine StateMachine = default!;

    private static readonly ObjectPool<VelvetVoidRunner<TStateMachine>> Pool = new(() =>
        new VelvetVoidRunner<TStateMachine>());

    public static VelvetVoidRunner<TStateMachine> Get() => Pool.Allocate();

    public void Return() => Pool.Free(this);

    private VelvetVoidRunner() {
        MoveNextAction = MoveNext;
    }

    public void MoveNext() {
        StateMachine.MoveNext();
    }

    public Action MoveNextAction { get; }
}