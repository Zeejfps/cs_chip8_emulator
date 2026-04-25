using System.Runtime.CompilerServices;

namespace Chip8Emulator.Core.Internal;

internal delegate void Routine(int ins);

internal sealed partial class Chip8Interpreter : IInterpreter
{
    public const int LowResFontBaseAddress = 0x050;
    public const int HighResFontBaseAddress = 0x0A0;
    public const int LowResFontCharWidth = 5;
    public const int HighResFontCharWidth = 10;
    public const int FlagBytes = 16;

    private const int InstructionSizeInBytes = 2;

    public IDisplay Display { get; }
    public IMemory Memory { get; }
    public IRegisters Registers { get; }
    public IStack Stack { get; }
    
    IReadOnlyMemory IInterpreter.Memory => Memory;
    IReadOnlyDisplay IInterpreter.Display => Display;
    IReadOnlyStack IInterpreter.Stack => Stack;
    IReadOnlyRegisters IInterpreter.Registers => Registers;

    public bool IsWaitingForKey => _isWaitingForKey;

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

    private readonly IClock _clock;
    private readonly IAudio _audio;
    private readonly IInput _input;
    private readonly IFlagStore _flagStore;
    private readonly IRenderer _renderer;

    private readonly long _ticksPerFrame;
    private long _ticksPerInstruction;
    private int _instructionsPerSecond = 1000;
    private long _lastTimestamp;
    private long _instructionAcc;
    private long _frameAcc;
    private bool _running;

    private bool _isWaitingForKey;
    private int _keyRegisterIndex;
    private bool _waitForVBlank;

    private bool _jumpUsesVx = true;
    private bool _loadStoreIncrementsI;
    private bool _logicResetsVf;

    internal readonly Routine[] UtilityRoutines;
    internal readonly Routine[] MainRoutines;
    internal readonly Routine[] SystemRoutines;
    internal readonly Routine[] InputRoutines;
    internal readonly Routine[] FiveOpRoutines;
    internal readonly Routine[] ArithmeticRoutines;

    public Chip8Interpreter(
        IClock clock,
        IDisplay display,
        IMemory memory,
        IAudio audio,
        IInput input,
        IRegisters registers,
        IStack stack,
        IFlagStore flagStore,
        IRenderer renderer)
    {
        _clock = clock;
        Display = display;
        Memory = memory;
        _audio = audio;
        _input = input;
        Registers = registers;
        Stack = stack;
        _flagStore = flagStore;
        _renderer = renderer;

        _ticksPerFrame = clock.Frequency / 60;
        _ticksPerInstruction = clock.Frequency / _instructionsPerSecond;
        _lastTimestamp = clock.Timestamp;

        MainRoutines = LoadMainRoutines();
        SystemRoutines = LoadSystemRoutines();
        UtilityRoutines = LoadUtilityRoutines();
        InputRoutines = LoadInputRoutines();
        FiveOpRoutines = LoadFiveOpRoutines();
        ArithmeticRoutines = LoadArithmeticRoutines();

        ApplyJumpUsesVx();
        ApplyLoadStoreIncrementsI();
        ApplyLogicResetsVf();
    }

    public void LoadProgram(ReadOnlySpan<byte> program)
    {
        ResetMemory();
        Registers.Clear();
        Stack.Clear();
        Registers.WritePc(0x200);
        Memory.Write(0x200, program);

        _audio.Reset();
        Display.Reset();

        _isWaitingForKey = false;
        _keyRegisterIndex = 0;
        _waitForVBlank = false;
        _instructionAcc = 0;
        _frameAcc = 0;
        _lastTimestamp = _clock.Timestamp;

        // Classic CHIP-8 HIRES signature: programs starting with `1260` (JP 0x260)
        // switch the display to a 64x64 canvas. See Hans Christian Egeberg / David Winter.
        if (program.Length >= 2 && program[0] == 0x12 && program[1] == 0x60)
        {
            Display.EnableClassicHiresMode();
        }
    }

    private void ResetMemory()
    {
        Memory.Clear();
        Memory.Write(LowResFontBaseAddress, LowResFont);
        Memory.Write(HighResFontBaseAddress, HighResFont);
    }

    public void Start()
    {
        if (_running) throw new InvalidOperationException("Machine is already started.");
        _lastTimestamp = _clock.Timestamp;
        _clock.Ticked += OnTicked;
        _running = true;
    }

    public void Stop()
    {
        if (!_running) return;
        _clock.Ticked -= OnTicked;
        _running = false;
        if (_audio.IsPlaying) _audio.StopSound();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private void AdvanceProgramCounter() => Registers.WritePc(Registers.ReadPc() + InstructionSizeInBytes);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private void FetchDecodeExecute()
    {
        var ins = Fetch();
        AdvanceProgramCounter();
        var opcode = (ins & 0xF000) >> 12;
        var execute = MainRoutines[opcode];
        execute(ins);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private int Fetch()
    {
        var pc = Registers.ReadPc();
        return Memory.Read(pc) << 8 | Memory.Read(pc + 1);
    }

    private void OnTicked(object? sender, EventArgs e)
    {
        var delta = CalculateDeltaTime();
        if (delta == 0) return;

        var maxDelta = _ticksPerFrame * 2;
        if (delta > maxDelta) delta = maxDelta;

        if (_isWaitingForKey && _input.WasAnyKeyPressedAndReleased(out var key))
        {
            Registers.WriteV(_keyRegisterIndex, key);
            _isWaitingForKey = false;
        }

        _frameAcc += delta;

        if (_isWaitingForKey || _waitForVBlank)
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
                if (_isWaitingForKey || _waitForVBlank)
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

        var st = Registers.ReadSt();
        if (st > 0 && !_audio.IsPlaying) _audio.PlaySound();
        else if (st == 0 && _audio.IsPlaying) _audio.StopSound();
    }

    private long CalculateDeltaTime()
    {
        var now = _clock.Timestamp;
        var delta = now - _lastTimestamp;
        _lastTimestamp = now;
        return delta;
    }

    private void StepFrame()
    {
        var dt = Registers.ReadDt();
        if (dt > 0) Registers.WriteDt((byte)(dt - 1));

        var st = Registers.ReadSt();
        if (st > 0) Registers.WriteSt((byte)(st - 1));

        _renderer.Render(Display);
        _frameAcc -= _ticksPerFrame;
        _waitForVBlank = false;
    }

    private void SaveFlags(int count)
    {
        Span<byte> buffer = stackalloc byte[FlagBytes];
        _flagStore.LoadInto(buffer);
        for (var i = 0; i <= count && i < buffer.Length; i++)
        {
            buffer[i] = Registers.ReadV(i);
        }
        _flagStore.SaveFrom(buffer);
    }

    private void LoadFlags(int count)
    {
        Span<byte> buffer = stackalloc byte[FlagBytes];
        _flagStore.LoadInto(buffer);
        for (var i = 0; i <= count && i < buffer.Length; i++)
        {
            Registers.WriteV(i, buffer[i]);
        }
    }

    private void ApplyJumpUsesVx()
    {
        MainRoutines[0xB] = _jumpUsesVx
            ? ExecuteJumpWithVxOffsetIns
            : ExecuteJumpWithV0OffsetIns;
    }

    private void ApplyLoadStoreIncrementsI()
    {
        UtilityRoutines[0x55] = _loadStoreIncrementsI
            ? ExecuteStoreRegistersIncIIns
            : ExecuteStoreRegistersKeepIIns;
        UtilityRoutines[0x65] = _loadStoreIncrementsI
            ? ExecuteLoadRegistersIncIIns
            : ExecuteLoadRegistersKeepIIns;
    }

    private void ApplyLogicResetsVf()
    {
        ArithmeticRoutines[0x1] = _logicResetsVf
            ? ExecuteBitwiseOrResetVfIns
            : ExecuteBitwiseOrPreserveVfIns;
        ArithmeticRoutines[0x2] = _logicResetsVf
            ? ExecuteBitwiseAndResetVfIns
            : ExecuteBitwiseAndPreserveVfIns;
        ArithmeticRoutines[0x3] = _logicResetsVf
            ? ExecuteXorResetVfIns
            : ExecuteXorPreserveVfIns;
    }

    private static void NoOp(int ins) { }

    private Routine[] LoadMainRoutines()
    {
        var table = new Routine[16];
        table[0x0] = ins => { if ((ins & 0xFF00) == 0x0000) SystemRoutines[ins & 0x00FF](ins); };
        table[0x1] = JumpToAddress;
        table[0x2] = CallSubroutine;
        table[0x3] = SkipNextInsIfRegisterValueEqualsValue;
        table[0x4] = SkipNextInsIfRegisterValueNotEqualsValue;
        table[0x5] = ins => FiveOpRoutines[ins & 0x000F](ins);
        table[0x6] = SetRegisterValue;
        table[0x7] = AddValueToRegister;
        table[0x8] = ins => ArithmeticRoutines[ins & 0x000F](ins);
        table[0x9] = SkipNextInsIfRegisterValueNotEqualsRegisterValue;
        table[0xA] = SetIndexRegisterIns;
        table[0xB] = JumpWithOffsetIns;
        table[0xC] = GenerateRandomNum;
        table[0xD] = DrawToScreen;
        table[0xE] = ins => InputRoutines[ins & 0x00FF](ins);
        table[0xF] = ins => UtilityRoutines[ins & 0x00FF](ins);
        return table;
    }

    private Routine[] LoadSystemRoutines()
    {
        var routines = new Routine[256];
        Array.Fill(routines, NoOp);
        routines[0xE0] = ClearDisplay;
        routines[0xEE] = ReturnFromSubroutine;
        routines[0xFF] = EnableHiresMode;
        routines[0xFE] = DisableHiresMode;
        routines[0xFB] = ScrollRight;
        routines[0xFC] = ScrollLeft;
        for (var n = 0; n < 16; n++)
        {
            // 00CN — S-CHIP: scroll display down N rows.
            routines[0xC0 + n] = ScrollDown;
            // 00DN — XO-CHIP: scroll display up N rows.
            routines[0xD0 + n] = ScrollUp;
        }
        return routines;
    }

    private Routine[] LoadUtilityRoutines()
    {
        var routines = new Routine[256];
        Array.Fill(routines, NoOp);
        // F000 NNNN — XO-CHIP long load I with the 16-bit word following the opcode.
        routines[0x00] = LongLoadIndexRegister;
        // FN01 — XO-CHIP select bitplane mask (N = 0..3).
        routines[0x01] = SelectPlane;
        // F002 — XO-CHIP copy 16 bytes at [I] into audio pattern buffer.
        routines[0x02] = LoadAudioPattern;
        routines[0x07] = ReadDelayTimer;
        routines[0x0A] = WaitForKeyPressAndRelease;
        routines[0x15] = SetDelayTimer;
        routines[0x18] = SetSoundTimer;
        routines[0x1E] = AddVxToI;
        routines[0x29] = LoadLowResFontCharacter;
        routines[0x30] = LoadHighResFontCharacter;
        routines[0x33] = StoreBcdInMemory;
        // FX3A — XO-CHIP set audio playback pitch from Vx.
        routines[0x3A] = SetPitch;
        routines[0x55] = StoreRegisters;
        routines[0x65] = LoadRegisters;
        // FX75 / FX85 — SCHIP save/load V0..Vx to persistent user flags.
        routines[0x75] = SaveFlagsIns;
        routines[0x85] = LoadFlagsIns;
        return routines;
    }

    private Routine[] LoadInputRoutines()
    {
        var routines = new Routine[256];
        Array.Fill(routines, NoOp);
        routines[0x9E] = SkipNextInsIfKeyIsPressed;
        routines[0xA1] = SkipNextInsIfKeyIsReleased;
        return routines;
    }

    private Routine[] LoadFiveOpRoutines()
    {
        var routines = new Routine[16];
        Array.Fill(routines, NoOp);
        routines[0] = SkipIfVxEqualsVy;
        routines[2] = StoreRegisterRange;
        routines[3] = LoadRegisterRange;
        return routines;
    }

    private Routine[] LoadArithmeticRoutines()
    {
        var routines = new Routine[16];
        Array.Fill(routines, NoOp);
        routines[0x0] = SetRegisterValueFromRegister;
        routines[0x1] = BitwiseOrOnRegisters;
        routines[0x2] = BitwiseAndOnRegisters;
        routines[0x3] = XorRegisterValueFromRegister;
        routines[0x4] = AddValueToRegisterWithCarry;
        routines[0x5] = VxSubVy;
        routines[0x6] = ShiftRight;
        routines[0x7] = VySubVx;
        routines[0xE] = ShiftLeft;
        return routines;
    }
}
