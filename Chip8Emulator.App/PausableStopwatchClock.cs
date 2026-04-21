using System.Diagnostics;
using Chip8Emulator.Core;

namespace Chip8Emulator.App;

public sealed class PausableStopwatchClock : IClock
{
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private long _offset;
    private long _pausedAt;
    private bool _paused;

    public long Frequency => Stopwatch.Frequency;

    public long Timestamp => (_paused ? _pausedAt : _stopwatch.ElapsedTicks) - _offset;

    public void Pause()
    {
        if (_paused) return;
        _pausedAt = _stopwatch.ElapsedTicks;
        _paused = true;
    }

    public void Resume()
    {
        if (!_paused) return;
        _offset += _stopwatch.ElapsedTicks - _pausedAt;
        _paused = false;
    }

    public void Advance(long ticks) => _offset -= ticks;
}
