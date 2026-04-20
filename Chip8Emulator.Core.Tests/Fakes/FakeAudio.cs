namespace Chip8Emulator.Core.Tests.Fakes;

internal sealed class FakeAudio : IAudio
{
    public int BeepCount { get; private set; }

    public void Beep() => BeepCount++;
}
