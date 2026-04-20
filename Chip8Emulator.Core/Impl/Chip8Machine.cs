using System.Runtime.CompilerServices;

namespace Chip8Emulator.Core.Impl;

internal sealed class Chip8Machine : IChip8Machine
{
    private const int FontBaseAddress = 0x050;

    private static ReadOnlySpan<byte> Font =>             
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
    
    private const int ScreenWidth = 64;
    private const int ScreenHeight = 32;
    private const int InstructionsPerSecond = 1000;
    private const int InstructionsPerFrame = InstructionsPerSecond / 60;
    private const int InstructionSizeInBytes = 2;
    
    private readonly IRenderer _renderer;
    private readonly IAudio _audio;
    private readonly IClock _clock;
    private readonly IInput _input;
    
    private readonly byte[] _memory = new byte[4096];
    private readonly byte[] _vRegisters = new byte[16];

    private readonly byte[] _displayPixels = new byte[ScreenWidth * ScreenHeight];
    private readonly int[] _stack = new int[16];

    private byte _delayTimer;
    private byte _soundTimer;
    private int _programCounter;
    private int _indexRegister;
    private int _stackPointer;

    private readonly long _ticksPerFrame;
    private long _lastTimestamp;
    private long _accumulatedTicks;
    private int _instructionsExecuted;
    private bool _isWaitingForKeyPress;
    private int _keyRegisterIndex;

    public Chip8Machine(IRenderer renderer, IAudio audio, IClock clock, IInput input)
    {
        _renderer = renderer;
        _audio = audio;
        _clock = clock;
        _input = input;
        _ticksPerFrame = clock.Frequency / 60;
        _lastTimestamp = clock.Timestamp;
        Font.CopyTo(_memory.AsSpan(FontBaseAddress));
    }
    
    public int ProgramCounter => _programCounter;
    public int IndexRegister => _indexRegister;
    public int StackPointer => _stackPointer;
    public byte DelayTimer => _delayTimer;
    public byte SoundTimer => _soundTimer;
    public bool IsWaitingForKeyPress => _isWaitingForKeyPress;
    public ReadOnlySpan<byte> Memory => _memory;
    
    public byte ReadRegister(int x)
    {
        return _vRegisters[x];
    }

    public int PeekStack()
    {
        return _stack[_stackPointer];
    }

    public void PushStack(int value)
    {
        _stackPointer++;
        if(_stackPointer >= _stack.Length)
            throw new InvalidOperationException("Stack overflow");
        _stack[_stackPointer] = value;
    }

    public int PopStack()
    {
        var stackPointer = _stackPointer;
        _stackPointer--;
        if (_stackPointer < 0)
            throw new InvalidOperationException("Stack underflow");
        return _stack[stackPointer];   
    }

    public Memory<byte> DisplayPixels => _displayPixels;
    public int DisplayWidth => ScreenWidth;
    public int DisplayHeight => ScreenHeight;

    internal void WriteMemory(int address, ReadOnlySpan<byte> data)
    {
        data.CopyTo(_memory.AsSpan(address));
    }
    
    public void LoadProgram(ReadOnlySpan<byte> program)
    {
        Array.Clear(_memory);
        Font.CopyTo(_memory.AsSpan(FontBaseAddress));
        program.CopyTo(_memory.AsSpan(0x200));
        _programCounter = 0x200;
        _indexRegister = 0;
        _stackPointer = 0;
        _delayTimer = 0;
        _soundTimer = 0;
        _instructionsExecuted = 0;
        _isWaitingForKeyPress = false;
        _keyRegisterIndex = 0;
        _accumulatedTicks = 0;
        _lastTimestamp = _clock.Timestamp;
        Array.Clear(_vRegisters);
        Array.Clear(_stack);
        Array.Clear(_displayPixels);
    }

    public void Update()
    {
        var now = _clock.Timestamp;
        var delta = now - _lastTimestamp;
        _lastTimestamp = now;
        if (delta == 0)
        {
            return;
        }

        if (_isWaitingForKeyPress)
        {
            if (_input.WasAnyKeyPressed(out var key))
            {
                _vRegisters[_keyRegisterIndex] = key;
                _isWaitingForKeyPress = false;
            }
        }

        if (!_isWaitingForKeyPress && _instructionsExecuted < InstructionsPerFrame)
        {
            FetchDecodeExecute();
        }

        _accumulatedTicks += delta;
        while (_accumulatedTicks >= _ticksPerFrame)
        {
            while (!_isWaitingForKeyPress && _instructionsExecuted < InstructionsPerFrame)
            {
                FetchDecodeExecute();
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

            _renderer.Render();
            _accumulatedTicks -= _ticksPerFrame;
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
                    ExecuteClearDisplayIns();
                else if (ins == 0x00EE)
                    ExecuteReturnFromSubroutineIns();
                else
                    throw new ArgumentOutOfRangeException(nameof(ins), ins, null);
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
        
        _instructionsExecuted++;
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
                ExecuteLoadFontCharacter(ins);
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
            default:
                throw new ArgumentOutOfRangeException(nameof(ins), ins, null);
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void ExecuteStoreBcdInMemory(int ins)
    {
        var x = ExtractX(ins);
        var bcd = _vRegisters[x];
        _memory[_indexRegister] = (byte)(bcd / 100);
        _memory[_indexRegister + 1] = (byte)((bcd / 10) % 10);
        _memory[_indexRegister + 2] = (byte)(bcd % 10);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void ExecuteLoadRegisters(int ins)
    {
        var x = ExtractX(ins);
        for (var i = 0; i <= x; i++)
        {
            _vRegisters[i] = _memory[_indexRegister + i];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void ExecuteStoreRegisters(int ins)
    {
        var x = ExtractX(ins);
        for (var i = 0; i <= x; i++)
        {
            _memory[_indexRegister + i] = _vRegisters[i];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void ExecuteLoadFontCharacter(int ins)
    {
        var x = ExtractX(ins);
        var value = _vRegisters[x];
        _indexRegister = value * 5 + FontBaseAddress;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void ExecuteAddVxToI(int ins)
    {
        var x = ExtractX(ins);
        _indexRegister += _vRegisters[x];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void ExecuteWaitForKeyPress(int ins)
    {
        _isWaitingForKeyPress = true;
        var x = ExtractX(ins);
        _keyRegisterIndex = x;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void ExecuteSetSoundTimer(int ins)
    {
        var x = ExtractX(ins);
        _soundTimer = _vRegisters[x];
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
        else
        {
            throw new ArgumentOutOfRangeException(nameof(ins), ins, null);
        }
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
        var op = ins & 0x000F;
        switch (op)
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
            default:
                throw new ArgumentOutOfRangeException(nameof(ins), ins, null);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void ExecuteShiftRightIns(int ins)
    {
        var x = ExtractX(ins);
        var value = _vRegisters[x];
        var flag = (byte)(value & 0x1);
        _vRegisters[x] = (byte)(value >> 1);
        _vRegisters[0xF] = flag;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void ExecuteShiftLeftIns(int ins)
    {
        var x = ExtractX(ins);
        var value = _vRegisters[x];
        var flag = (byte)((value >> 7) & 0x1);
        _vRegisters[x] = (byte)(value << 1);
        _vRegisters[0xF] = flag;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void ExecuteVxSubVyIns(int ins)
    {
        var x = ExtractX(ins);
        var y = ExtractY(ins);
        var minuend = _vRegisters[x];
        var subtrahend = _vRegisters[y];
        var flag = (byte)(minuend >= subtrahend ? 1 : 0);
        _vRegisters[x] = (byte)(minuend - subtrahend);
        _vRegisters[0xF] = flag;
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
        _vRegisters[x] = (byte)(minuend - subtrahend);
        _vRegisters[0xF] = flag;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void ExecuteAddValueToRegisterWithCarryIns(int ins)
    {
        var x = ExtractX(ins);
        var y = ExtractY(ins);
        var sum = _vRegisters[x] + _vRegisters[y];
        var carry = (byte)(sum > 0xFF ? 1 : 0);
        _vRegisters[x] = (byte)sum;
        _vRegisters[0xF] = carry;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void ExecuteBitwiseOrOnRegistersIns(int ins)
    {
        var x = ExtractX(ins);
        var y = ExtractY(ins);
        _vRegisters[x] |= _vRegisters[y];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void ExecuteBitwiseAndOnRegistersIns(int ins)
    {
        var x = ExtractX(ins);
        var y = ExtractY(ins);
        _vRegisters[x] &= _vRegisters[y]; 
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
    public void ExecuteJumpToAddressIns(int ins)
    {
        var address = ExtractNnn(ins);
        //Console.WriteLine($"Jumping to address: {address:X}");
        _programCounter = address;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void ExecuteJumpWithOffsetIns(int ins)
    {
        var address = ExtractNnn(ins);
        _programCounter = address + _vRegisters[0];
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
        Array.Clear(_displayPixels);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public int Fetch()
    {
        var ins = _memory[_programCounter] << 8 | _memory[_programCounter+1];
        _programCounter += InstructionSizeInBytes;
        return ins;
    }
}