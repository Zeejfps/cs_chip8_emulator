using Chip8Emulator.Core;

namespace Chip8Emulator.App;

public sealed class NoOpInput : IInput
{
    public bool IsKeyPressed(byte key) => false;
}
