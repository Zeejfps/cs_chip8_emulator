namespace Chip8Emulator.Core;

public interface IAudio
{
    byte Pitch { get; set; }

    void WritePattern(Action<Span<byte>> writeAction);
    void PlaySound();
    void StopSound();
    void Reset();
}
