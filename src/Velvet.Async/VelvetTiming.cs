namespace Velvet.Async;

public static class VelvetTiming
{
    public static TimeSpan TotalTime { get; private set; }
    public static TimeSpan TimeSinceLastUpdate { get; private set; }
    public static TimeSpan TimeSinceLastRender { get; private set; }
    
    public static ulong FrameCount { get; private set; }
    
    public static double FramesPerSecond { get; private set; }
    
    public static double UpdatesPerSecond { get; private set; }

    internal static void Update(double dt) {
        VelvetTask.TotalElapsed = TotalTime += TimeSinceLastUpdate = TimeSpan.FromSeconds(dt);
        UpdatesPerSecond = 1 / dt;
    }

    internal static void Render(double dt) {
        VelvetTask.Frame = ++FrameCount;
        TimeSinceLastRender = TimeSpan.FromSeconds(dt);
        FramesPerSecond = 1 / dt;
    }
}