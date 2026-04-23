using System.Runtime.CompilerServices;
using Chip8Emulator.Core.Routines;

namespace Chip8Emulator.Core;

internal delegate void Routine(EmulatedCpu cpu, int ins);

internal sealed class EmulatedCpu : ICpu
{
    public const int InstructionSizeInBytes = 2;

    private readonly IMemory _memory;
    private readonly IDisplay _display;
    private readonly IInput _input;
    private readonly IAudio _audio;
    private readonly IRegisters _registers;
    private readonly IStack _stack;
    private readonly IPersistentFlags _persistentFlags;

    private int _programCounter;
    private bool _isWaitingForKey;
    private bool _waitForVBlank;
    private int _keyRegisterIndex;

    private bool _jumpUsesVx = true;
    private bool _loadStoreIncrementsI;
    private bool _logicResetsVf;

    internal readonly Routine[] TimerRoutines;
    internal readonly Routine[] MainRoutines;
    internal readonly Routine[] SystemRoutines;
    internal readonly Routine[] KeyCheckRoutines;
    internal readonly Routine[] FiveOpRoutines;
    internal readonly Routine[] ArithmeticRoutines;

    public EmulatedCpu(
        IMemory memory,
        IDisplay display,
        IInput input,
        IAudio audio,
        IRegisters registers,
        IStack stack,
        IPersistentFlags persistentFlags)
    {
        _memory = memory;
        _display = display;
        _input = input;
        _audio = audio;
        _registers = registers;
        _stack = stack;
        _persistentFlags = persistentFlags;

        MainRoutines = LoadMainRoutines();
        SystemRoutines = LoadSystemRoutines();
        TimerRoutines = LoadTimerRoutines();
        KeyCheckRoutines = LoadKeyCheckRoutines();
        FiveOpRoutines = LoadFiveOpRoutines();
        ArithmeticRoutines = LoadArithmeticRoutines();

        ApplyJumpUsesVx();
        ApplyLoadStoreIncrementsI();
        ApplyLogicResetsVf();
    }

    public IMemory Memory => _memory;
    public IDisplay Display => _display;
    public IInput Input => _input;
    public IAudio Audio => _audio;
    public IRegisters Registers => _registers;
    public IStack Stack => _stack;

    public int ProgramCounter => _programCounter;
    public bool IsWaitingForKey => _isWaitingForKey;

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

    public void BeginWaitForKey(int registerIndex)
    {
        _isWaitingForKey = true;
        _keyRegisterIndex = registerIndex;
    }

    public void BeginWaitForVBlank()
    {
        _waitForVBlank = true;
    }

    public void ClearVBlankWait()
    {
        _waitForVBlank = false;
    }

    public bool CanExecute => !_waitForVBlank && !_isWaitingForKey;

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void FetchDecodeExecute()
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
        var pc = _programCounter;
        return _memory.Read(pc) << 8 | _memory.Read(pc + 1);
    }

    public void TryResumeFromKeyPress()
    {
        if (!_isWaitingForKey) return;
        if (_input.WasAnyKeyPressedAndReleased(out var key))
        {
            _registers.WriteV(_keyRegisterIndex, key);
            _isWaitingForKey = false;
        }
    }

    public void TickTimers()
    {
        var dt = _registers.ReadDt();
        if (dt > 0) _registers.WriteDt((byte)(dt - 1));

        var st = _registers.ReadSt();
        if (st > 0)
        {
            st--;
            _registers.WriteSt(st);
            if (st == 0) _audio.StopSound();
        }
    }

    public void OnStart()
    {
        if (_registers.ReadSt() > 0) _audio.PlaySound();
    }

    public void OnStop()
    {
        if (_registers.ReadSt() > 0) _audio.StopSound();
    }

    public void Reset(int programCounter)
    {
        _registers.Clear();
        _stack.Clear();
        _audio.Reset();
        _display.Reset();
        _programCounter = programCounter;
        _isWaitingForKey = false;
        _waitForVBlank = false;
        _keyRegisterIndex = 0;
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

    private static void NoOp(EmulatedCpu cpu, int ins) { }

    private Routine[] LoadMainRoutines()
    {
        var table = new Routine[16];
        table[0x0] = (cpu, ins) => { if ((ins & 0xFF00) == 0x0000) SystemRoutines[ins & 0x00FF](cpu, ins); };
        table[0x1] = Chip8Routines.JumpToAddress;
        table[0x2] = Chip8Routines.CallSubroutine;
        table[0x3] = Chip8Routines.SkipNextInsIfRegisterValueEqualsValue;
        table[0x4] = Chip8Routines.SkipNextInsIfRegisterValueNotEqualsValue;
        table[0x5] = (cpu, ins) => FiveOpRoutines[ins & 0x000F](cpu, ins);
        table[0x6] = Chip8Routines.SetRegisterValue;
        table[0x7] = Chip8Routines.AddValueToRegister;
        table[0x8] = (cpu, ins) => ArithmeticRoutines[ins & 0x000F](cpu, ins);
        table[0x9] = Chip8Routines.SkipNextInsIfRegisterValueNotEqualsRegisterValue;
        table[0xA] = Chip8Routines.SetIndexRegisterIns;
        table[0xB] = Chip8Routines.JumpWithOffsetIns;
        table[0xC] = Chip8Routines.GenerateRandomNum;
        table[0xD] = Chip8Routines.DrawToScreen;
        table[0xE] = (cpu, ins) => KeyCheckRoutines[ins & 0x00FF](cpu, ins);
        table[0xF] = (cpu, ins) => TimerRoutines[ins & 0x00FF](cpu, ins);
        return table;
    }

    private Routine[] LoadSystemRoutines()
    {
        var table = new Routine[256];
        Array.Fill(table, NoOp);
        table[0xE0] = Chip8Routines.ClearDisplay;
        table[0xEE] = Chip8Routines.ReturnFromSubroutine;
        table[0xFF] = SChipRoutines.EnableHiresMode;
        table[0xFE] = SChipRoutines.DisableHiresMode;
        table[0xFB] = SChipRoutines.ScrollRight;
        table[0xFC] = SChipRoutines.ScrollLeft;
        for (var n = 0; n < 16; n++)
        {
            // 00CN — S-CHIP: scroll display down N rows.
            table[0xC0 + n] = SChipRoutines.ScrollDown;
            // 00DN — XO-CHIP: scroll display up N rows.
            table[0xD0 + n] = XoChipRoutines.ScrollUp;
        }
        return table;
    }

    private Routine[] LoadTimerRoutines()
    {
        var table = new Routine[256];
        Array.Fill(table, NoOp);
        // F000 NNNN — XO-CHIP long load I with the 16-bit word following the opcode.
        table[0x00] = XoChipRoutines.LongLoadIndexRegister;
        // FN01 — XO-CHIP select bitplane mask (N = 0..3).
        table[0x01] = XoChipRoutines.SelectPlane;
        // F002 — XO-CHIP copy 16 bytes at [I] into audio pattern buffer.
        table[0x02] = XoChipRoutines.LoadAudioPattern;
        table[0x07] = Chip8Routines.ReadDelayTimer;
        table[0x0A] = Chip8Routines.WaitForKeyPressAndRelease;
        table[0x15] = Chip8Routines.SetDelayTimer;
        table[0x18] = Chip8Routines.SetSoundTimer;
        table[0x1E] = Chip8Routines.AddVxToI;
        table[0x29] = Chip8Routines.LoadLowResFontCharacter;
        table[0x30] = SChipRoutines.LoadHighResFontCharacter;
        table[0x33] = Chip8Routines.StoreBcdInMemory;
        // FX3A — XO-CHIP set audio playback pitch from Vx.
        table[0x3A] = XoChipRoutines.SetPitch;
        table[0x55] = Chip8Routines.StoreRegisters;
        table[0x65] = Chip8Routines.LoadRegisters;
        // FX75 / FX85 — SCHIP save/load V0..Vx to persistent user flags.
        table[0x75] = SChipRoutines.SaveFlags;
        table[0x85] = SChipRoutines.LoadFlags;
        return table;
    }

    private Routine[] LoadKeyCheckRoutines()
    {
        var table = new Routine[256];
        Array.Fill(table, NoOp);
        table[0x9E] = Chip8Routines.SkipNextInsIfKeyIsPressed;
        table[0xA1] = Chip8Routines.SkipNextInsIfKeyIsReleased;
        return table;
    }

    private Routine[] LoadFiveOpRoutines()
    {
        var table = new Routine[16];
        Array.Fill(table, NoOp);
        table[0] = Chip8Routines.SkipIfVxEqualsVy;
        table[2] = XoChipRoutines.StoreRegisterRange;
        table[3] = XoChipRoutines.LoadRegisterRange;
        return table;
    }

    private Routine[] LoadArithmeticRoutines()
    {
        var table = new Routine[16];
        Array.Fill(table, NoOp);
        table[0x0] = Chip8Routines.SetRegisterValueFromRegister;
        table[0x1] = Chip8Routines.BitwiseOrOnRegisters;
        table[0x2] = Chip8Routines.BitwiseAndOnRegisters;
        table[0x3] = Chip8Routines.XorRegisterValueFromRegister;
        table[0x4] = Chip8Routines.AddValueToRegisterWithCarry;
        table[0x5] = Chip8Routines.VxSubVy;
        table[0x6] = Chip8Routines.ShiftRight;
        table[0x7] = Chip8Routines.VySubVx;
        table[0xE] = Chip8Routines.ShiftLeft;
        return table;
    }
}
