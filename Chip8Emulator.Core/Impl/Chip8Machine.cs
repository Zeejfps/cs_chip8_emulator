using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Chip8Emulator.Core.Impl;

internal sealed class Chip8Machine : IChip8Machine
{
    private const int ScreenWidth = 64;
    private const int ScreenHeight = 32;
    private const double FrameTimeInSeconds = 1.0 / 60.0;
    private const int InstructionsPerSecond = 700;
    private const int InstructionsPerFrame = InstructionsPerSecond / 60;
    
    private readonly IDisplay _display;
    private readonly IAudio _audio;
    private readonly IClock _clock;
    
    private readonly byte[] _memory = new byte[4096];
    private readonly byte[] _vRegisters = new byte[16];
    private readonly byte[] _font =
    [
        0xF0, 0x90, 0x90, 0x90, 0xF0, // 0
        0x20, 0x60, 0x20, 0x20, 0x70, // 1
        0xF0, 0x10, 0xF0, 0x80, 0xF0, // 2
        0xF0, 0x10, 0xF0, 0x10, 0xF0, // 3
        0x90, 0x90, 0xF0, 0x10, 0x10, // 4
        0xF0, 0x80, 0xF0, 0x10, 0xF0, // 5
        0xF0, 0x80, 0xF0, 0x90, 0xF0, // 6
        0xF0, 0x10, 0x20, 0x40, 0x40, // 7
        0xF0, 0x90, 0xF0, 0x90, 0xF0, // 8
        0xF0, 0x90, 0xF0, 0x10, 0xF0, // 9
        0xF0, 0x90, 0xF0, 0x90, 0x90, // A
        0xE0, 0x90, 0xE0, 0x90, 0xE0, // B
        0xF0, 0x80, 0x80, 0x80, 0xF0, // C
        0xE0, 0x90, 0x90, 0x90, 0xE0, // D
        0xF0, 0x80, 0xF0, 0x80, 0xF0, // E
        0xF0, 0x80, 0xF0, 0x80, 0x80  // F
    ];

    private readonly byte[] _displayPixels = new byte[ScreenWidth * ScreenHeight];
    
    private byte _delayTimer;
    private byte _soundTimer;
    private int _programCounter;
    private int _indexRegister;
    private Stack<ushort> _stack = new();
    
    private double _totalElapsedSeconds;
    private int _instructionsExecuted;
    
    public Chip8Machine(IDisplay display, IAudio audio, IClock clock)
    {
        _display = display;
        _audio = audio;
        _clock = clock;
    }
    
    public int ProgramCounter => _programCounter;
    public int IndexRegister => _indexRegister;
    public byte DelayTimer => _delayTimer;
    public byte SoundTimer => _soundTimer;
    public ReadOnlySpan<byte> Memory => _memory;
    public byte ReadRegister(int x)
    {
        return _vRegisters[x];
    }

    internal ReadOnlySpan<byte> DisplayPixels => _displayPixels;

    internal void WriteMemory(int address, ReadOnlySpan<byte> data)
    {
        data.CopyTo(_memory.AsSpan(address));
    }

    public void LoadProgram(ReadOnlySpan<byte> program)
    {
        Array.Clear(_memory);
        Console.WriteLine(program.Length);
        program.CopyTo(_memory.AsSpan(0x200));
    }

    public void Update()
    {
        var elapsedTimeInSeconds = _clock.GetElapsedTimeInSeconds();
        if (elapsedTimeInSeconds == 0)
        {
            return;
        }
        
        if (_instructionsExecuted < InstructionsPerFrame)
        {
            FetchDecodeExecute();
            _instructionsExecuted++;
        }
        
        _totalElapsedSeconds += elapsedTimeInSeconds;
        while (_totalElapsedSeconds >= FrameTimeInSeconds)
        {
            while (_instructionsExecuted < InstructionsPerFrame)
            {
                FetchDecodeExecute();
                _instructionsExecuted++;
            }
            
            if (_delayTimer > 0)
            {
                _delayTimer--;
            }
                
            if (_soundTimer > 0)
            {
                _audio.Beep();
                _soundTimer--;
            }
                
            _display.Draw(_displayPixels);
            _totalElapsedSeconds -= FrameTimeInSeconds;
            _instructionsExecuted = 0;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private void FetchDecodeExecute()
    {
        var ins = Fetch();
        //Console.WriteLine("Executing: {0:X4}", ins);
        var opcode = (ins & 0xF000) >> 12;
        //Console.WriteLine("Op Code: {0:X4}", opcode);
        switch (opcode)
        {
            case 0:
                if (ins == 0x00E0)
                    ClearDisplay();
                else if (ins == 0x00EE)
                    ReturnFromSubroutine();
                break;
            case 1:
                var address = ExtractNnn(ins);
                JumpToAddress(address);
                break;
            case 2:
                break;
            case 4:
                break;
            case 3:
                break;
            case 5:
                break;
            case 6:
                SetRegisterValue(ins);
                break;
            case 7:
                AddValueToRegister(ins);
                break;
            case 8:
                break;
            case 9:
                break;
            case 0xA:
                SetIndexRegister(ins);
                break;
            case 0xB:
                break;
            case 0xC:
                break;
            case 0xD:
                DrawToScreen(ins);
                break;
            case 0xE:
                break;
            case 0xF:
                break;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void DrawToScreen(int ins)
    {
        //Console.WriteLine($"Draw to screen");
        var x = _vRegisters[ExtractX(ins)] % ScreenWidth;
        var y = _vRegisters[ExtractY(ins)] % ScreenHeight;
        var spriteHeight = ExtractN(ins);

        byte collision = 0;
        for (var i = 0; i < spriteHeight; i++)
        {
            var dstY = y + i;
            if (dstY >= ScreenHeight) break;
            
            var spritePixelsRow = _memory[_indexRegister + i];
            for (var bit = 0; bit < 8; bit++)
            {
                var dstX = x + bit;
                if (dstX >= ScreenWidth) break;
                
                var spritePixel = (byte)((spritePixelsRow >> (7 - bit)) & 1);
                var dstIndex = dstY * ScreenWidth + dstX;
                var before = _displayPixels[dstIndex];
                collision |= (byte)(before & spritePixel);
                _displayPixels[dstIndex] = (byte)(before ^ spritePixel);
            }
        }

        var vf = collision != 0;
        if (vf)
        {
            _vRegisters[0xF] = 1;
        }
        else
        {
            _vRegisters[0xF] = 0;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void SetRegisterValue(int ins)
    {
        var x = ExtractX(ins);
        var nn = ExtractNn(ins);
        _vRegisters[x] = nn;
        //Console.WriteLine($"Set Register Value: {x:X}, Value: {nn:X}");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void AddValueToRegister(int ins)
    {
        var x = ExtractX(ins);
        var nn = ExtractNn(ins);
        Console.WriteLine($"Add Value To Register: {x:X}, Value: {nn:X}");
        _vRegisters[x] += nn;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void SetIndexRegister(int ins)
    {
        var nnn = ExtractNnn(ins);
        //Console.WriteLine($"Set Index Register: {nnn:X}");
        _indexRegister = nnn;   
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static int ExtractNnn(int ins)
    {
        return ins & 0x0FFF;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static byte ExtractNn(int ins)
    {
        return (byte)(ins & 0x00FF);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static int ExtractN(int ins)
    {
        return ins & 0x000F;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static int ExtractX(int ins)
    {
        return (ins & 0x0F00) >> 8;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static int ExtractY(int ins)
    {
        return (ins & 0x00F0) >> 4;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void JumpToAddress(int address)
    {
        //Console.WriteLine($"Jumping to address: {address:X}");
        _programCounter = address;
    }

    public void ReturnFromSubroutine()
    {
        throw new NotImplementedException();
    }

    public void ClearDisplay()
    {
        Array.Clear(_displayPixels);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public int Fetch()
    {
        var ins = _memory[_programCounter] << 8 | _memory[_programCounter+1];
        _programCounter += 2;
        return ins;
    }
}