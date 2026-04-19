using Emulator.Api;

namespace Emulator.Tests.Fakes;

internal sealed class FakeAudio : IAudio
{
    public int BeepCount { get; private set; }

    public void Beep() => BeepCount++;
}
