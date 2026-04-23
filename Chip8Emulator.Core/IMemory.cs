namespace Chip8Emulator.Core;

public interface IMemory
{
    byte Read(int address);
    void Write(int address, byte value);
    void Write(int address, ReadOnlySpan<byte> value);
    void Clear();
    ReadOnlySpan<byte> AsReadOnlySpan();
}