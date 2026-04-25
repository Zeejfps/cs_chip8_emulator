using System.Runtime.CompilerServices;

namespace Chip8Emulator.Core.Spec;

internal delegate void Routine(in DecodedOp op);

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

    internal readonly Routine[] Routines;

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

        Routines = LoadRoutines();

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
        var op = Chip8Decoder.Decode(Fetch());
        AdvanceProgramCounter();
        Routines[(int)op.Kind](in op);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private int Fetch()
    {
        var pc = Registers.ReadPc();
        return Memory.Read(pc) << 8 | Memory.Read(pc + 1);
    }

    // Test helper: decode + dispatch a single instruction without touching PC or the clock loop.
    internal void Step(int ins)
    {
        var op = Chip8Decoder.Decode(ins);
        Routines[(int)op.Kind](in op);
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
        Routines[(int)OpKind.JpV0] = _jumpUsesVx
            ? ExecuteJumpWithVxOffsetIns
            : ExecuteJumpWithV0OffsetIns;
    }

    private void ApplyLoadStoreIncrementsI()
    {
        Routines[(int)OpKind.LdIVx] = _loadStoreIncrementsI
            ? ExecuteStoreRegistersIncIIns
            : ExecuteStoreRegistersKeepIIns;
        Routines[(int)OpKind.LdVxI] = _loadStoreIncrementsI
            ? ExecuteLoadRegistersIncIIns
            : ExecuteLoadRegistersKeepIIns;
    }

    private void ApplyLogicResetsVf()
    {
        Routines[(int)OpKind.OrVxVy] = _logicResetsVf
            ? ExecuteBitwiseOrResetVfIns
            : ExecuteBitwiseOrPreserveVfIns;
        Routines[(int)OpKind.AndVxVy] = _logicResetsVf
            ? ExecuteBitwiseAndResetVfIns
            : ExecuteBitwiseAndPreserveVfIns;
        Routines[(int)OpKind.XorVxVy] = _logicResetsVf
            ? ExecuteXorResetVfIns
            : ExecuteXorPreserveVfIns;
    }

    private static void NoOp(in DecodedOp op) { }

    private Routine[] LoadRoutines()
    {
        var count = Enum.GetValues<OpKind>().Length;
        var table = new Routine[count];
        Array.Fill(table, NoOp);

        table[(int)OpKind.Cls] = ClearDisplay;
        table[(int)OpKind.Ret] = ReturnFromSubroutine;
        table[(int)OpKind.ScrollDown] = ScrollDown;
        table[(int)OpKind.ScrollUp] = ScrollUp;
        table[(int)OpKind.ScrollRight] = ScrollRight;
        table[(int)OpKind.ScrollLeft] = ScrollLeft;
        table[(int)OpKind.DisableHires] = DisableHiresMode;
        table[(int)OpKind.EnableHires] = EnableHiresMode;

        table[(int)OpKind.Jp] = JumpToAddress;
        table[(int)OpKind.Call] = CallSubroutine;
        table[(int)OpKind.SeVxImm] = SkipNextInsIfRegisterValueEqualsValue;
        table[(int)OpKind.SneVxImm] = SkipNextInsIfRegisterValueNotEqualsValue;

        table[(int)OpKind.SeVxVy] = SkipIfVxEqualsVy;
        table[(int)OpKind.StoreRegisterRange] = StoreRegisterRange;
        table[(int)OpKind.LoadRegisterRange] = LoadRegisterRange;

        table[(int)OpKind.LdVxImm] = SetRegisterValue;
        table[(int)OpKind.AddVxImm] = AddValueToRegister;

        table[(int)OpKind.LdVxVy] = SetRegisterValueFromRegister;
        // OrVxVy / AndVxVy / XorVxVy filled by ApplyLogicResetsVf.
        table[(int)OpKind.AddVxVy] = AddValueToRegisterWithCarry;
        table[(int)OpKind.SubVxVy] = VxSubVy;
        table[(int)OpKind.ShrVx] = ShiftRight;
        table[(int)OpKind.SubnVxVy] = VySubVx;
        table[(int)OpKind.ShlVx] = ShiftLeft;

        table[(int)OpKind.SneVxVy] = SkipNextInsIfRegisterValueNotEqualsRegisterValue;
        table[(int)OpKind.LdIImm] = SetIndexRegisterIns;
        // JpV0 filled by ApplyJumpUsesVx.
        table[(int)OpKind.Rnd] = GenerateRandomNum;
        table[(int)OpKind.Drw] = DrawToScreen;

        table[(int)OpKind.Skp] = SkipNextInsIfKeyIsPressed;
        table[(int)OpKind.Sknp] = SkipNextInsIfKeyIsReleased;

        table[(int)OpKind.LongLoadI] = LongLoadIndexRegister;
        table[(int)OpKind.SelectPlane] = SelectPlane;
        table[(int)OpKind.LoadAudioPattern] = LoadAudioPattern;
        table[(int)OpKind.LdVxDt] = ReadDelayTimer;
        table[(int)OpKind.LdVxK] = WaitForKeyPressAndRelease;
        table[(int)OpKind.LdDtVx] = SetDelayTimer;
        table[(int)OpKind.LdStVx] = SetSoundTimer;
        table[(int)OpKind.AddIVx] = AddVxToI;
        table[(int)OpKind.LdFVx] = LoadLowResFontCharacter;
        table[(int)OpKind.LdHfVx] = LoadHighResFontCharacter;
        table[(int)OpKind.LdBVx] = StoreBcdInMemory;
        table[(int)OpKind.SetPitch] = SetPitch;
        // LdIVx / LdVxI filled by ApplyLoadStoreIncrementsI.
        table[(int)OpKind.SaveFlags] = SaveFlagsIns;
        table[(int)OpKind.LoadFlags] = LoadFlagsIns;

        return table;
    }
}
