using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;

namespace Velvet.Async;

[StructLayout(LayoutKind.Auto)]
public struct ManualResetVelvetTaskSourceCore<TResult>
{
    // Struct Size: TResult + (8 + 2 + 1 + 1 + 8 + 8)

    private TResult?         _result;
    private object?          _error; // ExceptionHolder or OperationCanceledException
    private bool             _hasUnhandledError;
    private int              _completedCount; // 0: completed == false
    private Action<object?>? _continuation;
    private object?          _continuationState;

    [DebuggerHidden]
    public void Reset() {
        ReportUnhandledError();

        unchecked {
            Version += 1; // incr version.
        }

        _completedCount = 0;
        _result = default;
        _error = null;
        _hasUnhandledError = false;
        _continuation = null;
        _continuationState = null;
    }

    private void ReportUnhandledError() {
        if (!_hasUnhandledError) return;
        try {
            if (_error is OperationCanceledException oc) {
                VelvetTaskScheduler.PublishUnobservedTaskException(oc);
            } else if (_error is ExceptionHolder e) {
                VelvetTaskScheduler.PublishUnobservedTaskException(e.GetException().SourceException);
            }
        }
        catch {
            // ignored
        }
    }

    internal void MarkHandled() {
        _hasUnhandledError = false;
    }

    /// <summary>Completes with a successful result.</summary>
    /// <param name="result">The result.</param>
    [DebuggerHidden]
    public bool TrySetResult(TResult result) {
        if (Interlocked.Increment(ref _completedCount) != 1) return false;
        // setup result
        this._result = result;

        if (_continuation == null && Interlocked.CompareExchange(ref _continuation,
                ManualResetVelvetTaskSourceCoreShared.Sentinel, null) == null) return false;
        _continuation(_continuationState);
        return true;
    }

    /// <summary>Completes with an error.</summary>
    /// <param name="error">The exception.</param>
    [DebuggerHidden]
    public bool TrySetException(Exception error) {
        if (Interlocked.Increment(ref _completedCount) != 1) return false;
        // setup result
        this._hasUnhandledError = true;
        if (error is OperationCanceledException) {
            this._error = error;
        } else {
            this._error = new ExceptionHolder(ExceptionDispatchInfo.Capture(error));
        }

        if (_continuation == null && Interlocked.CompareExchange(ref this._continuation,
                ManualResetVelvetTaskSourceCoreShared.Sentinel, null) == null) return false;
        _continuation(_continuationState);
        return true;
    }

    [DebuggerHidden]
    public bool TrySetCanceled(CancellationToken cancellationToken = default) {
        if (Interlocked.Increment(ref _completedCount) != 1) return false;
        // setup result
        this._hasUnhandledError = true;
        this._error = new OperationCanceledException(cancellationToken);

        if (_continuation == null && Interlocked.CompareExchange(ref this._continuation,
                ManualResetVelvetTaskSourceCoreShared.Sentinel, null) == null) return false;
        _continuation(_continuationState);
        return true;
    }

    /// <summary>Gets the operation version.</summary>
    [DebuggerHidden]
    public short Version { get; private set; }

    /// <summary>Gets the status of the operation.</summary>
    /// <param name="token">Opaque value that was provided to the <see cref="VelvetTask"/>'s constructor.</param>
    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public VelvetTaskStatus GetStatus(short token) {
        ValidateToken(token);
        return _continuation == null || _completedCount == 0 ? VelvetTaskStatus.Pending
            : _error == null ? VelvetTaskStatus.Succeeded
            : _error is OperationCanceledException ? VelvetTaskStatus.Canceled
            : VelvetTaskStatus.Faulted;
    }

    /// <summary>Gets the status of the operation without token validation.</summary>
    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public VelvetTaskStatus UnsafeGetStatus() =>
        _continuation == null || _completedCount == 0 ? VelvetTaskStatus.Pending
        : _error == null ? VelvetTaskStatus.Succeeded
        : _error is OperationCanceledException ? VelvetTaskStatus.Canceled
        : VelvetTaskStatus.Faulted;

    /// <summary>Gets the result of the operation.</summary>
    /// <param name="token">Opaque value that was provided to the <see cref="VelvetTask"/>'s constructor.</param>
    // [StackTraceHidden]
    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TResult GetResult(short token) {
        ValidateToken(token);
        if (_completedCount == 0) {
            throw new InvalidOperationException("Not yet completed, VelvetTask only allow to use await.");
        }

        if (_error == null) return _result!;
        _hasUnhandledError = false;
        switch (_error) {
            case OperationCanceledException oce:
                throw oce;
            case ExceptionHolder eh:
                eh.GetException().Throw();
                break;
        }

        throw new InvalidOperationException("Critical: invalid exception type was held.");
    }

    /// <summary>Schedules the continuation action for this operation.</summary>
    /// <param name="continuation">The continuation to invoke when the operation has completed.</param>
    /// <param name="state">The state object to pass to <paramref name="continuation"/> when it's invoked.</param>
    /// <param name="token">Opaque value that was provided to the <see cref="VelvetTask"/>'s constructor.</param>
    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void OnCompleted(Action<object?> continuation, object? state, short token /*, ValueTaskSourceOnCompletedFlags flags */) {
        if (continuation == null) {
            throw new ArgumentNullException(nameof(continuation));
        }

        ValidateToken(token);

        /* no use ValueTaskSourceOnCompletedFlags, always no capture ExecutionContext and SynchronizationContext. */

        /*
            PatternA: GetStatus=Pending => OnCompleted => TrySet*** => GetResult
            PatternB: TrySet*** => GetStatus=!Pending => GetResult
            PatternC: GetStatus=Pending => TrySet/OnCompleted(race condition) => GetResult
            C.1: win OnCompleted -> TrySet invoke saved continuation
            C.2: win TrySet -> should invoke continuation here.
        */

        // not set continuation yet.
        object? oldContinuation = _continuation;
        if (oldContinuation == null) {
            _continuationState = state;
            oldContinuation = Interlocked.CompareExchange(ref _continuation, continuation, null);
        }

        if (oldContinuation == null) return;
        // already running continuation in TrySet.
        // It will cause call OnCompleted multiple time, invalid.
        if (!ReferenceEquals(oldContinuation, ManualResetVelvetTaskSourceCoreShared.Sentinel)) {
            throw new InvalidOperationException(
                "Already continuation registered, can not await twice or get Status after await.");
        }

        continuation(state);
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ValidateToken(short token) {
        if (token != Version) {
            throw new InvalidOperationException(
                "Token version is not matched, can not await twice or get Status after await.");
        }
    }
}