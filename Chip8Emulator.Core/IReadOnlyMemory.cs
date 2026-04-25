namespace Chip8Emulator.Core;

public interface IReadOnlyMemory
{
    int Size { get; }
    byte Read(int address);
}