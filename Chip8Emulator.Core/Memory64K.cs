namespace Chip8Emulator.Core;

public sealed class Memory64K : IMemory
{
    private readonly byte[] _buffer = new byte[64 * 1024];
    
    public byte Read(int address)
    {
        return _buffer[address];
    }

    public void Write(int address, byte value)
    {
        _buffer[address] = value;
    }

    public void Write(int address, ReadOnlySpan<byte> value)
    {
        value.CopyTo(_buffer.AsSpan(address));
    }

    public void Clear()
    {
        Array.Clear(_buffer);
    }

    public ReadOnlySpan<byte> AsReadOnlySpan()
    {
        return _buffer;
    }
}