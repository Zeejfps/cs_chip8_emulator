namespace Chip8Emulator.Core;

public interface IAudio
{
    void PlaySound();
    void StopSound();
    void SetPattern(ReadOnlySpan<byte> pattern, double frequencyHz);
}
