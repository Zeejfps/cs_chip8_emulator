using System.Diagnostics;
using Chip8Emulator.Core;

namespace Chip8Emulator.App;

public sealed class StopwatchClock : IClock
{
    private readonly Stopwatch _stopwatch = new();
    private double _lastElapsedSeconds;

    public void Start()
    {
        _stopwatch.Start();
    }
    
    public double GetElapsedTimeInSeconds()
    {
        var now = _stopwatch.Elapsed.TotalSeconds;
        var delta = now - _lastElapsedSeconds;
        _lastElapsedSeconds = now;
        return delta;
    }
}
