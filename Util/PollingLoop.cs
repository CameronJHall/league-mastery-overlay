using System;
using System.Threading;
using System.Threading.Tasks;

namespace league_mastery_overlay.Util;

public sealed class PollingLoop
{
    private readonly Func<Task> _tick;
    private readonly TimeSpan _interval;
    private CancellationTokenSource? _cts;

    public PollingLoop(Func<Task> tick, TimeSpan interval)
    {
        _tick = tick;
        _interval = interval;
    }

    public void Start()
    {
        _cts = new CancellationTokenSource();
        Task.Run(async () =>
        {
            while (!_cts.IsCancellationRequested)
            {
                await _tick();
                await Task.Delay(_interval);
            }
        });
    }

    public void Stop()
    {
        _cts?.Cancel();
    }
}