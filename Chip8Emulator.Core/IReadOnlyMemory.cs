namespace Chip8Emulator.Core;

public interface IReadOnlyMemory
{
    byte Read(int address);
}