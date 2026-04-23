using System.Runtime.CompilerServices;

namespace Chip8Emulator.Core.Cpu;

internal sealed partial class Chip8Machine : IChip8Machine, ICpu
{
    public const int LowResFontBaseAddress = 0x050;
    public const int HighResFontBaseAddress = 0x0A0;
    public const int LowRestFontCharWidth = 5;
    public const int HighRestFontCharWidth = 10;
    public const int InstructionSizeInBytes = 2;
    private const int AudioPatternSize = 16;
    private const byte DefaultPitch = 64;

    private readonly IRenderer _renderer;
    private readonly IAudio _audio;
    private readonly IClock _clock;
    private readonly IInput _input;
    private readonly IPersistentFlags _persistentFlags;
    private readonly IMemory _memory;
    private readonly IStack _stack;
    private readonly Display _display = new();
    private readonly byte[] _vRegisters = new byte[16];

    private readonly byte[] _audioPattern = new byte[AudioPatternSize];
    private byte _pitch = DefaultPitch;

    private readonly long _ticksPerFrame;
    
    private int _instructionsPerSecond = 1000;
    private byte _delayTimer;
    private byte _soundTimer;
    private int _programCounter;
    private int _indexRegister;

    private long _ticksPerInstruction;
    private long _lastTimestamp;
    private long _instructionAcc;
    private long _frameAcc;
    private bool _isWaitingForKey;
    private bool _waitForVBlank;
    private int _keyRegisterIndex;
    private bool _running;
    private bool _jumpUsesVx = true;
    private bool _loadStoreIncrementsI;
    private bool _logicResetsVf;

    public Routine[] TimerRoutines { get; }

    internal readonly Routine[] MainRoutines;
    internal readonly Routine[] SystemRoutines;
    internal readonly Routine[] KeyCheckRoutines;
    internal readonly Routine[] FiveOpRoutines;
    internal readonly Routine[] ArithmeticRoutines;

    public Chip8Machine(IRenderer renderer, IAudio audio, IClock clock, IInput input)
        : this(renderer, audio, clock, input, new InMemoryPersistentFlags())
    {
    }

    public Chip8Machine(IRenderer renderer, IAudio audio, IClock clock, IInput input, IPersistentFlags persistentFlags)
    {
        _renderer = renderer;
        _audio = audio;
        _clock = clock;
        _input = input;
        _persistentFlags = persistentFlags;
        _ticksPerFrame = clock.Frequency / 60;
        _ticksPerInstruction = clock.Frequency / _instructionsPerSecond;
        _lastTimestamp = clock.Timestamp;
        _stack = new Stack16();
        _memory = new Memory64K();
        _memory.Write(LowResFontBaseAddress, LowResFont);
        _memory.Write(HighResFontBaseAddress, HighResFont);
        Debugger = new Chip8MachineDebugger(this);

        MainRoutines = LoadMainRoutines();
        SystemRoutines = LoadSystemRoutines();
        TimerRoutines = LoadTimerRoutines();
        KeyCheckRoutines = LoadKeyCheckRoutines();
        FiveOpRoutines = LoadFiveOpRoutines();
        ArithmeticRoutines = LoadArithmeticRoutines();

        // Specialize quirk-sensitive slots to match current flag defaults.
        ApplyJumpUsesVx();
        ApplyLoadStoreIncrementsI();
        ApplyLogicResetsVf();
    }

    public IMemory Memory => _memory;
    public IStack Stack => _stack;

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

    public bool ShiftUsesVy { get; set; }
    public bool SpritesWrap { get; set; }
    public bool DisplayWait { get; set; }
    public bool VfResultWrittenLast { get; set; }

    public bool JumpUsesVx
    {
        get => _jumpUsesVx;
        set { _jumpUsesVx = value; ApplyJumpUsesVx(); }
    }

    public bool LoadStoreIncrementsI
    {
        get => _loadStoreIncrementsI;
        set { _loadStoreIncrementsI = value; ApplyLoadStoreIncrementsI(); }
    }

    public bool LogicResetsVf
    {
        get => _logicResetsVf;
        set { _logicResetsVf = value; ApplyLogicResetsVf(); }
    }

    private void ApplyJumpUsesVx()
    {
        MainRoutines[0xB] = _jumpUsesVx
            ? Chip8InstructionSet.ExecuteJumpWithVxOffsetIns
            : Chip8InstructionSet.ExecuteJumpWithV0OffsetIns;
    }

    private void ApplyLoadStoreIncrementsI()
    {
        TimerRoutines[0x55] = _loadStoreIncrementsI
            ? Chip8InstructionSet.ExecuteStoreRegistersIncIIns
            : Chip8InstructionSet.ExecuteStoreRegistersKeepIIns;
        TimerRoutines[0x65] = _loadStoreIncrementsI
            ? Chip8InstructionSet.ExecuteLoadRegistersIncIIns
            : Chip8InstructionSet.ExecuteLoadRegistersKeepIIns;
    }

    private void ApplyLogicResetsVf()
    {
        ArithmeticRoutines[0x1] = _logicResetsVf
            ? Chip8InstructionSet.ExecuteBitwiseOrResetVfIns
            : Chip8InstructionSet.ExecuteBitwiseOrPreserveVfIns;
        ArithmeticRoutines[0x2] = _logicResetsVf
            ? Chip8InstructionSet.ExecuteBitwiseAndResetVfIns
            : Chip8InstructionSet.ExecuteBitwiseAndPreserveVfIns;
        ArithmeticRoutines[0x3] = _logicResetsVf
            ? Chip8InstructionSet.ExecuteXorResetVfIns
            : Chip8InstructionSet.ExecuteXorPreserveVfIns;
    }

    public IDisplay Display => _display;
    public IInput Input => _input;

    public void ScrollDisplayDown(int n) => _display.ScrollDown(n);
    public void ScrollDisplayUp(int n) => _display.ScrollUp(n);
    public void ScrollDisplayLeft(int n) => _display.ScrollLeft(n);
    public void ScrollDisplayRight(int n) => _display.ScrollRight(n);
    public void ClearDisplay() => _display.Clear();
    public void EnableHighResMode() => _display.EnableHighResMode();
    public void DisableHighResMode() => _display.DisableHighResMode();

    public byte SelectedPlanes
    {
        get => _display.SelectedPlanes;
        set => _display.SelectedPlanes = (byte)(value & Cpu.Display.AllPlanesMask);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private void FetchDecodeExecute()
    {
        var ins = Fetch();
        AdvanceProgramCounter();
        var opcode = (ins & 0xF000) >> 12;
        var execute = MainRoutines[opcode];
        execute(this, ins);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private int Fetch()
    {
        var pc = ReadProgramCounter();
        var ins = ReadMemory(pc) << 8 | ReadMemory(pc + 1);
        return ins;
    }
    
    public void LoadAudioPattern()
    {
        for (var i = 0; i < AudioPatternSize; i++)
        {
            var value = _memory.Read(ReadIndexRegisterWithOffset(i));
            _audioPattern[i] = value;
        }
        PushPatternToAudio();
    }

    public void SetPitch(byte pitch)
    {
        _pitch = pitch;
        PushPatternToAudio();
    }

    public byte Pitch => _pitch;

    public ReadOnlySpan<byte> AudioPattern => _audioPattern;

    public double AudioFrequencyHz => 4000.0 * Math.Pow(2.0, (_pitch - 64) / 48.0);

    private void PushPatternToAudio()
    {
        _audio.SetPattern(_audioPattern, AudioFrequencyHz);
    }

    public void SaveFlags(int count)
    {
        Span<byte> buffer = stackalloc byte[IPersistentFlags.Capacity];
        _persistentFlags.Read(buffer);
        for (var i = 0; i <= count && i < buffer.Length; i++)
        {
            buffer[i] = _vRegisters[i];
        }
        _persistentFlags.Write(buffer);
    }

    public void LoadFlags(int count)
    {
        Span<byte> buffer = stackalloc byte[IPersistentFlags.Capacity];
        _persistentFlags.Read(buffer);
        for (var i = 0; i <= count && i < buffer.Length; i++)
        {
            _vRegisters[i] = buffer[i];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public byte ReadGeneralPurposeRegister(int register) => _vRegisters[register];

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void WriteGeneralPurposeRegister(int register, byte value) => _vRegisters[register] = value;

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public int ReadIndexRegister() => _indexRegister;

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void WriteIndexRegister(int value) => _indexRegister = value & 0xFFFF;

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public int ReadIndexRegisterWithOffset(int offset) => (_indexRegister + offset) & 0xFFFF;

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public byte ReadMemory(int address) => _memory.Read(address);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void WriteMemory(int address, byte value) => _memory.Write(address, value);

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

    public void BeginWaitForKey(int registerIndex)
    {
        _isWaitingForKey = true;
        _keyRegisterIndex = registerIndex;
    }

    public void BeginWaitForVBlank()
    {
        _waitForVBlank = true;
    }
    
    public void WriteMemory(int address, ReadOnlySpan<byte> data)
    {
        _memory.Write(address, data);
    }

    public void LoadProgram(ReadOnlySpan<byte> program)
    {
        ResetMemory();
        ResetClock();
        ResetTimers();
        ResetDisplay();
        ResetRegisters();
        ResetStack();
        ResetAudioPattern();
        _indexRegister = 0;
        _isWaitingForKey = false;
        _waitForVBlank = false;
        _keyRegisterIndex = 0;
        _memory.Write(0x200, program);
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
        _memory.Clear();
        _memory.Write(LowResFontBaseAddress, LowResFont);
        _memory.Write(HighResFontBaseAddress, HighResFont);
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
       _stack.Clear();
    }

    private void ResetAudioPattern()
    {
        Array.Clear(_audioPattern);
        _pitch = DefaultPitch;
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

    private sealed class Chip8MachineDebugger(Chip8Machine machine) : IMachineDebugger
    {
        public ReadOnlySpan<byte> Memory => machine._memory.AsReadOnlySpan();
        public ReadOnlySpan<byte> Registers => machine._vRegisters;
        public ReadOnlySpan<int> Stack => machine._stack.AsReadOnlySpan();
        public int ProgramCounter => machine._programCounter;
        public int IndexRegister => machine._indexRegister;
        public int StackPointer => machine._stack.StackPointer;
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
