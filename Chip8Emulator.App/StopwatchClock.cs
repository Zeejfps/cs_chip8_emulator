using System.Diagnostics;
using Chip8Emulator.Core;

namespace Chip8Emulator.App;

public sealed class StopwatchClock : IClock
{
    public long Timestamp => Stopwatch.GetTimestamp();
    public long Frequency { get; } = Stopwatch.Frequency;
    public event EventHandler? Ticked;
    public void Tick() => Ticked?.Invoke(this, EventArgs.Empty);
}
