using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Velvet.Async.CompilerServices;

namespace Velvet.Async;

[AsyncMethodBuilder(typeof(VelvetTaskAsyncMethodBuilder))]
[StructLayout(LayoutKind.Auto)]
public readonly partial struct VelvetTask
{
    private readonly IVelvetTaskSource? _source;
    private readonly short                _token;

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public VelvetTask(IVelvetTaskSource source, short token) {
        _source = source;
        _token = token;
    }

    public VelvetTaskStatus Status {
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _source?.GetStatus(_token) ?? VelvetTaskStatus.Succeeded;
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Awaiter GetAwaiter() => new(this);

    public readonly struct Awaiter : ICriticalNotifyCompletion
    {
        private readonly VelvetTask _task;

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Awaiter(in VelvetTask task) {
            _task = task;
        }

        public bool IsCompleted {
            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _task.Status.IsCompleted();
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GetResult() {
            _task._source?.GetResult(_task._token);
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnCompleted(Action continuation) {
            if (_task._source == null) {
                continuation();
            } else {
                _task._source.OnCompleted(AwaiterActions.InvokeContinuationDelegate, continuation, _task._token);
            }
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnsafeOnCompleted(Action continuation) {
            if (_task._source == null) {
                continuation();
            } else {
                _task._source.OnCompleted(AwaiterActions.InvokeContinuationDelegate, continuation, _task._token);
            }
        }

        /// <summary>
        /// If register manually continuation, you can use it instead of for compiler OnCompleted methods.
        /// </summary>
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SourceOnCompleted(Action<object?> continuation, object? state) {
            if (_task._source == null) {
                continuation(state);
            } else {
                _task._source.OnCompleted(continuation, state, _task._token);
            }
        }
    }
}