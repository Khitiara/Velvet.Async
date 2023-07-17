namespace Velvet.Async.CompilerServices;

internal interface IStateMachineRunnerPromise<T> : IVelvetTaskSource<T>
{
    Action MoveNext { get; }
    VelvetTask<T> Task { get; }
    void SetResult(T result);
    void SetException(Exception exception);
}