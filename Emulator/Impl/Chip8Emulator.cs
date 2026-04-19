using System.Diagnostics;
using System.Runtime.CompilerServices;
using Emulator.Api;

namespace Emulator.Impl;

internal sealed class Chip8Emulator : IChip8
{
    private const double FrameTimeInSeconds = 1.0 / 60.0;
    private const int InstructionsPerSecond = 700;
    
    private readonly IDisplay _display;
    private readonly IAudio _audio;
    
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

    private byte _delayTimer;
    private byte _soundTimer;
    private int _programCounter;
    private int _indexRegister;
    private Stack<ushort> _stack = new();
    
    private long _startTime;
    private double _totalElapsedSeconds;
    private int _instructionsExecuted;
    
    public Chip8Emulator(IDisplay display, IAudio audio)
    {
        _display = display;
        _audio = audio;
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

    public void Execute(ReadOnlySpan<byte> program)
    {
        _startTime = Stopwatch.GetTimestamp();
        _totalElapsedSeconds = 0;
        while (true)
        {
            Tick();
        }
    }

    public void Tick()
    {
        if (_instructionsExecuted < InstructionsPerSecond)
        {
            FetchDecodeExecute();
            _instructionsExecuted++;
        }
        
        var endTime = Stopwatch.GetTimestamp();
        var elapsed = endTime - _startTime;
        _totalElapsedSeconds += elapsed / (double) Stopwatch.Frequency ;
        _startTime = endTime;
        
        if (_totalElapsedSeconds >= FrameTimeInSeconds)
        {
            if (_delayTimer > 0)
            {
                _delayTimer--;
            }
                
            if (_soundTimer > 0)
            {
                _audio.Beep();
                _soundTimer--;
            }
                
            _display.Update();
            _totalElapsedSeconds = 0;
            _instructionsExecuted = 0;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private void FetchDecodeExecute()
    {
        var ins = Fetch();
        var opcode = ins & 0x00F0;
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
    private void DrawToScreen(int ins)
    {
        var x = _vRegisters[ExtractX(ins)];
        var y = _vRegisters[ExtractY(ins)];
        var n = ExtractN(ins);

        for (var i = 0; i < n; i++)
        {
            var row = _memory[_indexRegister + i];
            var result = _display.BlitRow(x, y, row);
            if (result == 0)
            {
                _vRegisters[0xF] = 1;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void SetRegisterValue(int ins)
    {
        var x = ExtractX(ins);
        var nn = ExtractNn(ins);
        _vRegisters[x] = nn;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void AddValueToRegister(int ins)
    {
        var x = ExtractX(ins);
        var nn = ExtractNn(ins);
        _vRegisters[x] += nn;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void SetIndexRegister(int ins)
    {
        var nnn = ExtractNnn(ins);
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
        _programCounter = address;
    }

    public void ReturnFromSubroutine()
    {
        throw new NotImplementedException();
    }

    public void ClearDisplay()
    {
        _display.Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public int Fetch()
    {
        return _memory[_programCounter] << 8 | _memory[_programCounter+1];
    }
}