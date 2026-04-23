using Chip8Emulator.Core.Routines;
using Chip8Emulator.Core.Tests.Fakes;

namespace Chip8Emulator.Core.Tests;

public class Chip8InterpreterTests
{
    private readonly byte[] _pixelBuffer = new byte[Chip8Display.HighRestWidth * Chip8Display.HighRestHeight];

    private (Chip8Interpreter Emulator, Chip8Cpu Cpu) CreateEmulator(out FakeAudio audio, out FakeClock clock, out FakeInput input)
    {
        audio = new FakeAudio();
        clock = new FakeClock();
        input = new FakeInput();
        var display = new Chip8Display(size => _pixelBuffer.AsMemory(0, size));
        var stack = new Chip8Stack(size => new int[size]);
        var memory = new Chip8Memory(size => new byte[size]);
        var registers = new Chip8Registers(size => new byte[size]);
        var bus = new EmulatorBus();
        var cpu = new Chip8Cpu(memory, display, registers, stack, new EmulatedPersistentFlags(), bus);
        var emulator = new Chip8Interpreter(clock, display, memory, audio, input, bus, cpu);
        return (emulator, cpu);
    }

    private (Chip8Interpreter Emulator, Chip8Cpu Cpu) CreateEmulator() => CreateEmulator(out _, out _, out _);

    private static byte[] ReadMemorySlice(Chip8Interpreter emulator, int address, int length)
    {
        var result = new byte[length];
        for (var i = 0; i < length; i++)
            result[i] = emulator.Memory.Read(address + i);
        return result;
    }
    private (Chip8Interpreter Emulator, Chip8Cpu Cpu) CreateEmulator(out FakeInput input) => CreateEmulator(out _, out _, out input);
    private (Chip8Interpreter Emulator, Chip8Cpu Cpu) CreateEmulator(out FakeClock clock, out FakeInput input) => CreateEmulator(out _, out clock, out input);

    [Fact]
    public void InitialState_IsZeroed()
    {
        var (emulator, cpu) = CreateEmulator();

        Assert.Equal(0, cpu.ReadProgramCounter());
        Assert.Equal(0, cpu.Registers.ReadI());
        Assert.Equal(0, cpu.Registers.ReadDt());
        Assert.Equal(0, cpu.Registers.ReadSt());
    }

    [Fact]
    public void InitialState_AllRegistersAreZero()
    {
        var (emulator, cpu) = CreateEmulator();

        for (var i = 0; i < 16; i++)
        {
            Assert.Equal(0, cpu.Registers.ReadV(i));
        }
    }

    [Fact]
    public void InitialState_MemoryIsZeroOutsideFontRegion()
    {
        var (emulator, cpu) = CreateEmulator();

        for (var i = 0; i < 4096; i++)
        {
            if (i >= 0x050 && i < 0x050 + 80) continue;   // low-res font (16 glyphs * 5 bytes)
            if (i >= 0x0A0 && i < 0x0A0 + 100) continue;  // high-res font (10 glyphs * 10 bytes)
            Assert.Equal(0, emulator.Memory.Read(i));
        }
    }

    [Fact]
    public void InitialState_FontLoadedAt0x050()
    {
        var (emulator, cpu) = CreateEmulator();

        var zeroSprite = ReadMemorySlice(emulator, 0x050, 5);
        Assert.Equal(new byte[] { 0xF0, 0x90, 0x90, 0x90, 0xF0 }, zeroSprite);
    }

    [Theory]
    [InlineData(0x6000, 0x0, 0x00)]
    [InlineData(0x6142, 0x1, 0x42)]
    [InlineData(0x6AFF, 0xA, 0xFF)]
    [InlineData(0x6F01, 0xF, 0x01)]
    public void SetRegisterValue_StoresNnIntoVx(int instruction, int x, byte expected)
    {
        var (emulator, cpu) = CreateEmulator();

        Chip8Routines.SetRegisterValue(cpu, instruction);

        Assert.Equal(expected, cpu.Registers.ReadV(x));
    }

    [Fact]
    public void SetRegisterValue_DoesNotAffectOtherRegisters()
    {
        var (emulator, cpu) = CreateEmulator();

        Chip8Routines.SetRegisterValue(cpu, 0x6342);

        for (var i = 0; i < 16; i++)
        {
            if (i == 3) continue;
            Assert.Equal(0, cpu.Registers.ReadV(i));
        }
    }

    [Fact]
    public void AddValueToRegister_AddsNnToVx()
    {
        var (emulator, cpu) = CreateEmulator();
        Chip8Routines.SetRegisterValue(cpu, 0x6205);

        Chip8Routines.AddValueToRegister(cpu, 0x7203);

        Assert.Equal(8, cpu.Registers.ReadV(2));
    }

    [Fact]
    public void AddValueToRegister_WrapsOnByteOverflow()
    {
        var (emulator, cpu) = CreateEmulator();
        Chip8Routines.SetRegisterValue(cpu, 0x62FF);

        Chip8Routines.AddValueToRegister(cpu, 0x7202);

        Assert.Equal(0x01, cpu.Registers.ReadV(2));
    }

    [Theory]
    [InlineData(0xA000, 0x000)]
    [InlineData(0xA123, 0x123)]
    [InlineData(0xAFFF, 0xFFF)]
    public void SetIndexRegister_StoresNnn(int instruction, int expected)
    {
        var (emulator, cpu) = CreateEmulator();

        Chip8Routines.SetIndexRegisterIns(cpu, instruction);

        Assert.Equal(expected, cpu.Registers.ReadI());
    }

    [Theory]
    [InlineData(0x1000, 0x000)]
    [InlineData(0x1200, 0x200)]
    [InlineData(0x1FFF, 0xFFF)]
    public void JumpToAddress_SetsProgramCounterToNnn(int instruction, int expected)
    {
        var (emulator, cpu) = CreateEmulator();

        Chip8Routines.JumpToAddress(cpu, instruction);

        Assert.Equal(expected, cpu.ReadProgramCounter());
    }

    [Fact]
    public void SkipIfRegisterEqualsValue_SkipsNextInstruction_WhenEqual()
    {
        var (emulator, cpu) = CreateEmulator();
        Chip8Routines.SetRegisterValue(cpu, 0x6242);
        var pcBefore = cpu.ReadProgramCounter();

        Chip8Routines.SkipNextInsIfRegisterValueEqualsValue(cpu, 0x3242);

        Assert.Equal(pcBefore + 2, cpu.ReadProgramCounter());
    }

    [Fact]
    public void SkipIfRegisterEqualsValue_DoesNotSkip_WhenNotEqual()
    {
        var (emulator, cpu) = CreateEmulator();
        Chip8Routines.SetRegisterValue(cpu, 0x6242);
        var pcBefore = cpu.ReadProgramCounter();

        Chip8Routines.SkipNextInsIfRegisterValueEqualsValue(cpu, 0x3201);

        Assert.Equal(pcBefore, cpu.ReadProgramCounter());
    }

    [Fact]
    public void SkipIfRegisterNotEqualsValue_SkipsNextInstruction_WhenNotEqual()
    {
        var (emulator, cpu) = CreateEmulator();
        Chip8Routines.SetRegisterValue(cpu, 0x6242);
        var pcBefore = cpu.ReadProgramCounter();

        Chip8Routines.SkipNextInsIfRegisterValueNotEqualsValue(cpu, 0x4201);

        Assert.Equal(pcBefore + 2, cpu.ReadProgramCounter());
    }

    [Fact]
    public void SkipIfRegisterNotEqualsValue_DoesNotSkip_WhenEqual()
    {
        var (emulator, cpu) = CreateEmulator();
        Chip8Routines.SetRegisterValue(cpu, 0x6242);
        var pcBefore = cpu.ReadProgramCounter();

        Chip8Routines.SkipNextInsIfRegisterValueNotEqualsValue(cpu, 0x4242);

        Assert.Equal(pcBefore, cpu.ReadProgramCounter());
    }

    [Fact]
    public void SkipIfRegisterEqualsRegister_SkipsNextInstruction_WhenEqual()
    {
        var (emulator, cpu) = CreateEmulator();
        Chip8Routines.SetRegisterValue(cpu, 0x6142);
        Chip8Routines.SetRegisterValue(cpu, 0x6242);
        var pcBefore = cpu.ReadProgramCounter();

        cpu.FiveOpRoutines[0x5120 & 0x000F](cpu, 0x5120);

        Assert.Equal(pcBefore + 2, cpu.ReadProgramCounter());
    }

    [Fact]
    public void SkipIfRegisterEqualsRegister_DoesNotSkip_WhenNotEqual()
    {
        var (emulator, cpu) = CreateEmulator();
        Chip8Routines.SetRegisterValue(cpu, 0x6142);
        Chip8Routines.SetRegisterValue(cpu, 0x6201);
        var pcBefore = cpu.ReadProgramCounter();

        cpu.FiveOpRoutines[0x5120 & 0x000F](cpu, 0x5120);

        Assert.Equal(pcBefore, cpu.ReadProgramCounter());
    }

    [Fact]
    public void CallSubroutine_JumpsToAddressAndPushesReturnAddress()
    {
        var (emulator, cpu) = CreateEmulator();
        Chip8Routines.JumpToAddress(cpu, 0x1246);
        var spBefore = cpu.Stack.StackPointer;

        Chip8Routines.CallSubroutine(cpu, 0x2ABC);

        Assert.Equal(0xABC, cpu.ReadProgramCounter());
        Assert.Equal(spBefore + 1, cpu.Stack.StackPointer);
        Assert.Equal(0x246, cpu.Stack.Pop());
    }

    [Fact]
    public void ReturnFromSubroutine_RestoresProgramCounterFromStack()
    {
        var (emulator, cpu) = CreateEmulator();
        Chip8Routines.JumpToAddress(cpu, 0x1246);
        Chip8Routines.CallSubroutine(cpu, 0x2ABC);

        Chip8Routines.ReturnFromSubroutine(cpu, 0x00EE);

        Assert.Equal(0x246, cpu.ReadProgramCounter());
    }

    [Fact]
    public void SetRegisterValueFromRegister_CopiesVyIntoVx()
    {
        var (emulator, cpu) = CreateEmulator();
        Chip8Routines.SetRegisterValue(cpu, 0x6142);
        Chip8Routines.SetRegisterValue(cpu, 0x62AB);

        Chip8Routines.SetRegisterValueFromRegister(cpu, 0x8120);

        Assert.Equal(0xAB, cpu.Registers.ReadV(1));
        Assert.Equal(0xAB, cpu.Registers.ReadV(2));
    }

    [Fact]
    public void BitwiseOrOnRegisters_StoresVxOrVyIntoVx()
    {
        var (emulator, cpu) = CreateEmulator();
        Chip8Routines.SetRegisterValue(cpu, 0x61F0);
        Chip8Routines.SetRegisterValue(cpu, 0x620F);

        Chip8Routines.BitwiseOrOnRegisters(cpu, 0x8121);

        Assert.Equal(0xFF, cpu.Registers.ReadV(1));
        Assert.Equal(0x0F, cpu.Registers.ReadV(2));
    }

    [Fact]
    public void BitwiseAndOnRegisters_StoresVxAndVyIntoVx()
    {
        var (emulator, cpu) = CreateEmulator();
        Chip8Routines.SetRegisterValue(cpu, 0x61FC);
        Chip8Routines.SetRegisterValue(cpu, 0x620F);

        Chip8Routines.BitwiseAndOnRegisters(cpu, 0x8122);

        Assert.Equal(0x0C, cpu.Registers.ReadV(1));
        Assert.Equal(0x0F, cpu.Registers.ReadV(2));
    }

    [Fact]
    public void XorRegisterValueFromRegister_StoresVxXorVyIntoVx()
    {
        var (emulator, cpu) = CreateEmulator();
        Chip8Routines.SetRegisterValue(cpu, 0x61FC);
        Chip8Routines.SetRegisterValue(cpu, 0x620F);

        Chip8Routines.XorRegisterValueFromRegister(cpu, 0x8123);

        Assert.Equal(0xF3, cpu.Registers.ReadV(1));
        Assert.Equal(0x0F, cpu.Registers.ReadV(2));
    }

    [Fact]
    public void AddValueToRegisterWithCarry_NoOverflow_StoresSumAndClearsVf()
    {
        var (emulator, cpu) = CreateEmulator();
        Chip8Routines.SetRegisterValue(cpu, 0x6105);
        Chip8Routines.SetRegisterValue(cpu, 0x6203);
        Chip8Routines.SetRegisterValue(cpu, 0x6F01);

        Chip8Routines.AddValueToRegisterWithCarry(cpu, 0x8124);

        Assert.Equal(0x08, cpu.Registers.ReadV(1));
        Assert.Equal(0x03, cpu.Registers.ReadV(2));
        Assert.Equal(0, cpu.Registers.ReadV(0xF));
    }

    [Fact]
    public void AddValueToRegisterWithCarry_Overflow_WrapsAndSetsVf()
    {
        var (emulator, cpu) = CreateEmulator();
        Chip8Routines.SetRegisterValue(cpu, 0x61FF);
        Chip8Routines.SetRegisterValue(cpu, 0x6202);

        Chip8Routines.AddValueToRegisterWithCarry(cpu, 0x8124);

        Assert.Equal(0x01, cpu.Registers.ReadV(1));
        Assert.Equal(0x02, cpu.Registers.ReadV(2));
        Assert.Equal(1, cpu.Registers.ReadV(0xF));
    }

    [Fact]
    public void AddValueToRegisterWithCarry_AtBoundary_DoesNotSetVf()
    {
        var (emulator, cpu) = CreateEmulator();
        Chip8Routines.SetRegisterValue(cpu, 0x61F0);
        Chip8Routines.SetRegisterValue(cpu, 0x620F);

        Chip8Routines.AddValueToRegisterWithCarry(cpu, 0x8124);

        Assert.Equal(0xFF, cpu.Registers.ReadV(1));
        Assert.Equal(0, cpu.Registers.ReadV(0xF));
    }

    [Fact]
    public void VxSubVy_NoBorrow_StoresDifferenceAndSetsVf()
    {
        var (emulator, cpu) = CreateEmulator();
        Chip8Routines.SetRegisterValue(cpu, 0x610A);
        Chip8Routines.SetRegisterValue(cpu, 0x6203);

        Chip8Routines.VxSubVy(cpu, 0x8125);

        Assert.Equal(0x07, cpu.Registers.ReadV(1));
        Assert.Equal(0x03, cpu.Registers.ReadV(2));
        Assert.Equal(1, cpu.Registers.ReadV(0xF));
    }

    [Fact]
    public void VxSubVy_Borrow_WrapsAndClearsVf()
    {
        var (emulator, cpu) = CreateEmulator();
        Chip8Routines.SetRegisterValue(cpu, 0x6103);
        Chip8Routines.SetRegisterValue(cpu, 0x620A);

        Chip8Routines.VxSubVy(cpu, 0x8125);

        Assert.Equal(0xF9, cpu.Registers.ReadV(1));
        Assert.Equal(0x0A, cpu.Registers.ReadV(2));
        Assert.Equal(0, cpu.Registers.ReadV(0xF));
    }

    [Fact]
    public void VxSubVy_WhenEqual_SetsVf()
    {
        var (emulator, cpu) = CreateEmulator();
        Chip8Routines.SetRegisterValue(cpu, 0x6142);
        Chip8Routines.SetRegisterValue(cpu, 0x6242);

        Chip8Routines.VxSubVy(cpu, 0x8125);

        Assert.Equal(0x00, cpu.Registers.ReadV(1));
        Assert.Equal(1, cpu.Registers.ReadV(0xF));
    }

    [Fact]
    public void VySubVx_NoBorrow_StoresDifferenceAndSetsVf()
    {
        var (emulator, cpu) = CreateEmulator();
        Chip8Routines.SetRegisterValue(cpu, 0x6103);
        Chip8Routines.SetRegisterValue(cpu, 0x620A);

        Chip8Routines.VySubVx(cpu, 0x8127);

        Assert.Equal(0x07, cpu.Registers.ReadV(1));
        Assert.Equal(0x0A, cpu.Registers.ReadV(2));
        Assert.Equal(1, cpu.Registers.ReadV(0xF));
    }

    [Fact]
    public void VySubVx_Borrow_WrapsAndClearsVf()
    {
        var (emulator, cpu) = CreateEmulator();
        Chip8Routines.SetRegisterValue(cpu, 0x610A);
        Chip8Routines.SetRegisterValue(cpu, 0x6203);

        Chip8Routines.VySubVx(cpu, 0x8127);

        Assert.Equal(0xF9, cpu.Registers.ReadV(1));
        Assert.Equal(0x03, cpu.Registers.ReadV(2));
        Assert.Equal(0, cpu.Registers.ReadV(0xF));
    }

    [Fact]
    public void ShiftRight_EvenValue_ShiftsAndClearsVf()
    {
        var (emulator, cpu) = CreateEmulator();
        Chip8Routines.SetRegisterValue(cpu, 0x6108);

        Chip8Routines.ShiftRight(cpu, 0x8106);

        Assert.Equal(0x04, cpu.Registers.ReadV(1));
        Assert.Equal(0, cpu.Registers.ReadV(0xF));
    }

    [Fact]
    public void ShiftRight_OddValue_ShiftsAndSetsVf()
    {
        var (emulator, cpu) = CreateEmulator();
        Chip8Routines.SetRegisterValue(cpu, 0x6109);

        Chip8Routines.ShiftRight(cpu, 0x8106);

        Assert.Equal(0x04, cpu.Registers.ReadV(1));
        Assert.Equal(1, cpu.Registers.ReadV(0xF));
    }

    [Fact]
    public void ShiftLeft_MsbClear_ShiftsAndClearsVf()
    {
        var (emulator, cpu) = CreateEmulator();
        Chip8Routines.SetRegisterValue(cpu, 0x6141);

        Chip8Routines.ShiftLeft(cpu, 0x810E);

        Assert.Equal(0x82, cpu.Registers.ReadV(1));
        Assert.Equal(0, cpu.Registers.ReadV(0xF));
    }

    [Fact]
    public void ShiftLeft_MsbSet_ShiftsAndSetsVf()
    {
        var (emulator, cpu) = CreateEmulator();
        Chip8Routines.SetRegisterValue(cpu, 0x6181);

        Chip8Routines.ShiftLeft(cpu, 0x810E);

        Assert.Equal(0x02, cpu.Registers.ReadV(1));
        Assert.Equal(1, cpu.Registers.ReadV(0xF));
    }

    [Fact]
    public void SkipIfRegisterNotEqualsRegister_SkipsNextInstruction_WhenNotEqual()
    {
        var (emulator, cpu) = CreateEmulator();
        Chip8Routines.SetRegisterValue(cpu, 0x6142);
        Chip8Routines.SetRegisterValue(cpu, 0x6201);
        var pcBefore = cpu.ReadProgramCounter();

        Chip8Routines.SkipNextInsIfRegisterValueNotEqualsRegisterValue(cpu, 0x9120);

        Assert.Equal(pcBefore + 2, cpu.ReadProgramCounter());
    }

    [Fact]
    public void SkipIfRegisterNotEqualsRegister_DoesNotSkip_WhenEqual()
    {
        var (emulator, cpu) = CreateEmulator();
        Chip8Routines.SetRegisterValue(cpu, 0x6142);
        Chip8Routines.SetRegisterValue(cpu, 0x6242);
        var pcBefore = cpu.ReadProgramCounter();

        Chip8Routines.SkipNextInsIfRegisterValueNotEqualsRegisterValue(cpu, 0x9120);

        Assert.Equal(pcBefore, cpu.ReadProgramCounter());
    }

    [Theory]
    [InlineData(0x8120, 0x05, 0xAA, 0xAA)]
    [InlineData(0x8121, 0xF0, 0x0F, 0xFF)]
    [InlineData(0x8122, 0xFC, 0x0F, 0x0C)]
    [InlineData(0x8123, 0xFC, 0x0F, 0xF3)]
    public void ArithmeticOperation_DispatchesToCorrectOperation(int instruction, byte vx, byte vy, byte expected)
    {
        var (emulator, cpu) = CreateEmulator();
        Chip8Routines.SetRegisterValue(cpu, 0x6100 | vx);
        Chip8Routines.SetRegisterValue(cpu, 0x6200 | vy);

        cpu.ArithmeticRoutines[instruction & 0x000F](cpu, instruction);

        Assert.Equal(expected, cpu.Registers.ReadV(1));
    }

    [Fact]
    public void SkipIfKeyIsPressed_SkipsNextInstruction_WhenPressed()
    {
        var (emulator, cpu) = CreateEmulator(out var input);
        Chip8Routines.SetRegisterValue(cpu, 0x6105);
        input.Press(0x5);
        var pcBefore = cpu.ReadProgramCounter();

        Chip8Routines.SkipNextInsIfKeyIsPressed(cpu, 0xE19E);

        Assert.Equal(pcBefore + 2, cpu.ReadProgramCounter());
    }

    [Fact]
    public void SkipIfKeyIsPressed_DoesNotSkip_WhenNotPressed()
    {
        var (emulator, cpu) = CreateEmulator(out var input);
        Chip8Routines.SetRegisterValue(cpu, 0x6105);
        input.Press(0x3);
        var pcBefore = cpu.ReadProgramCounter();

        Chip8Routines.SkipNextInsIfKeyIsPressed(cpu, 0xE19E);

        Assert.Equal(pcBefore, cpu.ReadProgramCounter());
    }

    [Fact]
    public void SkipIfKeyIsReleased_SkipsNextInstruction_WhenNotPressed()
    {
        var (emulator, cpu) = CreateEmulator(out var input);
        Chip8Routines.SetRegisterValue(cpu, 0x6105);
        input.Press(0x3);
        var pcBefore = cpu.ReadProgramCounter();

        Chip8Routines.SkipNextInsIfKeyIsReleased(cpu, 0xE1A1);

        Assert.Equal(pcBefore + 2, cpu.ReadProgramCounter());
    }

    [Fact]
    public void SkipIfKeyIsReleased_DoesNotSkip_WhenPressed()
    {
        var (emulator, cpu) = CreateEmulator(out var input);
        Chip8Routines.SetRegisterValue(cpu, 0x6105);
        input.Press(0x5);
        var pcBefore = cpu.ReadProgramCounter();

        Chip8Routines.SkipNextInsIfKeyIsReleased(cpu, 0xE1A1);

        Assert.Equal(pcBefore, cpu.ReadProgramCounter());
    }

    [Fact]
    public void SkipIfKeyIsPressedOrReleased_Dispatches9EToIsPressed()
    {
        var (emulator, cpu) = CreateEmulator(out var input);
        Chip8Routines.SetRegisterValue(cpu, 0x6107);
        input.Press(0x7);
        var pcBefore = cpu.ReadProgramCounter();

        cpu.InputRoutines[0xE19E & 0x00FF](cpu, 0xE19E);

        Assert.Equal(pcBefore + 2, cpu.ReadProgramCounter());
    }

    [Fact]
    public void SkipIfKeyIsPressedOrReleased_DispatchesA1ToIsReleased()
    {
        var (emulator, cpu) = CreateEmulator(out var input);
        Chip8Routines.SetRegisterValue(cpu, 0x6107);
        var pcBefore = cpu.ReadProgramCounter();

        cpu.InputRoutines[0xE1A1 & 0x00FF](cpu, 0xE1A1);

        Assert.Equal(pcBefore + 2, cpu.ReadProgramCounter());
    }

    [Fact]
    public void SkipIfKeyIsPressedOrReleased_UnknownSubOp_IsNoOp()
    {
        var (emulator, cpu) = CreateEmulator(out var input);
        Chip8Routines.SetRegisterValue(cpu, 0x6107);
        input.Press(0x7);
        var pcBefore = cpu.ReadProgramCounter();

        cpu.InputRoutines[0xE100 & 0x00FF](cpu, 0xE100);

        Assert.Equal(pcBefore, cpu.ReadProgramCounter());
    }

    [Fact]
    public void SetDelayTimer_StoresVxIntoDelayTimer()
    {
        var (emulator, cpu) = CreateEmulator();
        Chip8Routines.SetRegisterValue(cpu, 0x613C);

        Chip8Routines.SetDelayTimer(cpu, 0xF115);

        Assert.Equal(0x3C, cpu.Registers.ReadDt());
    }

    [Fact]
    public void SetSoundTimer_StoresVxIntoSoundTimer()
    {
        var (emulator, cpu) = CreateEmulator();
        Chip8Routines.SetRegisterValue(cpu, 0x6120);

        Chip8Routines.SetSoundTimer(cpu, 0xF118);

        Assert.Equal(0x20, cpu.Registers.ReadSt());
    }

    [Fact]
    public void ReadDelayTimer_StoresDelayTimerIntoVx()
    {
        var (emulator, cpu) = CreateEmulator();
        Chip8Routines.SetRegisterValue(cpu, 0x612A);
        Chip8Routines.SetDelayTimer(cpu, 0xF115);

        Chip8Routines.ReadDelayTimer(cpu, 0xF207);

        Assert.Equal(0x2A, cpu.Registers.ReadV(2));
    }

    [Fact]
    public void TimerIns_Dispatches07ToReadDelayTimer()
    {
        var (emulator, cpu) = CreateEmulator();
        Chip8Routines.SetRegisterValue(cpu, 0x6155);
        Chip8Routines.SetDelayTimer(cpu, 0xF115);

        cpu.UtilityRoutines[0xF207 & 0x00FF](cpu, 0xF207);

        Assert.Equal(0x55, cpu.Registers.ReadV(2));
    }

    [Fact]
    public void TimerIns_Dispatches15ToSetDelayTimer()
    {
        var (emulator, cpu) = CreateEmulator();
        Chip8Routines.SetRegisterValue(cpu, 0x6199);

        cpu.UtilityRoutines[0xF115 & 0x00FF](cpu, 0xF115);

        Assert.Equal(0x99, cpu.Registers.ReadDt());
    }

    [Fact]
    public void TimerIns_Dispatches18ToSetSoundTimer()
    {
        var (emulator, cpu) = CreateEmulator();
        Chip8Routines.SetRegisterValue(cpu, 0x617F);

        cpu.UtilityRoutines[0xF118 & 0x00FF](cpu, 0xF118);

        Assert.Equal(0x7F, cpu.Registers.ReadSt());
    }

    [Fact]
    public void TimerIns_UnknownSubOp_IsNoOp()
    {
        var (emulator, cpu) = CreateEmulator();
        Chip8Routines.SetRegisterValue(cpu, 0x61AA);
        var pcBefore = cpu.ReadProgramCounter();

        cpu.UtilityRoutines[0xF100 & 0x00FF](cpu, 0xF100);

        Assert.Equal(pcBefore, cpu.ReadProgramCounter());
    }

    [Fact]
    public void AddVxToI_AddsVxToIndexRegister()
    {
        var (emulator, cpu) = CreateEmulator();
        Chip8Routines.SetIndexRegisterIns(cpu, 0xA100);
        Chip8Routines.SetRegisterValue(cpu, 0x6125);

        Chip8Routines.AddVxToI(cpu, 0xF11E);

        Assert.Equal(0x125, cpu.Registers.ReadI());
    }

    [Fact]
    public void AddVxToI_AccumulatesAcrossCalls()
    {
        var (emulator, cpu) = CreateEmulator();
        Chip8Routines.SetIndexRegisterIns(cpu, 0xA010);
        Chip8Routines.SetRegisterValue(cpu, 0x6105);

        Chip8Routines.AddVxToI(cpu, 0xF11E);
        Chip8Routines.AddVxToI(cpu, 0xF11E);

        Assert.Equal(0x01A, cpu.Registers.ReadI());
    }

    [Theory]
    [InlineData(0x0, 0x050)]
    [InlineData(0x1, 0x055)]
    [InlineData(0x9, 0x07D)]
    [InlineData(0xA, 0x082)]
    [InlineData(0xF, 0x09B)]
    public void LoadFontCharacter_SetsIndexToFontBasePlus5TimesVx(byte vx, int expectedIndex)
    {
        var (emulator, cpu) = CreateEmulator();
        Chip8Routines.SetRegisterValue(cpu, 0x6100 | vx);

        Chip8Routines.LoadLowResFontCharacter(cpu, 0xF129);

        Assert.Equal(expectedIndex, cpu.Registers.ReadI());
    }

    [Fact]
    public void LoadFontCharacter_IndexPointsAtFontSpriteData()
    {
        var (emulator, cpu) = CreateEmulator();
        Chip8Routines.SetRegisterValue(cpu, 0x6100);

        Chip8Routines.LoadLowResFontCharacter(cpu, 0xF129);

        var sprite = ReadMemorySlice(emulator, cpu.Registers.ReadI(), 5);
        Assert.Equal(new byte[] { 0xF0, 0x90, 0x90, 0x90, 0xF0 }, sprite);
    }

    [Fact]
    public void WaitForKeyPress_SetsIsWaitingForKeyPressFlag()
    {
        var (emulator, cpu) = CreateEmulator();

        Chip8Routines.WaitForKeyPressAndRelease(cpu, 0xF20A);

        Assert.True(emulator.IsWaitingForKey);
    }

    [Fact]
    public void WaitForKeyPress_DoesNotImmediatelyWriteToRegister()
    {
        var (emulator, cpu) = CreateEmulator();

        Chip8Routines.WaitForKeyPressAndRelease(cpu, 0xF20A);

        Assert.Equal(0, cpu.Registers.ReadV(2));
    }

    [Fact]
    public void Update_WhileWaitingForKey_AndNoKeyPressed_StaysWaiting()
    {
        var (emulator, cpu) = CreateEmulator(out var clock, out _);
        emulator.Start();
        Chip8Routines.WaitForKeyPressAndRelease(cpu, 0xF20A);
        clock.Timestamp = clock.Frequency / 60;

        clock.Tick();

        Assert.True(emulator.IsWaitingForKey);
        Assert.Equal(0, cpu.Registers.ReadV(2));
    }

    [Fact]
    public void Update_WhileWaitingForKey_AndKeyPressed_StoresKeyAndResumes()
    {
        var (emulator, cpu) = CreateEmulator(out var clock, out var input);
        emulator.Start();
        emulator.Memory.Write(0, [0x10, 0x00]);
        Chip8Routines.WaitForKeyPressAndRelease(cpu, 0xF20A);
        input.QueueKeyPressAndReleaseEvent(0xA);
        clock.Timestamp = clock.Frequency / 60;

        clock.Tick();

        Assert.False(emulator.IsWaitingForKey);
        Assert.Equal(0xA, cpu.Registers.ReadV(2));
    }

    [Fact]
    public void Update_WhileWaitingForKey_DelayTimerStillTicks()
    {
        var (emulator, cpu) = CreateEmulator(out var clock, out _);
        emulator.Start();
        Chip8Routines.SetRegisterValue(cpu, 0x600A);
        Chip8Routines.SetDelayTimer(cpu, 0xF015);
        Chip8Routines.WaitForKeyPressAndRelease(cpu, 0xF10A);
        clock.Timestamp = clock.Frequency / 60;

        clock.Tick();

        Assert.Equal(9, cpu.Registers.ReadDt());
        Assert.True(emulator.IsWaitingForKey);
    }

    [Fact]
    public void Update_WhileWaitingForKey_SoundTimerStillTicks()
    {
        var (emulator, cpu) = CreateEmulator(out var clock, out _);
        emulator.Start();
        Chip8Routines.SetRegisterValue(cpu, 0x6005);
        Chip8Routines.SetSoundTimer(cpu, 0xF018);
        Chip8Routines.WaitForKeyPressAndRelease(cpu, 0xF10A);
        clock.Timestamp = clock.Frequency / 60;

        clock.Tick();

        Assert.Equal(4, cpu.Registers.ReadSt());
    }

    [Fact]
    public void TimerIns_Dispatches0AToWaitForKeyPress()
    {
        var (emulator, cpu) = CreateEmulator();

        cpu.UtilityRoutines[0xF10A & 0x00FF](cpu, 0xF10A);

        Assert.True(emulator.IsWaitingForKey);
    }

    [Fact]
    public void TimerIns_Dispatches1EToAddVxToI()
    {
        var (emulator, cpu) = CreateEmulator();
        Chip8Routines.SetIndexRegisterIns(cpu, 0xA020);
        Chip8Routines.SetRegisterValue(cpu, 0x6103);

        cpu.UtilityRoutines[0xF11E & 0x00FF](cpu, 0xF11E);

        Assert.Equal(0x023, cpu.Registers.ReadI());
    }

    [Fact]
    public void TimerIns_Dispatches29ToLoadFontCharacter()
    {
        var (emulator, cpu) = CreateEmulator();
        Chip8Routines.SetRegisterValue(cpu, 0x6103);

        cpu.UtilityRoutines[0xF129 & 0x00FF](cpu, 0xF129);

        Assert.Equal(0x050 + 5 * 3, cpu.Registers.ReadI());
    }

    [Fact]
    public void StoreRegisters_WritesV0ThroughVxIntoMemoryAtI()
    {
        var (emulator, cpu) = CreateEmulator();
        Chip8Routines.SetRegisterValue(cpu, 0x6011);
        Chip8Routines.SetRegisterValue(cpu, 0x6122);
        Chip8Routines.SetRegisterValue(cpu, 0x6233);
        Chip8Routines.SetRegisterValue(cpu, 0x6344);
        Chip8Routines.SetIndexRegisterIns(cpu, 0xA300);

        Chip8Routines.StoreRegisters(cpu, 0xF355);

        Assert.Equal(0x11, emulator.Memory.Read(0x300));
        Assert.Equal(0x22, emulator.Memory.Read(0x301));
        Assert.Equal(0x33, emulator.Memory.Read(0x302));
        Assert.Equal(0x44, emulator.Memory.Read(0x303));
        Assert.Equal(0x00, emulator.Memory.Read(0x304));
    }

    [Fact]
    public void StoreRegisters_V0Only_WritesSingleByte()
    {
        var (emulator, cpu) = CreateEmulator();
        Chip8Routines.SetRegisterValue(cpu, 0x60AB);
        Chip8Routines.SetRegisterValue(cpu, 0x61CD);
        Chip8Routines.SetIndexRegisterIns(cpu, 0xA300);

        Chip8Routines.StoreRegisters(cpu, 0xF055);

        Assert.Equal(0xAB, emulator.Memory.Read(0x300));
        Assert.Equal(0x00, emulator.Memory.Read(0x301));
    }

    [Fact]
    public void LoadRegisters_ReadsMemoryAtIIntoV0ThroughVx()
    {
        var (emulator, cpu) = CreateEmulator();
        Chip8Routines.SetIndexRegisterIns(cpu, 0xA300);
        emulator.Memory.Write(0x300, [0x11, 0x22, 0x33, 0x44, 0xFF]);

        Chip8Routines.LoadRegisters(cpu, 0xF365);

        Assert.Equal(0x11, cpu.Registers.ReadV(0));
        Assert.Equal(0x22, cpu.Registers.ReadV(1));
        Assert.Equal(0x33, cpu.Registers.ReadV(2));
        Assert.Equal(0x44, cpu.Registers.ReadV(3));
        Assert.Equal(0x00, cpu.Registers.ReadV(4));
    }

    [Fact]
    public void LoadRegisters_V0Only_ReadsSingleByte()
    {
        var (emulator, cpu) = CreateEmulator();
        Chip8Routines.SetIndexRegisterIns(cpu, 0xA300);
        emulator.Memory.Write(0x300, [0xAB, 0xCD]);

        Chip8Routines.LoadRegisters(cpu, 0xF065);

        Assert.Equal(0xAB, cpu.Registers.ReadV(0));
        Assert.Equal(0x00, cpu.Registers.ReadV(1));
    }

    [Fact]
    public void StoreThenLoad_RoundTripsRegisters()
    {
        var (emulator, cpu) = CreateEmulator();
        Chip8Routines.SetRegisterValue(cpu, 0x6012);
        Chip8Routines.SetRegisterValue(cpu, 0x6134);
        Chip8Routines.SetRegisterValue(cpu, 0x6256);
        Chip8Routines.SetIndexRegisterIns(cpu, 0xA200);
        Chip8Routines.StoreRegisters(cpu, 0xF255);

        Chip8Routines.SetRegisterValue(cpu, 0x6000);
        Chip8Routines.SetRegisterValue(cpu, 0x6100);
        Chip8Routines.SetRegisterValue(cpu, 0x6200);
        Chip8Routines.LoadRegisters(cpu, 0xF265);

        Assert.Equal(0x12, cpu.Registers.ReadV(0));
        Assert.Equal(0x34, cpu.Registers.ReadV(1));
        Assert.Equal(0x56, cpu.Registers.ReadV(2));
    }

    [Fact]
    public void TimerIns_Dispatches55ToStoreRegisters()
    {
        var (emulator, cpu) = CreateEmulator();
        Chip8Routines.SetRegisterValue(cpu, 0x6099);
        Chip8Routines.SetIndexRegisterIns(cpu, 0xA400);

        cpu.UtilityRoutines[0xF055 & 0x00FF](cpu, 0xF055);

        Assert.Equal(0x99, emulator.Memory.Read(0x400));
    }

    [Theory]
    [InlineData(0, 0, 0, 0)]
    [InlineData(7, 0, 0, 7)]
    [InlineData(42, 0, 4, 2)]
    [InlineData(100, 1, 0, 0)]
    [InlineData(123, 1, 2, 3)]
    [InlineData(255, 2, 5, 5)]
    public void StoreBcdInMemory_WritesHundredsTensOnesToMemoryAtI(byte vx, byte hundreds, byte tens, byte ones)
    {
        var (emulator, cpu) = CreateEmulator();
        Chip8Routines.SetRegisterValue(cpu, 0x6100 | vx);
        Chip8Routines.SetIndexRegisterIns(cpu, 0xA300);

        Chip8Routines.StoreBcdInMemory(cpu, 0xF133);

        Assert.Equal(hundreds, emulator.Memory.Read(0x300));
        Assert.Equal(tens, emulator.Memory.Read(0x301));
        Assert.Equal(ones, emulator.Memory.Read(0x302));
    }

    [Fact]
    public void StoreBcdInMemory_DoesNotModifyRegisterOrIndex()
    {
        var (emulator, cpu) = CreateEmulator();
        Chip8Routines.SetRegisterValue(cpu, 0x617B);
        Chip8Routines.SetIndexRegisterIns(cpu, 0xA300);

        Chip8Routines.StoreBcdInMemory(cpu, 0xF133);

        Assert.Equal(0x7B, cpu.Registers.ReadV(1));
        Assert.Equal(0x300, cpu.Registers.ReadI());
    }

    [Fact]
    public void TimerIns_Dispatches33ToStoreBcdInMemory()
    {
        var (emulator, cpu) = CreateEmulator();
        Chip8Routines.SetRegisterValue(cpu, 0x61C8);
        Chip8Routines.SetIndexRegisterIns(cpu, 0xA400);

        cpu.UtilityRoutines[0xF133 & 0x00FF](cpu, 0xF133);

        Assert.Equal(2, emulator.Memory.Read(0x400));
        Assert.Equal(0, emulator.Memory.Read(0x401));
        Assert.Equal(0, emulator.Memory.Read(0x402));
    }

    [Fact]
    public void TimerIns_Dispatches65ToLoadRegisters()
    {
        var (emulator, cpu) = CreateEmulator();
        Chip8Routines.SetIndexRegisterIns(cpu, 0xA400);
        emulator.Memory.Write(0x400, [0x77]);

        cpu.UtilityRoutines[0xF065 & 0x00FF](cpu, 0xF065);

        Assert.Equal(0x77, cpu.Registers.ReadV(0));
    }

    [Fact]
    public void ClearDisplay_ZerosAllDisplayPixels()
    {
        var (emulator, cpu) = CreateEmulator();
        Chip8Routines.SetIndexRegisterIns(cpu, 0xA000);
        emulator.Memory.Write(0, [0xFF]);
        Chip8Routines.DrawToScreen(cpu, 0xD001);
        Assert.Contains(_pixelBuffer, p => p == 1);

        Chip8Routines.ClearDisplay(cpu, 0x00E0);

        foreach (var p in _pixelBuffer)
            Assert.Equal(0, p);
    }

}
