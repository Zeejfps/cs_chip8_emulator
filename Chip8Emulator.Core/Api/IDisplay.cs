namespace Chip8Emulator.Core.Api;

public interface IDisplay
{
    void Draw(ReadOnlySpan<byte> pixels);
}