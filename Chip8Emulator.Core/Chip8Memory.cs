namespace Chip8Emulator.Core;

public sealed class Chip8Memory : IMemory
{
    public ReadOnlyMemory<byte> Memory => _buffer;
    
    private readonly byte[] _buffer = new byte[4096];

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
}