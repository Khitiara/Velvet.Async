using System.Runtime.CompilerServices;

namespace Velvet.Async;

public static class VelvetTaskStatusExtensions
{
    /// <summary>status != Pending.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsCompleted(this VelvetTaskStatus status) => status != VelvetTaskStatus.Pending;

    /// <summary>status == Succeeded.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsCompletedSuccessfully(this VelvetTaskStatus status) => status == VelvetTaskStatus.Succeeded;

    /// <summary>status == Canceled.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsCanceled(this VelvetTaskStatus status) => status == VelvetTaskStatus.Canceled;

    /// <summary>status == Faulted.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsFaulted(this VelvetTaskStatus status) => status == VelvetTaskStatus.Faulted;
}