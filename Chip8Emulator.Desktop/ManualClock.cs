using System.Diagnostics;
using Chip8Emulator.Core;

namespace Chip8Emulator.Desktop;

internal sealed class ManualClock : IClock
{
    private long _timestamp;

    public long Timestamp => _timestamp;
    public long Frequency => Stopwatch.Frequency;
    public event EventHandler? Ticked;

    public void Advance(long delta)
    {
        if (delta <= 0) return;
        _timestamp += delta;
        Ticked?.Invoke(this, EventArgs.Empty);
    }
}
