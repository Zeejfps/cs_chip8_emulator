namespace Chip8Emulator.Core.Spec;

internal interface IMemory : IReadOnlyMemory  
{
    void Write(int address, byte value);
    void Write(int address, ReadOnlySpan<byte> value);
    void Clear();
}