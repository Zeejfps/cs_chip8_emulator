namespace Chip8Emulator.Core;

public interface IAudio
{
    byte Pitch { get; set; }
    bool IsPlaying { get; }

    void WritePattern(Action<Span<byte>> writeAction);
    void PlaySound();
    void StopSound();
    void Reset();
}
