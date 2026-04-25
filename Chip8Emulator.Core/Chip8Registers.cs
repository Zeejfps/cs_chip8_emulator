namespace Chip8Emulator.Core;

internal sealed class Chip8Registers : IRegisters
{
    public ReadOnlyMemory<byte> VRegisters => _vRegisters;

    private byte _delayTimer;
    private byte _soundTimer;
    private int _indexRegister;
    private int _programCounter;
    private readonly byte[] _vRegisters = new byte[16];

    public byte ReadV(int register)
    {
        return _vRegisters[register];
    }

    public void WriteV(int register, byte value)
    {
        _vRegisters[register] = value;
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

    public int ReadPc()
    {
        return _programCounter;
    }

    public void WritePc(int value)
    {
        _programCounter = value;
    }

    public void Clear()
    {
        _delayTimer = 0;
        _soundTimer = 0;
        _indexRegister = 0;
        _programCounter = 0;
        Array.Clear(_vRegisters);
    }
}
