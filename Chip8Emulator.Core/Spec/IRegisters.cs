namespace Chip8Emulator.Core.Spec;

internal interface IRegisters : IReadOnlyRegisters
{
    void WriteV(int register, byte value);
    void WriteI(int value);
    void WriteDt(byte value);
    void WriteSt(byte value);
    void WritePc(int value);
    void Clear();
}
