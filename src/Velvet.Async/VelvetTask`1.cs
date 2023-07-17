using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Velvet.Async.CompilerServices;

namespace Velvet.Async;

[AsyncMethodBuilder(typeof(VelvetTaskAsyncMethodBuilder<>))]
[StructLayout(LayoutKind.Auto)]
public readonly struct VelvetTask<T>
{
    private readonly IVelvetTaskSource<T>? _source;
    private readonly T                       _result;
    private readonly short                   _token;

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public VelvetTask(T result) {
        _source = default;
        _token = default;
        _result = result;
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public VelvetTask(IVelvetTaskSource<T> source, short token) {
        _source = source;
        _token = token;
        _result = default!;
    }

    public VelvetTaskStatus Status {
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            if (_source == null) return VelvetTaskStatus.Succeeded;
            return _source.GetStatus(_token);
        }
    }

    public VelvetTask AsVelvetTask() {
        if (_source == null) return VelvetTask.CompletedTask;

        VelvetTaskStatus status = _source.GetStatus(_token);
        if (!status.IsCompletedSuccessfully()) return new VelvetTask(_source, _token);
        _source.GetResult(_token);
        return VelvetTask.CompletedTask;

        // Converting UniTask<T> -> UniTask is zero overhead.
    }

    public static implicit operator VelvetTask(VelvetTask<T> self) => self.AsVelvetTask();

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Awaiter GetAwaiter() => new Awaiter(this);

    public readonly struct Awaiter : ICriticalNotifyCompletion
    {
        private readonly VelvetTask<T> _task;

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Awaiter(in VelvetTask<T> task) {
            _task = task;
        }

        public bool IsCompleted {
            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _task.Status.IsCompleted();
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetResult() => _task._source is { } s ? s.GetResult(_task._token) : _task._result;

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnCompleted(Action continuation) {
            if (_task._source is { } s)
                s.OnCompleted(AwaiterActions.InvokeContinuationDelegate, continuation,_task._token);
            else
                continuation();
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnsafeOnCompleted(Action continuation) {
            if (_task._source is { } s)
                s.OnCompleted(AwaiterActions.InvokeContinuationDelegate, continuation,_task._token);
            else
                continuation();
        }

        /// <summary>
        /// If register manually continuation, you can use it instead of for compiler OnCompleted methods.
        /// </summary>
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SourceOnCompleted(Action<object?> continuation, object? state) {
            if (_task._source is { } s)
                s.OnCompleted(continuation, state, _task._token);
            else
                continuation(state);
        }
    }
}