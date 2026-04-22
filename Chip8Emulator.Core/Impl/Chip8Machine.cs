using System.Runtime.CompilerServices;
using static Chip8Emulator.Core.Chip8Disassembler;

namespace Chip8Emulator.Core.Impl;

internal sealed class Chip8Machine : IChip8Machine
{
    private const int LowResFontBaseAddress = 0x050;
    private const int HighResFontBaseAddress = 0x0A0;

    private const int LowRestFontCharWidth = 5;
    private const int HighRestFontCharWidth = 10;
    
    private static ReadOnlySpan<byte> LowResFont =>             
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
        0xF0, 0x80, 0xF0, 0x80, 0x80, // F              
    ];

    private static ReadOnlySpan<byte> HighResFont => [
        // 0
        0x3C, 0x7E, 0xE7, 0xC3, 0xC3, 0xC3, 0xC3, 0xE7, 0x7E, 0x3C,
        // 1
        0x18, 0x38, 0x58, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0x3C,
        // 2
        0x3E, 0x7F, 0xC3, 0x06, 0x0C, 0x18, 0x30, 0x60, 0xFF, 0xFF,
        // 3
        0x3C, 0x7E, 0xC3, 0x03, 0x0E, 0x0E, 0x03, 0xC3, 0x7E, 0x3C,
        // 4
        0x06, 0x0E, 0x1E, 0x36, 0x66, 0xC6, 0xFF, 0xFF, 0x06, 0x06,
        // 5
        0xFF, 0xFF, 0xC0, 0xC0, 0xFC, 0xFE, 0x03, 0xC3, 0x7E, 0x3C,
        // 6
        0x3E, 0x7C, 0xC0, 0xC0, 0xFC, 0xFE, 0xC3, 0xC3, 0x7E, 0x3C,
        // 7
        0xFF, 0xFF, 0x03, 0x06, 0x0C, 0x18, 0x30, 0x60, 0x60, 0x60,
        // 8
        0x3C, 0x7E, 0xC3, 0xC3, 0x7E, 0x7E, 0xC3, 0xC3, 0x7E, 0x3C,
        // 9
        0x3C, 0x7E, 0xC3, 0xC3, 0x7F, 0x3F, 0x03, 0x03, 0x3E, 0x7C
    ];
    
    private const int InstructionSizeInBytes = 2;

    private int _instructionsPerSecond = 1000;
    
    private readonly IRenderer _renderer;
    private readonly IAudio _audio;
    private readonly IClock _clock;
    private readonly IInput _input;
    
    private readonly Display _display = new();
    
    private readonly byte[] _memory = new byte[4096];
    private readonly byte[] _vRegisters = new byte[16];

    private readonly int[] _stack = new int[16];
    private int _stackPointer = -1;

    private byte _delayTimer;
    private byte _soundTimer;
    private int _programCounter;
    private int _indexRegister;

    private readonly long _ticksPerFrame;
    private long _ticksPerInstruction;
    private long _lastTimestamp;
    private long _instructionAcc;
    private long _frameAcc;
    private bool _isWaitingForKey;
    private bool _waitForVBlank;
    private int _keyRegisterIndex;
    private bool _running;
    
    public Chip8Machine(IRenderer renderer, IAudio audio, IClock clock, IInput input)
    {
        _renderer = renderer;
        _audio = audio;
        _clock = clock;
        _input = input;
        _ticksPerFrame = clock.Frequency / 60;
        _ticksPerInstruction = clock.Frequency / _instructionsPerSecond;
        _lastTimestamp = clock.Timestamp;
        LowResFont.CopyTo(_memory.AsSpan(LowResFontBaseAddress));
        HighResFont.CopyTo(_memory.AsSpan(HighResFontBaseAddress));
        Debugger = new Chip8MachineDebugger(this);
    }

    public IMachineDebugger Debugger { get; }

    public int InstructionsPerSecond
    {
        get => _instructionsPerSecond;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
            _instructionsPerSecond = value;
            _ticksPerInstruction = _clock.Frequency / value;
            _instructionAcc = 0;
        }
    }

    public bool ShiftUsesVy { get; set; } = false;
    public bool JumpUsesVx { get; set; } = true;
    public bool LoadStoreIncrementsI { get; set; } = false;
    public bool LogicResetsVf { get; set; } = false;
    public bool SpritesWrap { get; set; } = false;
    public bool DisplayWait { get; set; } = false;
    public bool VfResultWrittenLast { get; set; } = false;

    private void PushStack(int value)
    {
        var nextStackPointer = _stackPointer + 1;
        if(nextStackPointer >= _stack.Length)
            throw new InvalidOperationException("Stack overflow");
        _stackPointer = nextStackPointer;
        _stack[_stackPointer] = value;
    }

    private int PopStack()
    {
        if (_stackPointer < 0)
            throw new InvalidOperationException("Stack underflow");

        var value = _stack[_stackPointer];
        _stackPointer--;
        return value;
    }

    public IDisplay Display => _display;
    
    internal void WriteMemory(int address, ReadOnlySpan<byte> data)
    {
        data.CopyTo(_memory.AsSpan(address));
    }
    
    public void LoadProgram(ReadOnlySpan<byte> program)
    {
        ResetMemory();
        ResetClock();
        ResetTimers();
        ResetDisplay();
        ResetRegisters();
        ResetStack();
        _indexRegister = 0;
        _isWaitingForKey = false;
        _waitForVBlank = false;
        _keyRegisterIndex = 0;
        program.CopyTo(_memory.AsSpan(0x200));
        _programCounter = 0x200;

        // Classic CHIP-8 HIRES signature: programs starting with `1260` (JP 0x260)
        // switch the display to a 64x64 canvas. See Hans Christian Egeberg / David Winter.
        if (program.Length >= 2 && program[0] == 0x12 && program[1] == 0x60)
        {
            _display.EnableClassicHiresMode();
        }
    }

    private void ResetClock()
    {
        _instructionAcc = 0;
        _frameAcc = 0;
        _lastTimestamp = _clock.Timestamp;
    }
    
    private void ResetTimers()
    {
        _delayTimer = 0;
        if (_soundTimer > 0)
        {
            _soundTimer = 0;
            _audio.StopSound();
        }
    }
    
    private void ResetMemory()
    {
        Array.Clear(_memory);
        LowResFont.CopyTo(_memory.AsSpan(LowResFontBaseAddress));
        HighResFont.CopyTo(_memory.AsSpan(HighResFontBaseAddress));
    }

    private void ResetDisplay()
    {
        _display.Reset();   
    }

    private void ResetRegisters()
    {
        Array.Clear(_vRegisters);
    }

    private void ResetStack()
    {
        Array.Clear(_stack);
        _stackPointer = -1;
    }

    public void Start()
    {
        if (_running) throw new InvalidOperationException("Machine is already started.");
        _lastTimestamp = _clock.Timestamp;
        _clock.Ticked += OnTicked;
        _running = true;
        if (_soundTimer > 0)
        {
            _audio.PlaySound();
        }
    }

    public void Stop()
    {
        if (!_running) return;
        _clock.Ticked -= OnTicked;
        _running = false;
        if (_soundTimer > 0)
        {
            _audio.StopSound();
        }
    }

    private void OnTicked(object? sender, EventArgs e)
    {
        var now = _clock.Timestamp;
        var delta = now - _lastTimestamp;
        _lastTimestamp = now;
        if (delta == 0)
        {
            return;
        }

        var maxDelta = _ticksPerFrame * 2;
        if (delta > maxDelta)
        {
            delta = maxDelta;
        }

        if (_isWaitingForKey)
        {
            if (_input.WasAnyKeyPressedAndReleased(out var key))
            {
                _vRegisters[_keyRegisterIndex] = key;
                _isWaitingForKey = false;
            }
        }

        _frameAcc += delta;

        if (_waitForVBlank || _isWaitingForKey)                            
        {                                                                  
            _instructionAcc = 0;                                           
        }                                                               
        else
        {
            _instructionAcc += delta;
            while (_instructionAcc >= _ticksPerInstruction)
            {                                                              
                FetchDecodeExecute();
                _instructionAcc -= _ticksPerInstruction;                   
                if (_waitForVBlank || _isWaitingForKey)                 
                {                                                          
                    _instructionAcc = 0;
                    break;                                                 
                }                                                       
            }
        }

        while (_frameAcc >= _ticksPerFrame)
        {
            StepFrame();
        }
    }

    private void StepFrame()
    {
        if (_delayTimer > 0)
        {
            _delayTimer--;
        }

        if (_soundTimer > 0)
        {
            _soundTimer--;
            if (_soundTimer == 0)
            {
                _audio.StopSound();
            }
        }

        _renderer.Render();
        _frameAcc -= _ticksPerFrame;
        _waitForVBlank = false;
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
                ExecuteZeroBaseIns(ins);
                break;
            case 1:
                ExecuteJumpToAddressIns(ins);
                break;
            case 2:
                ExecuteCallSubroutineIns(ins);
                break;
            case 3:
                ExecuteSkipNextInsIfRegisterValueEqualsValueIns(ins);
                break;
            case 4:
                ExecuteSkipNextInsIfRegisterValueNotEqualsValueIns(ins);
                break;
            case 5:
                ExecuteSkipNextInsIfRegisterValueEqualsRegisterValue(ins);
                break;
            case 6:
                ExecuteSetRegisterValueIns(ins);
                break;
            case 7:
                ExecuteAddValueToRegisterIns(ins);
                break;
            case 8:
                ExecuteArithmeticOperationIns(ins);
                break;
            case 9:
                ExecuteSkipNextInsIfRegisterValueNotEqualsRegisterValue(ins);
                break;
            case 0xA:
                ExecuteSetIndexRegisterIns(ins);
                break;
            case 0xB:
                ExecuteJumpWithOffsetIns(ins);
                break;
            case 0xC:
                ExecuteGenerateRandomNumIns(ins);
                break;
            case 0xD:
                ExeuteDrawToScreenIns(ins);
                break;
            case 0xE:
                ExecuteSkipNextInsIfKeyIsPressedOrReleased(ins);
                break;
            case 0xF:
                ExecuteTimerIns(ins);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(ins), ins, null);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void ExecuteZeroBaseIns(int ins)
    {
        if ((ins & 0xFF00) != 0x0000) return;

        var lo = ins & 0x00FF;

        switch (lo)
        {
            case 0xE0: ExecuteClearDisplayIns();     return;
            case 0xEE: ExecuteReturnFromSubroutineIns(); return;
            case 0xFF: ExecuteEnableHiresModeIns();  return;
            case 0xFE: ExecuteDisableHiresModeIns(); return;
            case 0xFB: _display.ScrollRight(4);      return;
            case 0xFC: _display.ScrollLeft(4);       return;
        }

        // 00CN — S-CHIP: scroll display down N rows.
        if ((lo & 0xF0) == 0xC0)
        {
            _display.ScrollDown(lo & 0x0F);
            return;
        }

        // 00DN — XO-CHIP: scroll display up N rows.
        if ((lo & 0xF0) == 0xD0)
        {
            _display.ScrollUp(lo & 0x0F);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private void ExecuteEnableHiresModeIns()
    {
        _display.EnableHighResMode();
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private void ExecuteDisableHiresModeIns()
    {
        _display.DisableHighResMode();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void ExecuteTimerIns(int ins)
    {
        var op = ins & 0x00FF;
        switch (op)
        {
            case 0x07:
                ExecuteReadDelayTimer(ins);
                break;
            case 0x0A:
                ExecuteWaitForKeyPress(ins);
                break;
            case 0x15:
                ExecuteSetDelayTimer(ins);
                break;
            case 0x18:
                ExecuteSetSoundTimer(ins);
                break;
            case 0x1E:
                ExecuteAddVxToI(ins);
                break;
            case 0x29:
                ExecuteLoadLowResFontCharacter(ins);
                break;
            case 0x30:
                ExecuteLoadHighResFontCharacter(ins);
                break;
            case 0x33:
                ExecuteStoreBcdInMemory(ins);
                break;
            case 0x55:
                ExecuteStoreRegisters(ins);
                break;
            case 0x65:
                ExecuteLoadRegisters(ins);
                break;
            // Unknown FXnn: no-op so extended-variant ROMs don't halt.
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void ExecuteStoreBcdInMemory(int ins)
    {
        var x = ExtractX(ins);
        var bcd = _vRegisters[x];
        _memory[_indexRegister & 0xFFF] = (byte)(bcd / 100);
        _memory[(_indexRegister + 1) & 0xFFF] = (byte)((bcd / 10) % 10);
        _memory[(_indexRegister + 2) & 0xFFF] = (byte)(bcd % 10);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void ExecuteLoadRegisters(int ins)
    {
        var x = ExtractX(ins);
        var index = _indexRegister;
        for (var i = 0; i <= x; i++, index++)
        {
            _vRegisters[i] = _memory[index & 0xFFF];
        }

        if (LoadStoreIncrementsI)
        {
            _indexRegister = index & 0xFFF;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void ExecuteStoreRegisters(int ins)
    {
        var x = ExtractX(ins);
        var index = _indexRegister;
        for (var i = 0; i <= x; i++, index++)
        {
            _memory[index & 0xFFF] = _vRegisters[i];
        }

        if (LoadStoreIncrementsI)
        {
            _indexRegister = index & 0xFFF;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void ExecuteLoadLowResFontCharacter(int ins)
    {
        var x = ExtractX(ins);
        var value = _vRegisters[x];
        _indexRegister = (value & 0x0F) * LowRestFontCharWidth + LowResFontBaseAddress;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void ExecuteLoadHighResFontCharacter(int ins)
    {
        var x = ExtractX(ins);
        var value = _vRegisters[x];
        _indexRegister = (value & 0x0F) * HighRestFontCharWidth + HighResFontBaseAddress;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void ExecuteAddVxToI(int ins)
    {
        var x = ExtractX(ins);
        _indexRegister = (_indexRegister + _vRegisters[x]) & 0xFFF;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void ExecuteWaitForKeyPress(int ins)
    {
        _isWaitingForKey = true;
        var x = ExtractX(ins);
        _keyRegisterIndex = x;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void ExecuteSetSoundTimer(int ins)
    {
        var x = ExtractX(ins);
        var prevSoundTimer = _soundTimer;
        _soundTimer = _vRegisters[x];
        if (prevSoundTimer == 0 && _soundTimer != 0)
        {
            _audio.PlaySound();
        }
        else if (prevSoundTimer != 0 && _soundTimer == 0)
        {
            _audio.StopSound();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void ExecuteSetDelayTimer(int ins)
    {
        var x = ExtractX(ins);
        _delayTimer = _vRegisters[x];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void ExecuteReadDelayTimer(int ins)
    {
        var x = ExtractX(ins);
        _vRegisters[x] = _delayTimer;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void ExecuteSkipNextInsIfKeyIsPressedOrReleased(int ins)
    {
        var op = ins & 0x00FF;
        if (op == 0x9E)
        {
            ExecuteSkipNextInsIfKeyIsPressed(ins);
        }
        else if (op == 0xA1)
        {
            ExecuteSkipNextInsIfKeyIsReleased(ins);
        }
        // Unknown EXnn: no-op.
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void ExecuteSkipNextInsIfKeyIsPressed(int ins)
    {
        var x = ExtractX(ins);
        var key = _vRegisters[x];
        if (_input.IsKeyPressed(key))
        {
            _programCounter += InstructionSizeInBytes;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void ExecuteSkipNextInsIfKeyIsReleased(int ins)
    {
        var x = ExtractX(ins);
        var key = _vRegisters[x];
        if (!_input.IsKeyPressed(key))
        {
            _programCounter += InstructionSizeInBytes;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void ExecuteGenerateRandomNumIns(int ins)
    {
        var x = ExtractX(ins);
        var nn = ExtractNn(ins);
        var randNum = (byte)Random.Shared.Next(0, 256);
        _vRegisters[x] = (byte)(randNum & nn);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void ExecuteSkipNextInsIfRegisterValueNotEqualsRegisterValue(int ins)
    {
        var x = ExtractX(ins);
        var y = ExtractY(ins);
        if (_vRegisters[x] != _vRegisters[y])
        {
            _programCounter += InstructionSizeInBytes;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void ExecuteSkipNextInsIfRegisterValueEqualsValueIns(int ins)
    {
        var x = ExtractX(ins);
        var nn = ExtractNn(ins);
        if (_vRegisters[x] == nn)
        {
            _programCounter += InstructionSizeInBytes;
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void ExecuteSkipNextInsIfRegisterValueNotEqualsValueIns(int ins)
    {
        var x = ExtractX(ins);
        var nn = ExtractNn(ins);
        if (_vRegisters[x] != nn)
        {
            _programCounter += InstructionSizeInBytes;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void ExecuteSkipNextInsIfRegisterValueEqualsRegisterValue(int ins)
    {
        var x = ExtractX(ins);
        var y = ExtractY(ins);
        if (_vRegisters[x] == _vRegisters[y])
        {
            _programCounter += InstructionSizeInBytes;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void ExecuteCallSubroutineIns(int ins)
    {
        var address = ExtractNnn(ins);
        PushStack(_programCounter);
        _programCounter = address;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void ExeuteDrawToScreenIns(int ins)
    {
        //Console.WriteLine($"Draw to screen");
        var x = _vRegisters[ExtractX(ins)] % Display.Width;
        var y = _vRegisters[ExtractY(ins)] % Display.Height;
        var n = ExtractN(ins);

        if (n == 0)
        {
            if (_display.IsHighRes)
                DrawHighResSprite(x, y);
            else
                DrawLowResSprite(x, y, 8);
        }
        else
        {
            DrawLowResSprite(x, y, n);
        }

        if (DisplayWait)
        {
            _waitForVBlank = true;
        }
    }

    private void DrawHighResSprite(int x, int y)
    {
        // S-CHIP 1.1 DXY0 hi-res collision semantics:
        // VF = number of sprite rows with at least one collision
        //    + number of sprite rows clipped off the bottom edge (when not wrapping).
        var displayPixels = _display.Pixels.Span;
        var collidedRows = 0;
        var clippedRows = 0;
        for (var i = 0; i < 16; i++)
        {
            var dstY = y + i;
            if (SpritesWrap)
            {
                dstY %= Display.Height;
            }
            else if (dstY >= Display.Height)
            {
                clippedRows = 16 - i;
                break;
            }

            var offset = i * 2;
            var spritePixelsRow = (ushort)(_memory[(_indexRegister + offset) & 0xFFF] << 8 |
                                          _memory[(_indexRegister + offset + 1) & 0xFFF]);
            var rowCollided = false;
            for (var bit = 0; bit < 16; bit++)
            {
                var dstX = x + bit;
                if (SpritesWrap) dstX %= Display.Width;
                else if (dstX >= Display.Width) break;

                var spritePixel = (byte)((spritePixelsRow >> (15 - bit)) & 1);
                var dstIndex = dstY * Display.Width + dstX;
                var before = displayPixels[dstIndex];
                if ((before & spritePixel) != 0) rowCollided = true;
                displayPixels[dstIndex] = (byte)(before ^ spritePixel);
            }
            if (rowCollided) collidedRows++;
        }

        _vRegisters[0xF] = (byte)(collidedRows + clippedRows);
    }

    private void DrawLowResSprite(int sx, int sy, int height)
    {
        var displayPixels = _display.Pixels.Span;
        byte collision = 0;
        for (var y = 0; y < height; y++)
        {
            var dstY = sy + y;
            if (SpritesWrap) dstY %= Display.Height;
            else if (dstY >= Display.Height) break;
            
            var spritePixelsRow = _memory[(_indexRegister + y) & 0xFFF];
            for (var bit = 0; bit < 8; bit++)
            {
                var dstX = sx + bit;
                if (SpritesWrap) dstX %= Display.Width;
                else if (dstX >= Display.Width) break;

                var spritePixel = (byte)((spritePixelsRow >> (7 - bit)) & 1);
                var dstIndex = dstY * Display.Width + dstX;
                var before = displayPixels[dstIndex];
                collision |= (byte)(before & spritePixel);
                displayPixels[dstIndex] = (byte)(before ^ spritePixel);
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
    public void ExecuteSetRegisterValueIns(int ins)
    {
        var x = ExtractX(ins);
        var nn = ExtractNn(ins);
        _vRegisters[x] = nn;
        //Console.WriteLine($"Set Register Value: {x:X}, Value: {nn:X}");
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void ExecuteArithmeticOperationIns(int ins)
    {
        var lo = ins & 0x000F;
        switch (lo)
        {
            case 0:
                ExecuteSetRegisterValueFromRegisterIns(ins);
                break;
            case 1:
                ExecuteBitwiseOrOnRegistersIns(ins);
                break;
            case 2:
                ExecuteBitwiseAndOnRegistersIns(ins);
                break;
            case 3:
                ExecuteXorRegisterValueFromRegisterIns(ins);
                break;
            case 4:
                ExecuteAddValueToRegisterWithCarryIns(ins);
                break;
            case 5:
                ExecuteVxSubVyIns(ins);
                break;
            case 6:
                ExecuteShiftRightIns(ins);
                break;
            case 7:
                ExecuteVySubVxIns(ins);
                break;
            case 0xE:
                ExecuteShiftLeftIns(ins);
                break;
            // Unknown 8XYn: no-op.
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void ExecuteShiftRightIns(int ins)
    {
        var x = ExtractX(ins);
        var value = _vRegisters[x];
        
        if (ShiftUsesVy)
        {
            var y = ExtractY(ins);
            value = _vRegisters[y];
        }
        
        var flag = (byte)(value & 0x1);
        var result = (byte)(value >> 1);
        _vRegisters[x] = result;
        _vRegisters[0xF] = flag;
        if (VfResultWrittenLast) _vRegisters[x] = result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void ExecuteShiftLeftIns(int ins)
    {
        var x = ExtractX(ins);
        var value = _vRegisters[x];
        
        if (ShiftUsesVy)
        {
            var y = ExtractY(ins);
            value = _vRegisters[y];
        }
        
        var flag = (byte)((value >> 7) & 0x1);
        var result = (byte)(value << 1);
        _vRegisters[x] = result;
        _vRegisters[0xF] = flag;
        if (VfResultWrittenLast) _vRegisters[x] = result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void ExecuteVxSubVyIns(int ins)
    {
        var x = ExtractX(ins);
        var y = ExtractY(ins);
        var minuend = _vRegisters[x];
        var subtrahend = _vRegisters[y];
        var flag = (byte)(minuend >= subtrahend ? 1 : 0);
        var result = (byte)(minuend - subtrahend);
        _vRegisters[x] = result;
        _vRegisters[0xF] = flag;
        if (VfResultWrittenLast) _vRegisters[x] = result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void ExecuteVySubVxIns(int ins)
    {
        var x = ExtractX(ins);
        var y = ExtractY(ins);
        // NOTE(Zee): y first
        var minuend = _vRegisters[y];
        var subtrahend = _vRegisters[x];
        var flag = (byte)(minuend >= subtrahend ? 1 : 0);
        var result = (byte)(minuend - subtrahend);
        _vRegisters[x] = result;
        _vRegisters[0xF] = flag;
        if (VfResultWrittenLast) _vRegisters[x] = result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void ExecuteAddValueToRegisterWithCarryIns(int ins)
    {
        var x = ExtractX(ins);
        var y = ExtractY(ins);
        var sum = _vRegisters[x] + _vRegisters[y];
        var carry = (byte)(sum > 0xFF ? 1 : 0);
        var result = (byte)sum;
        _vRegisters[x] = result;
        _vRegisters[0xF] = carry;
        if (VfResultWrittenLast) _vRegisters[x] = result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void ExecuteBitwiseOrOnRegistersIns(int ins)
    {
        var x = ExtractX(ins);
        var y = ExtractY(ins);
        _vRegisters[x] |= _vRegisters[y];

        if (LogicResetsVf)
        {
            _vRegisters[0xF] = 0;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void ExecuteBitwiseAndOnRegistersIns(int ins)
    {
        var x = ExtractX(ins);
        var y = ExtractY(ins);
        _vRegisters[x] &= _vRegisters[y]; 
        
        if (LogicResetsVf)
        {
            _vRegisters[0xF] = 0;
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void ExecuteSetRegisterValueFromRegisterIns(int ins)
    {
        var x = ExtractX(ins);
        var y = ExtractY(ins);
        _vRegisters[x] = _vRegisters[y];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void ExecuteXorRegisterValueFromRegisterIns(int ins)
    {
        var x = ExtractX(ins);
        var y = ExtractY(ins);
        _vRegisters[x] ^= _vRegisters[y];  
        
        if (LogicResetsVf)
        {
            _vRegisters[0xF] = 0;
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void ExecuteAddValueToRegisterIns(int ins)
    {
        var x = ExtractX(ins);
        var nn = ExtractNn(ins);
        //Console.WriteLine($"Add Value To Register: {x:X}, Value: {nn:X}");
        _vRegisters[x] += nn;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void ExecuteSetIndexRegisterIns(int ins)
    {
        var nnn = ExtractNnn(ins);
        //Console.WriteLine($"Set Index Register: {nnn:X}");
        _indexRegister = nnn;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void ExecuteJumpToAddressIns(int ins)
    {
        var address = ExtractNnn(ins);
        _programCounter = address;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void ExecuteJumpWithOffsetIns(int ins)
    {
        var address = ExtractNnn(ins);
        if (JumpUsesVx)
        {
            var x = ExtractX(ins);
            _programCounter = address + _vRegisters[x];
        }
        else
        {
            _programCounter = address + _vRegisters[0];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void ExecuteReturnFromSubroutineIns()
    {
        var address = PopStack();
        _programCounter = address;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void ExecuteClearDisplayIns()
    {
        _display.Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private int Fetch()
    {
        var ins = _memory[_programCounter] << 8 | _memory[_programCounter+1];
        _programCounter += InstructionSizeInBytes;
        return ins;
    }

    private sealed class Chip8MachineDebugger(Chip8Machine machine) : IMachineDebugger
    {
        public ReadOnlySpan<byte> Memory => machine._memory;
        public ReadOnlySpan<byte> Registers => machine._vRegisters;
        public ReadOnlySpan<int> Stack => machine._stack;
        public int ProgramCounter => machine._programCounter;
        public int IndexRegister => machine._indexRegister;
        public int StackPointer => machine._stackPointer;
        public byte DelayTimer => machine._delayTimer;
        public byte SoundTimer => machine._soundTimer;
        public bool IsWaitingForKey => machine._isWaitingForKey;
        public bool IsWaitingForVBlank => machine._waitForVBlank;

        public void StepInstruction()
        {
            machine.FetchDecodeExecute();
        }
    }
}