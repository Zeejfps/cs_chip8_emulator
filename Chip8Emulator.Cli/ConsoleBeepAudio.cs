using Chip8Emulator.Core;

namespace Chip8Emulator.Cli;

public sealed class ConsoleBeepAudio : IAudio
{
    public void PlaySound() => Console.Write('\a');
    public void StopSound() { }
    public void SetPattern(ReadOnlySpan<byte> pattern, double frequencyHz) { }
}
