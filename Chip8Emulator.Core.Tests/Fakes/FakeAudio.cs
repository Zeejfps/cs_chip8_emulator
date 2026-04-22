namespace Chip8Emulator.Core.Tests.Fakes;

internal sealed class FakeAudio : IAudio
{
    public int PlayCount { get; private set; }
    public int StopCount { get; private set; }
    public bool IsPlaying { get; private set; }
    public byte[] LastPattern { get; private set; } = [];
    public double LastFrequencyHz { get; private set; }
    public int SetPatternCount { get; private set; }

    public void PlaySound()
    {
        PlayCount++;
        IsPlaying = true;
    }

    public void StopSound()
    {
        StopCount++;
        IsPlaying = false;
    }

    public void SetPattern(ReadOnlySpan<byte> pattern, double frequencyHz)
    {
        LastPattern = pattern.ToArray();
        LastFrequencyHz = frequencyHz;
        SetPatternCount++;
    }
}
