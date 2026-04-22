using Chip8Emulator.Core;

namespace Chip8Emulator.App;

public sealed class NoOpInput : IInput
{
    public bool IsKeyPressed(byte key) => false;

    public bool WasAnyKeyPressedAndReleased(out byte key)
    {
        key = 0;
        return false;
    }
}
