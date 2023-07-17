using System.Diagnostics;
using System.Runtime.ExceptionServices;

namespace Velvet.Async;

public class VelvetTaskCompletionSource : IVelvetTaskSource
{
    private CancellationToken                 _cancellationToken;
    private ExceptionHolder?                  _exception;
    private object?                           _gate;
    private Action<object?>?                  _singleContinuation;
    private object?                           _singleState;
    private List<(Action<object?>, object?)>? _secondaryContinuationList;

    private int  _intStatus; // VelvetTaskStatus
    private bool _handled;

    [DebuggerHidden]
    internal void MarkHandled() {
        if (!_handled) {
            _handled = true;
        }
    }

    public VelvetTask Task {
        [DebuggerHidden] get => new(this, 0);
    }

    [DebuggerHidden]
    public bool TrySetResult() => TrySignalCompletion(VelvetTaskStatus.Succeeded);

    [DebuggerHidden]
    public bool TrySetCanceled(CancellationToken cancellationToken = default) {
        if (UnsafeGetStatus() != VelvetTaskStatus.Pending) return false;

        _cancellationToken = cancellationToken;
        return TrySignalCompletion(VelvetTaskStatus.Canceled);
    }

    [DebuggerHidden]
    public bool TrySetException(Exception exception) {
        if (exception is OperationCanceledException oce) {
            return TrySetCanceled(oce.CancellationToken);
        }

        if (UnsafeGetStatus() != VelvetTaskStatus.Pending) return false;

        _exception = new ExceptionHolder(ExceptionDispatchInfo.Capture(exception));
        return TrySignalCompletion(VelvetTaskStatus.Faulted);
    }

    [DebuggerHidden]
    public void GetResult(short token) {
        MarkHandled();

        VelvetTaskStatus status = (VelvetTaskStatus)_intStatus;
        switch (status) {
            case VelvetTaskStatus.Succeeded:
                return;
            case VelvetTaskStatus.Faulted:
                _exception?.GetException().Throw();
                return;
            case VelvetTaskStatus.Canceled:
                throw new OperationCanceledException(_cancellationToken);
            default:
            case VelvetTaskStatus.Pending:
                throw new InvalidOperationException("not yet completed.");
        }
    }

    [DebuggerHidden]
    public VelvetTaskStatus GetStatus(short token) => (VelvetTaskStatus)_intStatus;

    [DebuggerHidden]
    public VelvetTaskStatus UnsafeGetStatus() => (VelvetTaskStatus)_intStatus;

    [DebuggerHidden]
    public void OnCompleted(Action<object?> continuation, object? state, short token) {
        if (_gate == null) {
            Interlocked.CompareExchange(ref _gate, new object(), null);
        }

        object lockGate = Thread.VolatileRead(ref _gate);
        lock (lockGate) // wait TrySignalCompletion, after status is not pending.
        {
            if ((VelvetTaskStatus)_intStatus != VelvetTaskStatus.Pending) {
                continuation(state);
                return;
            }

            if (_singleContinuation == null) {
                _singleContinuation = continuation;
                _singleState = state;
            } else {
                if (_secondaryContinuationList == null) {
                    _secondaryContinuationList = new List<(Action<object?>, object?)>();
                }

                _secondaryContinuationList.Add((continuation, state));
            }
        }
    }

    [DebuggerHidden]
    private bool TrySignalCompletion(VelvetTaskStatus status) {
        if (Interlocked.CompareExchange(ref _intStatus, (int)status, (int)VelvetTaskStatus.Pending) !=
            (int)VelvetTaskStatus.Pending) return false;
        if (_gate == null) {
            Interlocked.CompareExchange(ref _gate, new object(), null);
        }

        object lockGate = Thread.VolatileRead(ref _gate);
        lock (lockGate) // wait OnCompleted.
        {
            if (_singleContinuation != null) {
                try {
                    _singleContinuation(_singleState);
                }
                catch (Exception ex) {
                    VelvetTaskScheduler.PublishUnobservedTaskException(ex);
                }
            }

            if (_secondaryContinuationList != null) {
                foreach ((Action<object?> c, object? state) in _secondaryContinuationList) {
                    try {
                        c(state);
                    }
                    catch (Exception ex) {
                        VelvetTaskScheduler.PublishUnobservedTaskException(ex);
                    }
                }
            }

            _singleContinuation = null;
            _singleState = null;
            _secondaryContinuationList = null;
        }

        return true;

    }
}