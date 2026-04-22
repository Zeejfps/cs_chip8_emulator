using Chip8Emulator.Core;

namespace Chip8Emulator.App;

public sealed class ConsoleBeepAudio : IAudio
{
    public void PlaySound() => Console.Write('\a');
    public void StopSound() { }
}
