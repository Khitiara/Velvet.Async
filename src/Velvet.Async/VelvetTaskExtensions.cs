using System.Diagnostics.CodeAnalysis;
using Velvet.Async.State;

namespace Velvet.Async;

public static class VelvetTaskExtensions
{
    public static Task<T> AsTask<T>(this VelvetTask<T> task) {
        try {
            VelvetTask<T>.Awaiter awaiter;
            try {
                awaiter = task.GetAwaiter();
            }
            catch (Exception ex) {
                return Task.FromException<T>(ex);
            }

            if (awaiter.IsCompleted) {
                try {
                    T result = awaiter.GetResult();
                    return Task.FromResult(result);
                }
                catch (Exception ex) {
                    return Task.FromException<T>(ex);
                }
            }

            TaskCompletionSource<T> tcs = new();

            awaiter.SourceOnCompleted(state => {
                using StateTuple<TaskCompletionSource<T>, VelvetTask<T>.Awaiter> tuple =
                    (StateTuple<TaskCompletionSource<T>, VelvetTask<T>.Awaiter>)state!;
                (TaskCompletionSource<T> inTcs, VelvetTask<T>.Awaiter inAwaiter) = tuple;
                try {
                    T result = inAwaiter.GetResult();
                    inTcs.SetResult(result);
                }
                catch (Exception ex) {
                    inTcs.SetException(ex);
                }
            }, StateTuple.Create(tcs, awaiter));

            return tcs.Task;
        }
        catch (Exception ex) {
            return Task.FromException<T>(ex);
        }
    }

    public static Task AsTask(this VelvetTask task) {
        try {
            VelvetTask.Awaiter awaiter;
            try {
                awaiter = task.GetAwaiter();
            }
            catch (Exception ex) {
                return Task.FromException(ex);
            }

            if (awaiter.IsCompleted) {
                try {
                    awaiter.GetResult(); // check token valid on Succeeded
                    return Task.CompletedTask;
                }
                catch (Exception ex) {
                    return Task.FromException(ex);
                }
            }

            TaskCompletionSource<object> tcs = new();

            awaiter.SourceOnCompleted(state => {
                using StateTuple<TaskCompletionSource<object>, VelvetTask.Awaiter> tuple =
                    (StateTuple<TaskCompletionSource<object>, VelvetTask.Awaiter>)state!;
                (TaskCompletionSource<object> inTcs, VelvetTask.Awaiter inAwaiter) = tuple;
                try {
                    inAwaiter.GetResult();
                    inTcs.SetResult(null!);
                }
                catch (Exception ex) {
                    inTcs.SetException(ex);
                }
            }, StateTuple.Create(tcs, awaiter));

            return tcs.Task;
        }
        catch (Exception ex) {
            return Task.FromException(ex);
        }
    }

    public static void Forget(this VelvetTask task) {
        VelvetTask.Awaiter awaiter = task.GetAwaiter();
        if (awaiter.IsCompleted) {
            try {
                awaiter.GetResult();
            }
            catch (Exception ex) {
                VelvetTaskScheduler.PublishUnobservedTaskException(ex);
            }
        } else {
            awaiter.SourceOnCompleted(state => {
                using StateTuple<VelvetTask.Awaiter> t = (StateTuple<VelvetTask.Awaiter>)state!;
                try {
                    t.Item1.GetResult();
                }
                catch (Exception ex) {
                    VelvetTaskScheduler.PublishUnobservedTaskException(ex);
                }
            }, StateTuple.Create(awaiter));
        }
    }

    public static void Forget(this VelvetTask task, Action<Exception> exceptionHandler,
        bool handleExceptionOnMainThread = true) {
        if (exceptionHandler == null!) {
            Forget(task);
        } else {
            ForgetCoreWithCatch(task, exceptionHandler, handleExceptionOnMainThread).Forget();
        }
    }

    private static async VelvetVoidTask ForgetCoreWithCatch(VelvetTask task, Action<Exception> exceptionHandler,
        bool handleExceptionOnMainThread) {
        try {
            await task;
        }
        catch (Exception ex) {
            try {
                if (handleExceptionOnMainThread) {
                    await VelvetTask.SwitchToMainThread();
                }

                exceptionHandler(ex);
            }
            catch (Exception ex2) {
                VelvetTaskScheduler.PublishUnobservedTaskException(ex2);
            }
        }
    }

    public static void Forget<T>(this VelvetTask<T> task) {
        VelvetTask<T>.Awaiter awaiter = task.GetAwaiter();
        if (awaiter.IsCompleted) {
            try {
                awaiter.GetResult();
            }
            catch (Exception ex) {
                VelvetTaskScheduler.PublishUnobservedTaskException(ex);
            }
        } else {
            awaiter.SourceOnCompleted(state => {
                using StateTuple<VelvetTask<T>.Awaiter> t = (StateTuple<VelvetTask<T>.Awaiter>)state!;
                try {
                    t.Item1.GetResult();
                }
                catch (Exception ex) {
                    VelvetTaskScheduler.PublishUnobservedTaskException(ex);
                }
            }, StateTuple.Create(awaiter));
        }
    }

    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters")]
    public static void Forget<T>(this VelvetTask<T> task, Action<Exception> exceptionHandler,
        bool handleExceptionOnMainThread = true) {
        if (exceptionHandler == null!) {
            task.Forget();
        } else {
            ForgetCoreWithCatch(task, exceptionHandler, handleExceptionOnMainThread).Forget();
        }
    }

    private static async VelvetVoidTask ForgetCoreWithCatch<T>(VelvetTask<T> task, Action<Exception> exceptionHandler,
        bool handleExceptionOnMainThread) {
        try {
            await task;
        }
        catch (Exception ex) {
            try {
                if (handleExceptionOnMainThread) {
                    await VelvetTask.SwitchToMainThread();
                }

                exceptionHandler(ex);
            }
            catch (Exception ex2) {
                VelvetTaskScheduler.PublishUnobservedTaskException(ex2);
            }
        }
    }

    public static async VelvetTask ContinueWith<T>(this VelvetTask<T> task, Action<T> continuationFunction) =>
        continuationFunction(await task);

    public static async VelvetTask ContinueWith<T>(this VelvetTask<T> task,
        Func<T, VelvetTask> continuationFunction) =>
        await continuationFunction(await task);

    public static async VelvetTask<TR> ContinueWith<T, TR>(this VelvetTask<T> task,
        Func<T, TR> continuationFunction) =>
        continuationFunction(await task);

    public static async VelvetTask<TR> ContinueWith<T, TR>(this VelvetTask<T> task,
        Func<T, VelvetTask<TR>> continuationFunction) =>
        await continuationFunction(await task);

    public static async VelvetTask ContinueWith(this VelvetTask task, Action continuationFunction) {
        await task;
        continuationFunction();
    }

    public static async VelvetTask ContinueWith(this VelvetTask task, Func<VelvetTask> continuationFunction) {
        await task;
        await continuationFunction();
    }

    public static async VelvetTask<T> ContinueWith<T>(this VelvetTask task, Func<T> continuationFunction) {
        await task;
        return continuationFunction();
    }

    public static async VelvetTask<T> ContinueWith<T>(this VelvetTask task,
        Func<VelvetTask<T>> continuationFunction) {
        await task;
        return await continuationFunction();
    }

    public static async VelvetTask<T> Unwrap<T>(this VelvetTask<VelvetTask<T>> task) => await await task;

    public static async VelvetTask Unwrap(this VelvetTask<VelvetTask> task) => await await task;

    public static async VelvetTask<T> Unwrap<T>(this Task<VelvetTask<T>> task) => await await task;

    public static async VelvetTask<T> Unwrap<T>(this Task<VelvetTask<T>> task, bool continueOnCapturedContext) =>
        await await task.ConfigureAwait(continueOnCapturedContext);

    public static async VelvetTask Unwrap(this Task<VelvetTask> task) => await await task;

    public static async VelvetTask Unwrap(this Task<VelvetTask> task, bool continueOnCapturedContext) =>
        await await task.ConfigureAwait(continueOnCapturedContext);

    public static async VelvetTask<T> Unwrap<T>(this VelvetTask<Task<T>> task) => await await task;

    public static async VelvetTask<T> Unwrap<T>(this VelvetTask<Task<T>> task, bool continueOnCapturedContext) =>
        await (await task).ConfigureAwait(continueOnCapturedContext);

    public static async VelvetTask Unwrap(this VelvetTask<Task> task) => await await task;

    public static async VelvetTask Unwrap(this VelvetTask<Task> task, bool continueOnCapturedContext) =>
        await (await task).ConfigureAwait(continueOnCapturedContext);
}