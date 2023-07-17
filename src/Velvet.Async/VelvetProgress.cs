// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Velvet.Async;

/// <summary>
/// Provides an IProgress{T} that invokes callbacks for each reported progress value.
/// </summary>
/// <typeparam name="T">Specifies the type of the progress report value.</typeparam>
/// <remarks>
/// Any handler provided to the constructor or event handlers registered with
/// the <see cref="ProgressChanged"/> event are invoked through a
/// <see cref="System.Threading.SynchronizationContext"/> instance captured
/// when the instance is constructed.  If there is no current SynchronizationContext
/// at the time of construction, the callbacks will be invoked on the ThreadPool.
/// </remarks>
public class VelvetProgress<T> : IProgress<T>
{
    /// <summary>The handler specified to the constructor.  This may be null.</summary>
    private readonly Action<T>? _handler;
    /// <summary>A cached delegate used to post invocation to the synchronization context.</summary>
    private readonly Action<T> _invokeHandlers;

    /// <summary>Initializes the <see cref="VelvetProgress{T}"/>.</summary>
    public VelvetProgress()
    {
        _invokeHandlers = InvokeHandlers;
    }

    /// <summary>Initializes the <see cref="VelvetProgress{T}"/> with the specified callback.</summary>
    /// <param name="handler">
    /// A handler to invoke for each reported progress value.  This handler will be invoked
    /// in addition to any delegates registered with the <see cref="ProgressChanged"/> event.
    /// Depending on the <see cref="System.Threading.SynchronizationContext"/> instance captured by
    /// the <see cref="VelvetProgress{T}"/> at construction, it's possible that this handler instance
    /// could be invoked concurrently with itself.
    /// </param>
    /// <exception cref="System.ArgumentNullException">The <paramref name="handler"/> is null (Nothing in Visual Basic).</exception>
    public VelvetProgress(Action<T> handler) : this()
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    /// <summary>Raised for each reported progress value.</summary>
    /// <remarks>
    /// Handlers registered with this event will be invoked on the
    /// <see cref="System.Threading.SynchronizationContext"/> captured when the instance was constructed.
    /// </remarks>
    public event EventHandler<T>? ProgressChanged;

    /// <summary>Reports a progress change.</summary>
    /// <param name="value">The value of the updated progress.</param>
    protected virtual void OnReport(T value)
    {
        // If there's no handler, don't bother going through the sync context.
        // Inside the callback, we'll need to check again, in case
        // an event handler is removed between now and then.
        Action<T>? handler = _handler;
        EventHandler<T>? changedEvent = ProgressChanged;
        if (handler != null || changedEvent != null)
            VelvetTaskScheduler.Schedule(VelvetPlatformStepMask.All, _invokeHandlers, value);
    }

    /// <summary>Reports a progress change.</summary>
    /// <param name="value">The value of the updated progress.</param>
    void IProgress<T>.Report(T value) { OnReport(value); }

    /// <summary>Invokes the action and event callbacks.</summary>
    /// <param name="state">The progress value.</param>
    private void InvokeHandlers(T state)
    {
        Action<T>? handler = _handler;
        EventHandler<T>? changedEvent = ProgressChanged;

        handler?.Invoke(state);
        changedEvent?.Invoke(this, state);
    }
}