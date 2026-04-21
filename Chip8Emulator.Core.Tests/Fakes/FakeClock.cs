using System.Diagnostics;

namespace Chip8Emulator.Core.Tests.Fakes;

internal sealed class FakeClock : IClock
{
    public long Timestamp { get; set; }
    public long Frequency { get; set; } = Stopwatch.Frequency;
    public event EventHandler? Ticked;
    public void Tick() => Ticked?.Invoke(this, EventArgs.Empty);
}
