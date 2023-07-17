using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Velvet.Async;

internal static class AwaiterActions
{
    internal static readonly Action<object?> InvokeContinuationDelegate = Continuation;

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Continuation(object? state)
    {
        ((Action?)state)?.Invoke();
    }
}