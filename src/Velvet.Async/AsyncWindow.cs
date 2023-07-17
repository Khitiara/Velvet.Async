using Microsoft.CodeAnalysis.PooledObjects;
using Silk.NET.Windowing;

namespace Velvet.Async;

public static class AsyncWindow
{
    private sealed class TaskHolder
    {
        internal Task Task = Task.CompletedTask;
    }

    public static void Run(this IWindow window, Func<Task> loadAsync, Func<double, Task, VelvetTask> updateAsync,
        Func<double, Task, VelvetTask> renderAsync) {
        TaskHolder holder = new();
        using (PooledDelegates.GetPooledAction(t => {
                   (TaskHolder h, Func<Task> cb) = t;
                   VelvetTaskScheduler.Schedule(VelvetPlatformLoopStep.Initialization,
                       tuple => tuple.Item1.Task = tuple.Item2(),
                       (h, cb));
                   VelvetTaskScheduler.RunContinuations(VelvetPlatformLoopStep.Initialization);
                   VelvetTaskScheduler.RunContinuations(VelvetPlatformLoopStep.PostInitialization);
               }, (holder, loadAsync), out Action loadAction))
        using (PooledDelegates.GetPooledAction((dt, t) => {
                   (TaskHolder h, Func<double, Task, VelvetTask> cb) = t;
                   VelvetTiming.Update(dt);
                   VelvetTaskScheduler.Schedule(VelvetPlatformLoopStep.Update,
                       tuple => tuple.Item1(tuple.Item2, tuple.Item3).Forget(), (cb, dt, h.Task));
                   VelvetTaskScheduler.RunContinuations(VelvetPlatformLoopStep.PreUpdate);
                   VelvetTaskScheduler.RunContinuations(VelvetPlatformLoopStep.Update);
                   VelvetTaskScheduler.RunContinuations(VelvetPlatformLoopStep.PostUpdate);
               }, (holder, updateAsync), out Action<double> updateAction))
        using (PooledDelegates.GetPooledAction((dt, t) => {
                   (TaskHolder h, Func<double, Task, VelvetTask> cb) = t;
                   VelvetTiming.Render(dt);
                   VelvetTaskScheduler.Schedule(VelvetPlatformLoopStep.Render,
                       tuple => tuple.Item1(tuple.Item2, tuple.Item3).Forget(), (cb, dt, h.Task));
                   VelvetTaskScheduler.RunContinuations(VelvetPlatformLoopStep.PreRender);
                   VelvetTaskScheduler.RunContinuations(VelvetPlatformLoopStep.Render);
                   VelvetTaskScheduler.RunContinuations(VelvetPlatformLoopStep.PostRender);
               }, (holder, renderAsync), out Action<double> renderAction)) {
            window.Load += loadAction;
            window.Update += updateAction;
            window.Render += renderAction;

            window.Run();

            window.Load -= loadAction;
            window.Update -= updateAction;
            window.Render -= renderAction;
        }
    }
}