using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Velvet.Async;

public readonly partial struct VelvetTask
{

    /// <summary>
    /// If running on mainthread, do nothing. Otherwise, same as UniTask.Yield(timing).
    /// </summary>
    public static SwitchToMainThreadAwaitable SwitchToMainThread(VelvetPlatformLoopStep timing = VelvetPlatformLoopStep.Update,
        CancellationToken cancellationToken = default) => new(timing, cancellationToken);


    /// <summary>
    /// Return to mainthread(same as await SwitchToMainThread) after using scope is closed.
    /// </summary>
    public static ReturnToMainThread ReturnToMainThread(VelvetPlatformLoopStep timing = VelvetPlatformLoopStep.Update,
        CancellationToken cancellationToken = default) => new(timing, cancellationToken);

    /// <summary>
    /// Queue the action to PlayerLoop.
    /// </summary>
    public static void Post(Action action, VelvetPlatformLoopStep timing = VelvetPlatformLoopStep.Update) {
        VelvetTaskScheduler.Schedule(timing, action);
    }
    
    public static SwitchToThreadPoolAwaitable SwitchToThreadPool() => new();
}

public readonly struct SwitchToMainThreadAwaitable
{
    private readonly         VelvetPlatformLoopStep _timing;
    private readonly CancellationToken        _cancellationToken;

    public SwitchToMainThreadAwaitable(VelvetPlatformLoopStep timing,
        CancellationToken cancellationToken) {
        _timing = timing;
        _cancellationToken = cancellationToken;
    }

    public Awaiter GetAwaiter() => new(_timing, _cancellationToken);

    public readonly struct Awaiter : ICriticalNotifyCompletion
    {
        private readonly VelvetPlatformLoopStep _timing;
        private readonly CancellationToken        _cancellationToken;

        public Awaiter(VelvetPlatformLoopStep timing, CancellationToken cancellationToken) {
            _timing = timing;
            _cancellationToken = cancellationToken;
        }

        public bool IsCompleted => VelvetTaskScheduler.MainThreadId == Environment.CurrentManagedThreadId;

        public void GetResult() {
            _cancellationToken.ThrowIfCancellationRequested();
        }

        public void OnCompleted(Action continuation) {
            VelvetTaskScheduler.Schedule(_timing, continuation);
        }

        public void UnsafeOnCompleted(Action continuation) {
            VelvetTaskScheduler.Schedule(_timing, continuation);
        }
    }
}

public readonly struct ReturnToMainThread
{
    private readonly VelvetPlatformLoopStep _timing;
    private readonly CancellationToken        _cancellationToken;

    public ReturnToMainThread(VelvetPlatformLoopStep timing, CancellationToken cancellationToken) {
        _timing = timing;
        _cancellationToken = cancellationToken;
    }

    public Awaiter DisposeAsync() => new(_timing, _cancellationToken); // run immediate.

    public readonly struct Awaiter : ICriticalNotifyCompletion
    {
        private readonly VelvetPlatformLoopStep _timing;
        private readonly CancellationToken        _cancellationToken;

        public Awaiter(VelvetPlatformLoopStep timing, CancellationToken cancellationToken) {
            _timing = timing;
            _cancellationToken = cancellationToken;
        }

        public Awaiter GetAwaiter() => this;

        public bool IsCompleted => VelvetTaskScheduler.MainThreadId == Environment.CurrentManagedThreadId;

        public void GetResult() {
            _cancellationToken.ThrowIfCancellationRequested();
        }

        public void OnCompleted(Action continuation) {
            VelvetTaskScheduler.Schedule(_timing, continuation);
        }

        public void UnsafeOnCompleted(Action continuation) {
            VelvetTaskScheduler.Schedule(_timing, continuation);
        }
    }
}

public struct SwitchToThreadPoolAwaitable
{
    public Awaiter GetAwaiter() => new();

    public readonly struct Awaiter : ICriticalNotifyCompletion
    {
        private static readonly WaitCallback SwitchToCallback = Callback;

        public bool IsCompleted => false;
        public void GetResult() { }

        public void OnCompleted(Action continuation) {
            ThreadPool.QueueUserWorkItem(SwitchToCallback, continuation);
        }

        public void UnsafeOnCompleted(Action continuation) {
            ThreadPool.UnsafeQueueUserWorkItem(ThreadPoolWorkItem.Create(continuation), false);
        }

        private static void Callback(object? state) {
            (state as Action)?.Invoke();
        }
    }

    private sealed class ThreadPoolWorkItem : IThreadPoolWorkItem
    {
        private static readonly ObjectPool<ThreadPoolWorkItem> Pool = new(() => new ThreadPoolWorkItem());

        private Action? _continuation;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ThreadPoolWorkItem Create(Action continuation) {
            ThreadPoolWorkItem item = Pool.Allocate();
            item._continuation = continuation;
            return item;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Execute() {
            Action? call = _continuation;
            _continuation = null;
            if (call == null) return;
            Pool.Free(this);
            call.Invoke();
        }
    }
}