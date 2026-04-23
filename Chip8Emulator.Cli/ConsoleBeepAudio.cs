using Chip8Emulator.Core;

namespace Chip8Emulator.Cli;

public sealed class ConsoleBeepAudio : IAudio
{
    public byte Pitch { get; set; }
    public bool IsPlaying { get; private set; }
    public void WritePattern(Action<Span<byte>> writeAction) { }
    public void PlaySound() { Console.Write('\a'); IsPlaying = true; }
    public void StopSound() => IsPlaying = false;
    public void Reset() => IsPlaying = false;
}
