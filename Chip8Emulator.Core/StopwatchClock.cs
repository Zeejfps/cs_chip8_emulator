using System.Diagnostics;

namespace Chip8Emulator.Core;

public sealed class StopwatchClock : IClock
{
    public long Timestamp => Stopwatch.GetTimestamp();
    public long Frequency => Stopwatch.Frequency;
    public event EventHandler? Ticked;
    public void Tick() => Ticked?.Invoke(this, EventArgs.Empty);
}
