namespace Chip8Emulator.Core;

public interface IRegisters
{
    byte ReadV(int register);
    void WriteV(int register, byte value);
    int ReadI();
    int ReadIWithOffset(int offset);
    void WriteI(int value);
    byte ReadDt();
    void WriteDt(byte value);
    void WriteSt(byte value);
    byte ReadSt();
    void Clear();
    
    ReadOnlySpan<byte> AsReadOnlySpan();
}