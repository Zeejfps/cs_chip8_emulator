namespace Chip8Emulator.Core;

public interface IDisplay
{
    void Draw(ReadOnlySpan<byte> pixels);
}