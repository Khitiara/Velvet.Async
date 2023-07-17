# Velvet.Async
An over-engineered-to-death async main loop for [Silk.NET](https://github.com/dotnet/Silk.NET), based loosely on
[UniTask](https://github.com/Cysharp/UniTask) in principle. 

Instead of using a single-threaded `SynchronizationContext`, this project uses a custom 
[task-like type](https://github.com/dotnet/roslyn/blob/main/docs/features/task-types.md) called `VelvetTask`, whose
sources largely provide awaiters that schedule back on the main loop, along with some auxiliary awaiters that allow
seamlessly returning to the main loop thread, advancing one frame, etc.

The initial implementation for this comes from my previous work on 
[Anabasis](https://github.com/AnabasisEngine/AnabasisLegacy/tree/develop/src/Anabasis.Tasks)