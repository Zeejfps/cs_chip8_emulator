using System.Runtime.CompilerServices;
using Chip8Emulator.Core.Routines;

namespace Chip8Emulator.Core;

internal sealed partial class Cpu : ICpu
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

    public Cpu(
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

    public void StepInstruction() => FetchDecodeExecute();

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
}
