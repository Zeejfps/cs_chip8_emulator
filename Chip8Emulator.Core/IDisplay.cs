namespace Chip8Emulator.Core;

public interface IDisplay
{
    Memory<byte> Pixels { get; }
    int Width { get; }
    int Height { get; }
    bool IsHighRes { get; }
}
