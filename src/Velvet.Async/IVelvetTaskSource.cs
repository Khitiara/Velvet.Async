namespace Velvet.Async;

public interface IVelvetTaskSource
{
    VelvetTaskStatus GetStatus(short token);
    void OnCompleted(Action<object?> continuation, object? state, short token);
    void GetResult(short token);
    
    
    VelvetTaskStatus UnsafeGetStatus(); // only for debug use.
}


public interface IVelvetTaskSource<out T> : IVelvetTaskSource
{
    new T GetResult(short token);
}