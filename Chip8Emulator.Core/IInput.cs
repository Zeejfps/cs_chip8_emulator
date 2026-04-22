namespace Chip8Emulator.Core;

public interface IInput
{
    bool IsKeyPressed(byte key);
    bool WasAnyKeyPressedAndReleased(out byte key);
}