namespace Chip8Emulator.Core;

public interface IRenderer
{
    void Render(ReadOnlySpan<byte> pixels);
}