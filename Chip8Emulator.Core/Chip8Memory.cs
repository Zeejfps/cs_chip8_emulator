namespace Chip8Emulator.Core;

public sealed class Chip8Memory : IMemory
{
    private readonly Memory<byte> _buffer;

    public Chip8Memory(Func<int, Memory<byte>> alloc)
    {
        const int requiredSize = 4096;
        _buffer = alloc(requiredSize);
        if (_buffer.Length < requiredSize)
            throw new InvalidOperationException($"Allocator returned {_buffer.Length} bytes, expected at least {requiredSize}.");
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