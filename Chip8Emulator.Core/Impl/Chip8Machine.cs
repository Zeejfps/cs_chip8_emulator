using System.Runtime.CompilerServices;

namespace Chip8Emulator.Core.Impl;

internal sealed class Chip8Machine : IChip8Machine
{
    public const int LowResFontBaseAddress = 0x050;
    public const int HighResFontBaseAddress = 0x0A0;

    public const int LowRestFontCharWidth = 5;
    public const int HighRestFontCharWidth = 10;

    public const int InstructionSizeInBytes = 2;

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

    private int _instructionsPerSecond = 1000;

    private readonly IRenderer _renderer;
    private readonly IAudio _audio;
    private readonly IClock _clock;
    private readonly IInput _input;

    private readonly Display _display = new();

    private readonly byte[] _memory = new byte[64 * 1024];
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

    IDisplay IChip8Machine.Display => _display;
    public Display Display => _display;
    public IInput Input => _input;

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public byte ReadRegister(int register) => _vRegisters[register];

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void WriteRegister(int register, byte value) => _vRegisters[register] = value;

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public int ReadIndexRegister() => _indexRegister;

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void WriteIndexRegister(int value) => _indexRegister = value & 0xFFFF;

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public int ReadIndexRegisterWithOffset(int offset) => (_indexRegister + offset) & 0xFFFF;

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public byte ReadMemory(int address) => _memory[address];

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void WriteMemory(int address, byte value) => _memory[address] = value;

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public int ReadProgramCounter() => _programCounter;

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void WriteProgramCounter(int value) => _programCounter = value;

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void AdvanceProgramCounter() => _programCounter += InstructionSizeInBytes;

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public byte ReadDelayTimer() => _delayTimer;

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void WriteDelayTimer(byte value) => _delayTimer = value;

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public byte ReadSoundTimer() => _soundTimer;

    public void WriteSoundTimer(byte value)
    {
        var prev = _soundTimer;
        _soundTimer = value;
        if (prev == 0 && _soundTimer != 0)
        {
            _audio.PlaySound();
        }
        else if (prev != 0 && _soundTimer == 0)
        {
            _audio.StopSound();
        }
    }

    public void PushStack(int value)
    {
        var nextStackPointer = _stackPointer + 1;
        if (nextStackPointer >= _stack.Length)
            throw new InvalidOperationException("Stack overflow");
        _stackPointer = nextStackPointer;
        _stack[_stackPointer] = value;
    }

    public int PopStack()
    {
        if (_stackPointer < 0)
            throw new InvalidOperationException("Stack underflow");

        var value = _stack[_stackPointer];
        _stackPointer--;
        return value;
    }

    public void BeginWaitForKey(int registerIndex)
    {
        _isWaitingForKey = true;
        _keyRegisterIndex = registerIndex;
    }

    public void BeginWaitForVBlank()
    {
        _waitForVBlank = true;
    }

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
                Cpu.FetchDecodeExecute(this);
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
            Cpu.FetchDecodeExecute(machine);
        }
    }
}
