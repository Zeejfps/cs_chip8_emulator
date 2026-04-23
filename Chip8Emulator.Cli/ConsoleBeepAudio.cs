using Chip8Emulator.Core;

namespace Chip8Emulator.Cli;

public sealed class ConsoleBeepAudio : IAudio
{
    public byte Pitch { get; set; }
    public void WritePattern(Action<Span<byte>> writeAction) { }
    public void PlaySound() => Console.Write('\a');
    public void StopSound() { }
    public void Reset() { }
}
