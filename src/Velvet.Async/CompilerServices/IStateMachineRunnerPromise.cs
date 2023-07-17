namespace Velvet.Async.CompilerServices;

internal interface IStateMachineRunnerPromise : IVelvetTaskSource
{
    Action MoveNext { get; }
    VelvetTask Task { get; }
    void SetResult();
    void SetException(Exception exception);
}