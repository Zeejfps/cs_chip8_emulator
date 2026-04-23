namespace Chip8Emulator.Core;

public sealed class EmulatedMemory : IMemory
{
    private readonly Memory<byte> _buffer;

    public EmulatedMemory(Func<int, Memory<byte>> alloc)
    {
        _buffer = alloc(4096);
    }
    
    public byte Read(int address)
    {
        return _buffer.Span[address];
    }

    public void Write(int address, byte value)
    {
        _buffer.Span[address] = value;
    }

    public void Write(int address, ReadOnlySpan<byte> value)
    {
        value.CopyTo(_buffer.Span[address..]);
    }

    public void Clear()
    {
        _buffer.Span.Clear();
    }
}