namespace Chip8Emulator.Core;

public interface IReadOnlyDisplay
{
    ReadOnlyMemory<byte> VMem { get; }
    int Width { get; }
    int Height { get; }
}