namespace Chip8Emulator.Core;

public sealed class Chip8Registers : IRegisters
{
    private byte _delayTimer;
    private byte _soundTimer;
    private int _indexRegister;
    private readonly Memory<byte> _vRegisters;

    public Chip8Registers(Func<int, Memory<byte>> alloc)
    {
        const int requiredSize = 16;
        _vRegisters = alloc(requiredSize);
        if (_vRegisters.Length < requiredSize)
            throw new InvalidOperationException($"Allocator returned {_vRegisters.Length} bytes, expected at least {requiredSize}.");
    }
    
    public byte ReadV(int register)
    {
        return _vRegisters.Span[register];
    }

    public void WriteV(int register, byte value)
    {
        _vRegisters.Span[register] = value;
    }

    public int ReadI()
    {
        return _indexRegister;
    }

    public int ReadIWithOffset(int offset)
    {
        return (_indexRegister + offset) & 0xFFFF;
    }

    public void WriteI(int value)
    {
        _indexRegister = value & 0xFFFF;
    }

    public byte ReadDt()
    {
        return _delayTimer;
    }

    public void WriteDt(byte value)
    {
        _delayTimer = value;
    }

    public byte ReadSt()
    {
        return _soundTimer;
    }
    
    public void WriteSt(byte value)
    {
        _soundTimer = value;
    }

    public void Clear()
    {
        _delayTimer = 0;
        _soundTimer = 0;
        _indexRegister = 0;
        _vRegisters.Span.Clear();
    }
}