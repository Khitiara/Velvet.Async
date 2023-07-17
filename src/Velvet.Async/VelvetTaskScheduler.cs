using System.Runtime.ExceptionServices;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Velvet.Async;

public static partial class VelvetTaskScheduler
{
    private readonly struct ScheduledContinuation
    {
        private readonly  VelvetPlatformStepMask  _steps;
        internal readonly Action                    RunScheduledAction;
        internal readonly PooledDelegates.Releaser? Disposable;

        public ScheduledContinuation(VelvetPlatformStepMask steps, Action runScheduledAction, PooledDelegates.Releaser? disposable = null) {
            _steps = steps;
            RunScheduledAction = runScheduledAction;
            Disposable = disposable;
        }

        public bool ShouldRun(VelvetPlatformLoopStep step) => _steps.HasStep(step);
    }
    
    public static bool IsOnMainThread => Environment.CurrentManagedThreadId == MainThreadId;
    public static int MainThreadId { get; internal set; }
    
    private static readonly LinkedList<ScheduledContinuation> Continuations = new();
    private static readonly LinkedList<ScheduledContinuation> Drain        = new();

    internal static readonly Action<VelvetPlatformLoopStep> RunAction = RunContinuations;
    internal static void RunContinuations(VelvetPlatformLoopStep step) {
        lock (Continuations) {
            for (LinkedListNode<ScheduledContinuation>? node = Continuations.First, next = node?.Next; node != null; node = next) {
                ref ScheduledContinuation continuation = ref node.ValueRef;
                if (!continuation.ShouldRun(step)) continue;
                Drain.AddLast(continuation);
                Continuations.Remove(node);
            }
        }

        foreach (ScheduledContinuation action in Drain) {
            action.RunScheduledAction();
            action.Disposable?.Dispose();
        }

        Drain.Clear();
    }

    public static void PublishUnobservedTaskException(Exception exception) {
        Schedule(VelvetPlatformStepMask.All, e => e.Throw(), ExceptionDispatchInfo.Capture(exception));
    }
}