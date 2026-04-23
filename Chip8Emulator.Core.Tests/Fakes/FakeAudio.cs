namespace Chip8Emulator.Core.Tests.Fakes;

internal sealed class FakeAudio : IAudio
{
    public int PlayCount { get; private set; }
    public int StopCount { get; private set; }
    public int ResetCount { get; private set; }
    public int WritePatternCount { get; private set; }
    public bool IsPlaying { get; private set; }
    public byte Pitch { get; set; }
    public byte[] LastPattern { get; private set; } = [];

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

    public void Reset()
    {
        ResetCount++;
    }

    public void WritePattern(Action<Span<byte>> writeAction)
    {
        var buffer = new byte[16];
        writeAction(buffer.AsSpan());
        LastPattern = buffer;
        WritePatternCount++;
    }
}
