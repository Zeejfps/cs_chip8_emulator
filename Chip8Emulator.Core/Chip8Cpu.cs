using System.Runtime.CompilerServices;

namespace Chip8Emulator.Core;

internal delegate void Routine(int ins);

public sealed partial class Chip8Cpu : ICpu
{
    private const int InstructionSizeInBytes = 2;

    private readonly IMemory _memory;
    private readonly IDisplay _display;
    private readonly IBus _bus;
    private readonly IPersistentFlags _persistentFlags;

    private bool _jumpUsesVx = true;
    private bool _loadStoreIncrementsI;
    private bool _logicResetsVf;

    internal readonly Routine[] UtilityRoutines;
    internal readonly Routine[] MainRoutines;
    internal readonly Routine[] SystemRoutines;
    internal readonly Routine[] InputRoutines;
    internal readonly Routine[] FiveOpRoutines;
    internal readonly Routine[] ArithmeticRoutines;

    public Chip8Cpu(
        IMemory memory,
        IDisplay display,
        IRegisters registers,
        IStack stack,
        IPersistentFlags persistentFlags,
        IBus bus)
    {
        _memory = memory;
        _display = display;
        Registers = registers;
        Stack = stack;
        _bus = bus;
        _persistentFlags = persistentFlags;

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

    public IRegisters Registers { get; }
    public IStack Stack { get; }

    private int _programCounter;
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

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public int ReadProgramCounter() => _programCounter;

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void WriteProgramCounter(int value) => _programCounter = value;

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void AdvanceProgramCounter() => _programCounter += InstructionSizeInBytes;

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void FetchDecodeExecute()
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
        var pc = _programCounter;
        return _memory.Read(pc) << 8 | _memory.Read(pc + 1);
    }

    public void SaveFlags(int count)
    {
        Span<byte> buffer = stackalloc byte[IPersistentFlags.Capacity];
        _persistentFlags.Read(buffer);
        for (var i = 0; i <= count && i < buffer.Length; i++)
        {
            buffer[i] = Registers.ReadV(i);
        }
        _persistentFlags.Write(buffer);
    }

    public void LoadFlags(int count)
    {
        Span<byte> buffer = stackalloc byte[IPersistentFlags.Capacity];
        _persistentFlags.Read(buffer);
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
