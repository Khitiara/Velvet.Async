namespace Velvet.Async;

public enum VelvetPlatformLoopStep : byte
{
    Initialization,
    PostInitialization,
    PreUpdate,
    Update,
    PostUpdate,
    PreRender,
    Render,
    PostRender,
    // TimeUpdate,
}