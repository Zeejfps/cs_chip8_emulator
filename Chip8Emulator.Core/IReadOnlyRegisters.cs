namespace Chip8Emulator.Core;

public interface IReadOnlyRegisters
{
    ReadOnlyMemory<byte> VRegisters { get; }
    byte ReadV(int register);
    int ReadI();
    int ReadIWithOffset(int offset);
    byte ReadDt();
    byte ReadSt();
    int ReadPc();
}
