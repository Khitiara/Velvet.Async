namespace Velvet.Async;

internal static class CompletedTasks
{
    public static readonly VelvetTask<AsyncUnit> AsyncUnit = VelvetTask.FromResult(Async.AsyncUnit.Default);
    public static readonly VelvetTask<bool>      True      = VelvetTask.FromResult(true);
    public static readonly VelvetTask<bool>      False     = VelvetTask.FromResult(false);
    public static readonly VelvetTask<int>       Zero      = VelvetTask.FromResult(0);
    public static readonly VelvetTask<int>       MinusOne  = VelvetTask.FromResult(-1);
    public static readonly VelvetTask<int>       One       = VelvetTask.FromResult(1);
}