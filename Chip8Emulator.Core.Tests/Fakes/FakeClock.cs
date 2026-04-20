namespace Chip8Emulator.Core.Tests.Fakes;

internal sealed class FakeClock : IClock
{
    public double ElapsedTimeInSeconds { get; set; }

    public double GetElapsedTimeInSeconds() => ElapsedTimeInSeconds;
}
