using System.Diagnostics.CodeAnalysis;

namespace Velvet.Async;

[SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters")]
public readonly partial struct VelvetTask
{
    /// <summary>Run action on the threadPool and return to main thread if configureAwait = true.</summary>
    public static async VelvetTask RunOnThreadPool(Action action, bool configureAwait = true,
        CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();

        await SwitchToThreadPool();

        cancellationToken.ThrowIfCancellationRequested();

        if (configureAwait) {
            try {
                action();
            }
            finally {
                await Yield();
            }
        } else {
            action();
        }

        cancellationToken.ThrowIfCancellationRequested();
    }

    /// <summary>Run action on the threadPool and return to main thread if configureAwait = true.</summary>
    public static async VelvetTask RunOnThreadPool<TArg>(Action<TArg> action, TArg state, bool configureAwait = true,
        CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();

        await SwitchToThreadPool();

        cancellationToken.ThrowIfCancellationRequested();

        if (configureAwait) {
            try {
                action(state);
            }
            finally {
                await Yield();
            }
        } else {
            action(state);
        }

        cancellationToken.ThrowIfCancellationRequested();
    }

    /// <summary>Run action on the threadPool and return to main thread if configureAwait = true.</summary>
    public static async VelvetTask RunOnThreadPool(Func<VelvetTask> action, bool configureAwait = true,
        CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();

        await SwitchToThreadPool();

        cancellationToken.ThrowIfCancellationRequested();

        if (configureAwait) {
            try {
                await action();
            }
            finally {
                await Yield();
            }
        } else {
            await action();
        }

        cancellationToken.ThrowIfCancellationRequested();
    }

    /// <summary>Run action on the threadPool and return to main thread if configureAwait = true.</summary>
    public static async VelvetTask RunOnThreadPool(Func<object, VelvetTask> action, object state,
        bool configureAwait = true, CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();

        await SwitchToThreadPool();

        cancellationToken.ThrowIfCancellationRequested();

        if (configureAwait) {
            try {
                await action(state);
            }
            finally {
                await Yield();
            }
        } else {
            await action(state);
        }

        cancellationToken.ThrowIfCancellationRequested();
    }

    /// <summary>Run action on the threadPool and return to main thread if configureAwait = true.</summary>
    public static async VelvetTask<T> RunOnThreadPool<T>(Func<T> func, bool configureAwait = true,
        CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();

        await SwitchToThreadPool();

        cancellationToken.ThrowIfCancellationRequested();

        if (configureAwait) {
            try {
                return func();
            }
            finally {
                await Yield();
                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        return func();
    }

    /// <summary>Run action on the threadPool and return to main thread if configureAwait = true.</summary>
    public static async VelvetTask<T> RunOnThreadPool<T>(Func<VelvetTask<T>> func, bool configureAwait = true,
        CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();

        await SwitchToThreadPool();

        cancellationToken.ThrowIfCancellationRequested();

        if (configureAwait) {
            try {
                return await func();
            }
            finally {
                cancellationToken.ThrowIfCancellationRequested();
                await Yield();
                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        T result = await func();
        cancellationToken.ThrowIfCancellationRequested();
        return result;
    }

    /// <summary>Run action on the threadPool and return to main thread if configureAwait = true.</summary>
    public static async VelvetTask<T> RunOnThreadPool<T, TArg>(Func<TArg, T> func, TArg state,
        bool configureAwait = true, CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();

        await SwitchToThreadPool();

        cancellationToken.ThrowIfCancellationRequested();

        if (configureAwait) {
            try {
                return func(state);
            }
            finally {
                await Yield();
                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        return func(state);
    }

    /// <summary>Run action on the threadPool and return to main thread if configureAwait = true.</summary>
    public static async VelvetTask<T> RunOnThreadPool<T, TArg>(Func<TArg, VelvetTask<T>> func, TArg state,
        bool configureAwait = true, CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();

        await SwitchToThreadPool();

        cancellationToken.ThrowIfCancellationRequested();

        if (configureAwait) {
            try {
                return await func(state);
            }
            finally {
                cancellationToken.ThrowIfCancellationRequested();
                await Yield();
                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        T result = await func(state);
        cancellationToken.ThrowIfCancellationRequested();
        return result;
    }
}