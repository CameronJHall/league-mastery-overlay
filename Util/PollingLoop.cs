using System.Diagnostics;

namespace league_mastery_overlay.Util;

public sealed class PollingLoop
{
    private readonly Func<Task> _tick;
    private readonly TimeSpan _interval;
    private CancellationTokenSource? _cts;
    private Task? _loopTask;

    public bool IsRunning => _cts != null && !_cts.IsCancellationRequested;

    public PollingLoop(Func<Task> tick, TimeSpan interval)
    {
        _tick = tick;
        _interval = interval;
    }

    public void Start()
    {
        _cts = new CancellationTokenSource();
        _loopTask = Task.Run(async () =>
        {
            Debug.WriteLine("[PollingLoop] Started");
            
            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    await _tick();
                }
                catch (Exception ex)
                {
                    // Catch exceptions from the tick function so the loop continues
                    Debug.WriteLine($"[PollingLoop] Tick exception: {ex.GetType().Name}: {ex.Message}");
                    Debug.WriteLine($"  StackTrace: {ex.StackTrace}");
                }

                try
                {
                    await Task.Delay(_interval, _cts.Token);
                }
                catch (OperationCanceledException)
                {
                    // Expected when stopping
                    Debug.WriteLine("[PollingLoop] Cancelled");
                    break;
                }
            }
            
            Debug.WriteLine("[PollingLoop] Exited");
        }, _cts.Token);
    }

    public void Stop()
    {
        Debug.WriteLine("[PollingLoop] Stopping...");
        _cts?.Cancel();
        _loopTask?.Wait(TimeSpan.FromSeconds(2));
    }
}