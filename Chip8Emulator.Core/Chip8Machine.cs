using System.Runtime.CompilerServices;
using Chip8Emulator.Core.Routines;

namespace Chip8Emulator.Core;

internal sealed partial class Chip8Machine : IChip8Machine, ICpu
{
    public const int LowResFontBaseAddress = 0x050;
    public const int HighResFontBaseAddress = 0x0A0;
    public const int LowRestFontCharWidth = 5;
    public const int HighRestFontCharWidth = 10;
    public const int InstructionSizeInBytes = 2;

    private readonly IRenderer _renderer;
    private readonly IAudio _audio;
    private readonly IClock _clock;
    private readonly IInput _input;
    private readonly IPersistentFlags _persistentFlags;
    private readonly IMemory _memory;
    private readonly IStack _stack;
    private readonly IRegisters _registers = new EmulatedRegisters();
    private readonly EmulatedDisplay _display = new();
    
    private readonly long _ticksPerFrame;
    
    private int _instructionsPerSecond = 1000;
    private int _programCounter;

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
        : this(renderer, audio, clock, input, new EmulatedPersistentFlags())
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
        _stack = new EmulatedStack();
        _memory = new EmulatedMemory();
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

    public IAudio Audio => _audio;
    public IMemory Memory => _memory;
    public IStack Stack => _stack;
    public IRegisters Registers => _registers;
    public IDisplay Display => _display;
    public IInput Input => _input;
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
            ? Chip8Routines.ExecuteJumpWithVxOffsetIns
            : Chip8Routines.ExecuteJumpWithV0OffsetIns;
    }

    private void ApplyLoadStoreIncrementsI()
    {
        TimerRoutines[0x55] = _loadStoreIncrementsI
            ? Chip8Routines.ExecuteStoreRegistersIncIIns
            : Chip8Routines.ExecuteStoreRegistersKeepIIns;
        TimerRoutines[0x65] = _loadStoreIncrementsI
            ? Chip8Routines.ExecuteLoadRegistersIncIIns
            : Chip8Routines.ExecuteLoadRegistersKeepIIns;
    }

    private void ApplyLogicResetsVf()
    {
        ArithmeticRoutines[0x1] = _logicResetsVf
            ? Chip8Routines.ExecuteBitwiseOrResetVfIns
            : Chip8Routines.ExecuteBitwiseOrPreserveVfIns;
        ArithmeticRoutines[0x2] = _logicResetsVf
            ? Chip8Routines.ExecuteBitwiseAndResetVfIns
            : Chip8Routines.ExecuteBitwiseAndPreserveVfIns;
        ArithmeticRoutines[0x3] = _logicResetsVf
            ? Chip8Routines.ExecuteXorResetVfIns
            : Chip8Routines.ExecuteXorPreserveVfIns;
    }

    public byte SelectedPlanes
    {
        get => _display.SelectedPlanes;
        set => _display.SelectedPlanes = (byte)(value & Core.EmulatedDisplay.AllPlanesMask);
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
        var ins = _memory.Read(pc) << 8 | _memory.Read(pc + 1);
        return ins;
    }
    
    public void SaveFlags(int count)
    {
        Span<byte> buffer = stackalloc byte[IPersistentFlags.Capacity];
        _persistentFlags.Read(buffer);
        for (var i = 0; i <= count && i < buffer.Length; i++)
        {
            buffer[i] = _registers.ReadV(i);
        }
        _persistentFlags.Write(buffer);
    }

    public void LoadFlags(int count)
    {
        Span<byte> buffer = stackalloc byte[IPersistentFlags.Capacity];
        _persistentFlags.Read(buffer);
        for (var i = 0; i <= count && i < buffer.Length; i++)
        {
            _registers.WriteV(i, buffer[i]);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public int ReadProgramCounter() => _programCounter;

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void WriteProgramCounter(int value) => _programCounter = value;

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void AdvanceProgramCounter() => _programCounter += InstructionSizeInBytes;

    public void BeginWaitForKey(int registerIndex)
    {
        _isWaitingForKey = true;
        _keyRegisterIndex = registerIndex;
    }

    public void BeginWaitForVBlank()
    {
        _waitForVBlank = true;
    }

    public void LoadProgram(ReadOnlySpan<byte> program)
    {
        ResetMemory();
        ResetAccumulators();
        ResetDisplay();
        ResetRegisters();
        ResetStack();
        ResetAudio();
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

    private void ResetAudio()
    {
        _audio.Reset();
    }

    private void ResetAccumulators()
    {
        _instructionAcc = 0;
        _frameAcc = 0;
        _lastTimestamp = _clock.Timestamp;
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
        _registers.Clear();
    }

    private void ResetStack()
    {
       _stack.Clear();
    }

    public void Start()
    {
        if (_running) throw new InvalidOperationException("Machine is already started.");
        _lastTimestamp = _clock.Timestamp;
        _clock.Ticked += OnTicked;
        _running = true;
        if (_registers.ReadSt() > 0)
        {
            _audio.PlaySound();
        }
    }

    public void Stop()
    {
        if (!_running) return;
        _clock.Ticked -= OnTicked;
        _running = false;
        if (_registers.ReadSt() > 0)
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
                _registers.WriteV(_keyRegisterIndex, key);
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
        var delayTimer = _registers.ReadDt();
        if (delayTimer > 0)
        {
            delayTimer--;
            _registers.WriteDt(delayTimer);
        }

        var soundTimer = _registers.ReadSt();
        if (soundTimer > 0)
        {
            soundTimer--;
            _registers.WriteSt(soundTimer);
            if (soundTimer == 0)
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
        public int ProgramCounter => machine._programCounter;
        public bool IsWaitingForKey => machine._isWaitingForKey;
        public bool IsWaitingForVBlank => machine._waitForVBlank;

        public void StepInstruction()
        {
            machine.FetchDecodeExecute();
        }
    }
}
