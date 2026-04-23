using Chip8Emulator.Core.Cpu;
using Chip8Emulator.Core.Tests.Fakes;

namespace Chip8Emulator.Core.Tests;

public class Chip8MachineTests
{
    private static Chip8Machine CreateEmulator(out FakeRenderer renderer, out FakeAudio audio, out FakeClock clock, out FakeInput input)
    {
        renderer = new FakeRenderer();
        audio = new FakeAudio();
        clock = new FakeClock();
        input = new FakeInput();
        return new Chip8Machine(renderer, audio, clock, input);
    }

    private static Chip8Machine CreateEmulator() => CreateEmulator(out _, out _, out _, out _);
    private static Chip8Machine CreateEmulator(out FakeInput input) => CreateEmulator(out _, out _, out _, out input);
    private static Chip8Machine CreateEmulator(out FakeClock clock, out FakeInput input) => CreateEmulator(out _, out _, out clock, out input);

    [Fact]
    public void InitialState_IsZeroed()
    {
        var emulator = CreateEmulator();

        Assert.Equal(0, emulator.Debugger.ProgramCounter);
        Assert.Equal(0, emulator.Debugger.IndexRegister);
        Assert.Equal(0, emulator.Debugger.DelayTimer);
        Assert.Equal(0, emulator.Debugger.SoundTimer);
        Assert.Equal(64 * 1024, emulator.Debugger.Memory.Length);
    }

    [Fact]
    public void InitialState_AllRegistersAreZero()
    {
        var emulator = CreateEmulator();

        for (var i = 0; i < 16; i++)
        {
            Assert.Equal(0, emulator.Debugger.Registers[i]);
        }
    }

    [Fact]
    public void InitialState_MemoryIsZeroOutsideFontRegion()
    {
        var emulator = CreateEmulator();

        for (var i = 0; i < emulator.Debugger.Memory.Length; i++)
        {
            if (i >= 0x050 && i < 0x050 + 80) continue;   // low-res font (16 glyphs * 5 bytes)
            if (i >= 0x0A0 && i < 0x0A0 + 100) continue;  // high-res font (10 glyphs * 10 bytes)
            Assert.Equal(0, emulator.Debugger.Memory[i]);
        }
    }

    [Fact]
    public void InitialState_FontLoadedAt0x050()
    {
        var emulator = CreateEmulator();

        var zeroSprite = emulator.Debugger.Memory.Slice(0x050, 5);
        Assert.Equal(new byte[] { 0xF0, 0x90, 0x90, 0x90, 0xF0 }, zeroSprite.ToArray());
    }

    [Theory]
    [InlineData(0x6000, 0x0, 0x00)]
    [InlineData(0x6142, 0x1, 0x42)]
    [InlineData(0x6AFF, 0xA, 0xFF)]
    [InlineData(0x6F01, 0xF, 0x01)]
    public void SetRegisterValue_StoresNnIntoVx(int instruction, int x, byte expected)
    {
        var emulator = CreateEmulator();

        Chip8InstructionSet.SetRegisterValue(emulator, instruction);

        Assert.Equal(expected, emulator.Debugger.Registers[x]);
    }

    [Fact]
    public void SetRegisterValue_DoesNotAffectOtherRegisters()
    {
        var emulator = CreateEmulator();

        Chip8InstructionSet.SetRegisterValue(emulator, 0x6342);

        for (var i = 0; i < 16; i++)
        {
            if (i == 3) continue;
            Assert.Equal(0, emulator.Debugger.Registers[i]);
        }
    }

    [Fact]
    public void AddValueToRegister_AddsNnToVx()
    {
        var emulator = CreateEmulator();
        Chip8InstructionSet.SetRegisterValue(emulator, 0x6205);

        Chip8InstructionSet.AddValueToRegister(emulator, 0x7203);

        Assert.Equal(8, emulator.Debugger.Registers[2]);
    }

    [Fact]
    public void AddValueToRegister_WrapsOnByteOverflow()
    {
        var emulator = CreateEmulator();
        Chip8InstructionSet.SetRegisterValue(emulator, 0x62FF);

        Chip8InstructionSet.AddValueToRegister(emulator, 0x7202);

        Assert.Equal(0x01, emulator.Debugger.Registers[2]);
    }

    [Theory]
    [InlineData(0xA000, 0x000)]
    [InlineData(0xA123, 0x123)]
    [InlineData(0xAFFF, 0xFFF)]
    public void SetIndexRegister_StoresNnn(int instruction, int expected)
    {
        var emulator = CreateEmulator();

        Chip8InstructionSet.SetIndexRegisterIns(emulator, instruction);

        Assert.Equal(expected, emulator.Debugger.IndexRegister);
    }

    [Theory]
    [InlineData(0x1000, 0x000)]
    [InlineData(0x1200, 0x200)]
    [InlineData(0x1FFF, 0xFFF)]
    public void JumpToAddress_SetsProgramCounterToNnn(int instruction, int expected)
    {
        var emulator = CreateEmulator();

        Chip8InstructionSet.JumpToAddress(emulator, instruction);

        Assert.Equal(expected, emulator.Debugger.ProgramCounter);
    }

    [Fact]
    public void SkipIfRegisterEqualsValue_SkipsNextInstruction_WhenEqual()
    {
        var emulator = CreateEmulator();
        Chip8InstructionSet.SetRegisterValue(emulator, 0x6242);
        var pcBefore = emulator.Debugger.ProgramCounter;

        Chip8InstructionSet.SkipNextInsIfRegisterValueEqualsValue(emulator, 0x3242);

        Assert.Equal(pcBefore + 2, emulator.Debugger.ProgramCounter);
    }

    [Fact]
    public void SkipIfRegisterEqualsValue_DoesNotSkip_WhenNotEqual()
    {
        var emulator = CreateEmulator();
        Chip8InstructionSet.SetRegisterValue(emulator, 0x6242);
        var pcBefore = emulator.Debugger.ProgramCounter;

        Chip8InstructionSet.SkipNextInsIfRegisterValueEqualsValue(emulator, 0x3201);

        Assert.Equal(pcBefore, emulator.Debugger.ProgramCounter);
    }

    [Fact]
    public void SkipIfRegisterNotEqualsValue_SkipsNextInstruction_WhenNotEqual()
    {
        var emulator = CreateEmulator();
        Chip8InstructionSet.SetRegisterValue(emulator, 0x6242);
        var pcBefore = emulator.Debugger.ProgramCounter;

        Chip8InstructionSet.SkipNextInsIfRegisterValueNotEqualsValue(emulator, 0x4201);

        Assert.Equal(pcBefore + 2, emulator.Debugger.ProgramCounter);
    }

    [Fact]
    public void SkipIfRegisterNotEqualsValue_DoesNotSkip_WhenEqual()
    {
        var emulator = CreateEmulator();
        Chip8InstructionSet.SetRegisterValue(emulator, 0x6242);
        var pcBefore = emulator.Debugger.ProgramCounter;

        Chip8InstructionSet.SkipNextInsIfRegisterValueNotEqualsValue(emulator, 0x4242);

        Assert.Equal(pcBefore, emulator.Debugger.ProgramCounter);
    }

    [Fact]
    public void SkipIfRegisterEqualsRegister_SkipsNextInstruction_WhenEqual()
    {
        var emulator = CreateEmulator();
        Chip8InstructionSet.SetRegisterValue(emulator, 0x6142);
        Chip8InstructionSet.SetRegisterValue(emulator, 0x6242);
        var pcBefore = emulator.Debugger.ProgramCounter;

        Chip8InstructionSet.SkipNextInsIfRegisterValueEqualsRegisterValue(emulator, 0x5120);

        Assert.Equal(pcBefore + 2, emulator.Debugger.ProgramCounter);
    }

    [Fact]
    public void SkipIfRegisterEqualsRegister_DoesNotSkip_WhenNotEqual()
    {
        var emulator = CreateEmulator();
        Chip8InstructionSet.SetRegisterValue(emulator, 0x6142);
        Chip8InstructionSet.SetRegisterValue(emulator, 0x6201);
        var pcBefore = emulator.Debugger.ProgramCounter;

        Chip8InstructionSet.SkipNextInsIfRegisterValueEqualsRegisterValue(emulator, 0x5120);

        Assert.Equal(pcBefore, emulator.Debugger.ProgramCounter);
    }

    [Fact]
    public void CallSubroutine_JumpsToAddressAndPushesReturnAddress()
    {
        var emulator = CreateEmulator();
        Chip8InstructionSet.JumpToAddress(emulator, 0x1246);
        var spBefore = emulator.Debugger.StackPointer;

        Chip8InstructionSet.CallSubroutine(emulator, 0x2ABC);

        Assert.Equal(0xABC, emulator.Debugger.ProgramCounter);
        Assert.Equal(spBefore + 1, emulator.Debugger.StackPointer);
        Assert.Equal(0x246, emulator.Debugger.Stack[emulator.Debugger.StackPointer]);
    }

    [Fact]
    public void ReturnFromSubroutine_RestoresProgramCounterFromStack()
    {
        var emulator = CreateEmulator();
        Chip8InstructionSet.JumpToAddress(emulator, 0x1246);
        Chip8InstructionSet.CallSubroutine(emulator, 0x2ABC);

        Chip8InstructionSet.ReturnFromSubroutine(emulator, 0x00EE);

        Assert.Equal(0x246, emulator.Debugger.ProgramCounter);
    }

    [Fact]
    public void SetRegisterValueFromRegister_CopiesVyIntoVx()
    {
        var emulator = CreateEmulator();
        Chip8InstructionSet.SetRegisterValue(emulator, 0x6142);
        Chip8InstructionSet.SetRegisterValue(emulator, 0x62AB);

        Chip8InstructionSet.ExecuteSetRegisterValueFromRegisterIns(emulator, 0x8120);

        Assert.Equal(0xAB, emulator.Debugger.Registers[1]);
        Assert.Equal(0xAB, emulator.Debugger.Registers[2]);
    }

    [Fact]
    public void BitwiseOrOnRegisters_StoresVxOrVyIntoVx()
    {
        var emulator = CreateEmulator();
        Chip8InstructionSet.SetRegisterValue(emulator, 0x61F0);
        Chip8InstructionSet.SetRegisterValue(emulator, 0x620F);

        Chip8InstructionSet.ExecuteBitwiseOrOnRegistersIns(emulator, 0x8121);

        Assert.Equal(0xFF, emulator.Debugger.Registers[1]);
        Assert.Equal(0x0F, emulator.Debugger.Registers[2]);
    }

    [Fact]
    public void BitwiseAndOnRegisters_StoresVxAndVyIntoVx()
    {
        var emulator = CreateEmulator();
        Chip8InstructionSet.SetRegisterValue(emulator, 0x61FC);
        Chip8InstructionSet.SetRegisterValue(emulator, 0x620F);

        Chip8InstructionSet.ExecuteBitwiseAndOnRegistersIns(emulator, 0x8122);

        Assert.Equal(0x0C, emulator.Debugger.Registers[1]);
        Assert.Equal(0x0F, emulator.Debugger.Registers[2]);
    }

    [Fact]
    public void XorRegisterValueFromRegister_StoresVxXorVyIntoVx()
    {
        var emulator = CreateEmulator();
        Chip8InstructionSet.SetRegisterValue(emulator, 0x61FC);
        Chip8InstructionSet.SetRegisterValue(emulator, 0x620F);

        Chip8InstructionSet.ExecuteXorRegisterValueFromRegisterIns(emulator, 0x8123);

        Assert.Equal(0xF3, emulator.Debugger.Registers[1]);
        Assert.Equal(0x0F, emulator.Debugger.Registers[2]);
    }

    [Fact]
    public void AddValueToRegisterWithCarry_NoOverflow_StoresSumAndClearsVf()
    {
        var emulator = CreateEmulator();
        Chip8InstructionSet.SetRegisterValue(emulator, 0x6105);
        Chip8InstructionSet.SetRegisterValue(emulator, 0x6203);
        Chip8InstructionSet.SetRegisterValue(emulator, 0x6F01);

        Chip8InstructionSet.ExecuteAddValueToRegisterWithCarryIns(emulator, 0x8124);

        Assert.Equal(0x08, emulator.Debugger.Registers[1]);
        Assert.Equal(0x03, emulator.Debugger.Registers[2]);
        Assert.Equal(0, emulator.Debugger.Registers[0xF]);
    }

    [Fact]
    public void AddValueToRegisterWithCarry_Overflow_WrapsAndSetsVf()
    {
        var emulator = CreateEmulator();
        Chip8InstructionSet.SetRegisterValue(emulator, 0x61FF);
        Chip8InstructionSet.SetRegisterValue(emulator, 0x6202);

        Chip8InstructionSet.ExecuteAddValueToRegisterWithCarryIns(emulator, 0x8124);

        Assert.Equal(0x01, emulator.Debugger.Registers[1]);
        Assert.Equal(0x02, emulator.Debugger.Registers[2]);
        Assert.Equal(1, emulator.Debugger.Registers[0xF]);
    }

    [Fact]
    public void AddValueToRegisterWithCarry_AtBoundary_DoesNotSetVf()
    {
        var emulator = CreateEmulator();
        Chip8InstructionSet.SetRegisterValue(emulator, 0x61F0);
        Chip8InstructionSet.SetRegisterValue(emulator, 0x620F);

        Chip8InstructionSet.ExecuteAddValueToRegisterWithCarryIns(emulator, 0x8124);

        Assert.Equal(0xFF, emulator.Debugger.Registers[1]);
        Assert.Equal(0, emulator.Debugger.Registers[0xF]);
    }

    [Fact]
    public void VxSubVy_NoBorrow_StoresDifferenceAndSetsVf()
    {
        var emulator = CreateEmulator();
        Chip8InstructionSet.SetRegisterValue(emulator, 0x610A);
        Chip8InstructionSet.SetRegisterValue(emulator, 0x6203);

        Chip8InstructionSet.ExecuteVxSubVyIns(emulator, 0x8125);

        Assert.Equal(0x07, emulator.Debugger.Registers[1]);
        Assert.Equal(0x03, emulator.Debugger.Registers[2]);
        Assert.Equal(1, emulator.Debugger.Registers[0xF]);
    }

    [Fact]
    public void VxSubVy_Borrow_WrapsAndClearsVf()
    {
        var emulator = CreateEmulator();
        Chip8InstructionSet.SetRegisterValue(emulator, 0x6103);
        Chip8InstructionSet.SetRegisterValue(emulator, 0x620A);

        Chip8InstructionSet.ExecuteVxSubVyIns(emulator, 0x8125);

        Assert.Equal(0xF9, emulator.Debugger.Registers[1]);
        Assert.Equal(0x0A, emulator.Debugger.Registers[2]);
        Assert.Equal(0, emulator.Debugger.Registers[0xF]);
    }

    [Fact]
    public void VxSubVy_WhenEqual_SetsVf()
    {
        var emulator = CreateEmulator();
        Chip8InstructionSet.SetRegisterValue(emulator, 0x6142);
        Chip8InstructionSet.SetRegisterValue(emulator, 0x6242);

        Chip8InstructionSet.ExecuteVxSubVyIns(emulator, 0x8125);

        Assert.Equal(0x00, emulator.Debugger.Registers[1]);
        Assert.Equal(1, emulator.Debugger.Registers[0xF]);
    }

    [Fact]
    public void VySubVx_NoBorrow_StoresDifferenceAndSetsVf()
    {
        var emulator = CreateEmulator();
        Chip8InstructionSet.SetRegisterValue(emulator, 0x6103);
        Chip8InstructionSet.SetRegisterValue(emulator, 0x620A);

        Chip8InstructionSet.ExecuteVySubVxIns(emulator, 0x8127);

        Assert.Equal(0x07, emulator.Debugger.Registers[1]);
        Assert.Equal(0x0A, emulator.Debugger.Registers[2]);
        Assert.Equal(1, emulator.Debugger.Registers[0xF]);
    }

    [Fact]
    public void VySubVx_Borrow_WrapsAndClearsVf()
    {
        var emulator = CreateEmulator();
        Chip8InstructionSet.SetRegisterValue(emulator, 0x610A);
        Chip8InstructionSet.SetRegisterValue(emulator, 0x6203);

        Chip8InstructionSet.ExecuteVySubVxIns(emulator, 0x8127);

        Assert.Equal(0xF9, emulator.Debugger.Registers[1]);
        Assert.Equal(0x03, emulator.Debugger.Registers[2]);
        Assert.Equal(0, emulator.Debugger.Registers[0xF]);
    }

    [Fact]
    public void ShiftRight_EvenValue_ShiftsAndClearsVf()
    {
        var emulator = CreateEmulator();
        Chip8InstructionSet.SetRegisterValue(emulator, 0x6108);

        Chip8InstructionSet.ExecuteShiftRightIns(emulator, 0x8106);

        Assert.Equal(0x04, emulator.Debugger.Registers[1]);
        Assert.Equal(0, emulator.Debugger.Registers[0xF]);
    }

    [Fact]
    public void ShiftRight_OddValue_ShiftsAndSetsVf()
    {
        var emulator = CreateEmulator();
        Chip8InstructionSet.SetRegisterValue(emulator, 0x6109);

        Chip8InstructionSet.ExecuteShiftRightIns(emulator, 0x8106);

        Assert.Equal(0x04, emulator.Debugger.Registers[1]);
        Assert.Equal(1, emulator.Debugger.Registers[0xF]);
    }

    [Fact]
    public void ShiftLeft_MsbClear_ShiftsAndClearsVf()
    {
        var emulator = CreateEmulator();
        Chip8InstructionSet.SetRegisterValue(emulator, 0x6141);

        Chip8InstructionSet.ExecuteShiftLeftIns(emulator, 0x810E);

        Assert.Equal(0x82, emulator.Debugger.Registers[1]);
        Assert.Equal(0, emulator.Debugger.Registers[0xF]);
    }

    [Fact]
    public void ShiftLeft_MsbSet_ShiftsAndSetsVf()
    {
        var emulator = CreateEmulator();
        Chip8InstructionSet.SetRegisterValue(emulator, 0x6181);

        Chip8InstructionSet.ExecuteShiftLeftIns(emulator, 0x810E);

        Assert.Equal(0x02, emulator.Debugger.Registers[1]);
        Assert.Equal(1, emulator.Debugger.Registers[0xF]);
    }

    [Fact]
    public void SkipIfRegisterNotEqualsRegister_SkipsNextInstruction_WhenNotEqual()
    {
        var emulator = CreateEmulator();
        Chip8InstructionSet.SetRegisterValue(emulator, 0x6142);
        Chip8InstructionSet.SetRegisterValue(emulator, 0x6201);
        var pcBefore = emulator.Debugger.ProgramCounter;

        Chip8InstructionSet.SkipNextInsIfRegisterValueNotEqualsRegisterValue(emulator, 0x9120);

        Assert.Equal(pcBefore + 2, emulator.Debugger.ProgramCounter);
    }

    [Fact]
    public void SkipIfRegisterNotEqualsRegister_DoesNotSkip_WhenEqual()
    {
        var emulator = CreateEmulator();
        Chip8InstructionSet.SetRegisterValue(emulator, 0x6142);
        Chip8InstructionSet.SetRegisterValue(emulator, 0x6242);
        var pcBefore = emulator.Debugger.ProgramCounter;

        Chip8InstructionSet.SkipNextInsIfRegisterValueNotEqualsRegisterValue(emulator, 0x9120);

        Assert.Equal(pcBefore, emulator.Debugger.ProgramCounter);
    }

    [Theory]
    [InlineData(0x8120, 0x05, 0xAA, 0xAA)]
    [InlineData(0x8121, 0xF0, 0x0F, 0xFF)]
    [InlineData(0x8122, 0xFC, 0x0F, 0x0C)]
    [InlineData(0x8123, 0xFC, 0x0F, 0xF3)]
    public void ArithmeticOperation_DispatchesToCorrectOperation(int instruction, byte vx, byte vy, byte expected)
    {
        var emulator = CreateEmulator();
        Chip8InstructionSet.SetRegisterValue(emulator, 0x6100 | vx);
        Chip8InstructionSet.SetRegisterValue(emulator, 0x6200 | vy);

        Chip8InstructionSet.ArithmeticOperation(emulator, instruction);

        Assert.Equal(expected, emulator.Debugger.Registers[1]);
    }

    [Fact]
    public void SkipIfKeyIsPressed_SkipsNextInstruction_WhenPressed()
    {
        var emulator = CreateEmulator(out var input);
        Chip8InstructionSet.SetRegisterValue(emulator, 0x6105);
        input.Press(0x5);
        var pcBefore = emulator.Debugger.ProgramCounter;

        Chip8InstructionSet.SkipNextInsIfKeyIsPressed(emulator, 0xE19E);

        Assert.Equal(pcBefore + 2, emulator.Debugger.ProgramCounter);
    }

    [Fact]
    public void SkipIfKeyIsPressed_DoesNotSkip_WhenNotPressed()
    {
        var emulator = CreateEmulator(out var input);
        Chip8InstructionSet.SetRegisterValue(emulator, 0x6105);
        input.Press(0x3);
        var pcBefore = emulator.Debugger.ProgramCounter;

        Chip8InstructionSet.SkipNextInsIfKeyIsPressed(emulator, 0xE19E);

        Assert.Equal(pcBefore, emulator.Debugger.ProgramCounter);
    }

    [Fact]
    public void SkipIfKeyIsReleased_SkipsNextInstruction_WhenNotPressed()
    {
        var emulator = CreateEmulator(out var input);
        Chip8InstructionSet.SetRegisterValue(emulator, 0x6105);
        input.Press(0x3);
        var pcBefore = emulator.Debugger.ProgramCounter;

        Chip8InstructionSet.SkipNextInsIfKeyIsReleased(emulator, 0xE1A1);

        Assert.Equal(pcBefore + 2, emulator.Debugger.ProgramCounter);
    }

    [Fact]
    public void SkipIfKeyIsReleased_DoesNotSkip_WhenPressed()
    {
        var emulator = CreateEmulator(out var input);
        Chip8InstructionSet.SetRegisterValue(emulator, 0x6105);
        input.Press(0x5);
        var pcBefore = emulator.Debugger.ProgramCounter;

        Chip8InstructionSet.SkipNextInsIfKeyIsReleased(emulator, 0xE1A1);

        Assert.Equal(pcBefore, emulator.Debugger.ProgramCounter);
    }

    [Fact]
    public void SkipIfKeyIsPressedOrReleased_Dispatches9EToIsPressed()
    {
        var emulator = CreateEmulator(out var input);
        Chip8InstructionSet.SetRegisterValue(emulator, 0x6107);
        input.Press(0x7);
        var pcBefore = emulator.Debugger.ProgramCounter;

        Chip8InstructionSet.SkipNextInsIfKeyIsPressedOrReleased(emulator, 0xE19E);

        Assert.Equal(pcBefore + 2, emulator.Debugger.ProgramCounter);
    }

    [Fact]
    public void SkipIfKeyIsPressedOrReleased_DispatchesA1ToIsReleased()
    {
        var emulator = CreateEmulator(out var input);
        Chip8InstructionSet.SetRegisterValue(emulator, 0x6107);
        var pcBefore = emulator.Debugger.ProgramCounter;

        Chip8InstructionSet.SkipNextInsIfKeyIsPressedOrReleased(emulator, 0xE1A1);

        Assert.Equal(pcBefore + 2, emulator.Debugger.ProgramCounter);
    }

    [Fact]
    public void SkipIfKeyIsPressedOrReleased_UnknownSubOp_IsNoOp()
    {
        var emulator = CreateEmulator(out var input);
        Chip8InstructionSet.SetRegisterValue(emulator, 0x6107);
        input.Press(0x7);
        var pcBefore = emulator.Debugger.ProgramCounter;

        Chip8InstructionSet.SkipNextInsIfKeyIsPressedOrReleased(emulator, 0xE100);

        Assert.Equal(pcBefore, emulator.Debugger.ProgramCounter);
    }

    [Fact]
    public void SetDelayTimer_StoresVxIntoDelayTimer()
    {
        var emulator = CreateEmulator();
        Chip8InstructionSet.SetRegisterValue(emulator, 0x613C);

        Chip8InstructionSet.SetDelayTimer(emulator, 0xF115);

        Assert.Equal(0x3C, emulator.Debugger.DelayTimer);
    }

    [Fact]
    public void SetSoundTimer_StoresVxIntoSoundTimer()
    {
        var emulator = CreateEmulator();
        Chip8InstructionSet.SetRegisterValue(emulator, 0x6120);

        Chip8InstructionSet.SetSoundTimer(emulator, 0xF118);

        Assert.Equal(0x20, emulator.Debugger.SoundTimer);
    }

    [Fact]
    public void ReadDelayTimer_StoresDelayTimerIntoVx()
    {
        var emulator = CreateEmulator();
        Chip8InstructionSet.SetRegisterValue(emulator, 0x612A);
        Chip8InstructionSet.SetDelayTimer(emulator, 0xF115);

        Chip8InstructionSet.ReadDelayTimer(emulator, 0xF207);

        Assert.Equal(0x2A, emulator.Debugger.Registers[2]);
    }

    [Fact]
    public void TimerIns_Dispatches07ToReadDelayTimer()
    {
        var emulator = CreateEmulator();
        Chip8InstructionSet.SetRegisterValue(emulator, 0x6155);
        Chip8InstructionSet.SetDelayTimer(emulator, 0xF115);

        Chip8InstructionSet.TimerInstructions(emulator, 0xF207);

        Assert.Equal(0x55, emulator.Debugger.Registers[2]);
    }

    [Fact]
    public void TimerIns_Dispatches15ToSetDelayTimer()
    {
        var emulator = CreateEmulator();
        Chip8InstructionSet.SetRegisterValue(emulator, 0x6199);

        Chip8InstructionSet.TimerInstructions(emulator, 0xF115);

        Assert.Equal(0x99, emulator.Debugger.DelayTimer);
    }

    [Fact]
    public void TimerIns_Dispatches18ToSetSoundTimer()
    {
        var emulator = CreateEmulator();
        Chip8InstructionSet.SetRegisterValue(emulator, 0x617F);

        Chip8InstructionSet.TimerInstructions(emulator, 0xF118);

        Assert.Equal(0x7F, emulator.Debugger.SoundTimer);
    }

    [Fact]
    public void TimerIns_UnknownSubOp_IsNoOp()
    {
        var emulator = CreateEmulator();
        Chip8InstructionSet.SetRegisterValue(emulator, 0x61AA);
        var pcBefore = emulator.Debugger.ProgramCounter;

        Chip8InstructionSet.TimerInstructions(emulator, 0xF100);

        Assert.Equal(pcBefore, emulator.Debugger.ProgramCounter);
    }

    [Fact]
    public void AddVxToI_AddsVxToIndexRegister()
    {
        var emulator = CreateEmulator();
        Chip8InstructionSet.SetIndexRegisterIns(emulator, 0xA100);
        Chip8InstructionSet.SetRegisterValue(emulator, 0x6125);

        Chip8InstructionSet.AddVxToI(emulator, 0xF11E);

        Assert.Equal(0x125, emulator.Debugger.IndexRegister);
    }

    [Fact]
    public void AddVxToI_AccumulatesAcrossCalls()
    {
        var emulator = CreateEmulator();
        Chip8InstructionSet.SetIndexRegisterIns(emulator, 0xA010);
        Chip8InstructionSet.SetRegisterValue(emulator, 0x6105);

        Chip8InstructionSet.AddVxToI(emulator, 0xF11E);
        Chip8InstructionSet.AddVxToI(emulator, 0xF11E);

        Assert.Equal(0x01A, emulator.Debugger.IndexRegister);
    }

    [Theory]
    [InlineData(0x0, 0x050)]
    [InlineData(0x1, 0x055)]
    [InlineData(0x9, 0x07D)]
    [InlineData(0xA, 0x082)]
    [InlineData(0xF, 0x09B)]
    public void LoadFontCharacter_SetsIndexToFontBasePlus5TimesVx(byte vx, int expectedIndex)
    {
        var emulator = CreateEmulator();
        Chip8InstructionSet.SetRegisterValue(emulator, 0x6100 | vx);

        Chip8InstructionSet.LoadLowResFontCharacter(emulator, 0xF129);

        Assert.Equal(expectedIndex, emulator.Debugger.IndexRegister);
    }

    [Fact]
    public void LoadFontCharacter_IndexPointsAtFontSpriteData()
    {
        var emulator = CreateEmulator();
        Chip8InstructionSet.SetRegisterValue(emulator, 0x6100);

        Chip8InstructionSet.LoadLowResFontCharacter(emulator, 0xF129);

        var sprite = emulator.Debugger.Memory.Slice(emulator.Debugger.IndexRegister, 5);
        Assert.Equal(new byte[] { 0xF0, 0x90, 0x90, 0x90, 0xF0 }, sprite.ToArray());
    }

    [Fact]
    public void WaitForKeyPress_SetsIsWaitingForKeyPressFlag()
    {
        var emulator = CreateEmulator();

        Chip8InstructionSet.WaitForKeyPress(emulator, 0xF20A);

        Assert.True(emulator.Debugger.IsWaitingForKey);
    }

    [Fact]
    public void WaitForKeyPress_DoesNotImmediatelyWriteToRegister()
    {
        var emulator = CreateEmulator();

        Chip8InstructionSet.WaitForKeyPress(emulator, 0xF20A);

        Assert.Equal(0, emulator.Debugger.Registers[2]);
    }

    [Fact]
    public void Update_WhileWaitingForKey_AndNoKeyPressed_StaysWaiting()
    {
        var emulator = CreateEmulator(out var clock, out _);
        emulator.Start();
        Chip8InstructionSet.WaitForKeyPress(emulator, 0xF20A);
        clock.Timestamp = clock.Frequency / 60;

        clock.Tick();

        Assert.True(emulator.Debugger.IsWaitingForKey);
        Assert.Equal(0, emulator.Debugger.Registers[2]);
    }

    [Fact]
    public void Update_WhileWaitingForKey_AndKeyPressed_StoresKeyAndResumes()
    {
        var emulator = CreateEmulator(out var clock, out var input);
        emulator.Start();
        emulator.WriteMemory(0, [0x10, 0x00]);
        Chip8InstructionSet.WaitForKeyPress(emulator, 0xF20A);
        input.QueueKeyPressAndReleaseEvent(0xA);
        clock.Timestamp = clock.Frequency / 60;

        clock.Tick();

        Assert.False(emulator.Debugger.IsWaitingForKey);
        Assert.Equal(0xA, emulator.Debugger.Registers[2]);
    }

    [Fact]
    public void Update_WhileWaitingForKey_DelayTimerStillTicks()
    {
        var emulator = CreateEmulator(out var clock, out _);
        emulator.Start();
        Chip8InstructionSet.SetRegisterValue(emulator, 0x600A);
        Chip8InstructionSet.SetDelayTimer(emulator, 0xF015);
        Chip8InstructionSet.WaitForKeyPress(emulator, 0xF10A);
        clock.Timestamp = clock.Frequency / 60;

        clock.Tick();

        Assert.Equal(9, emulator.Debugger.DelayTimer);
        Assert.True(emulator.Debugger.IsWaitingForKey);
    }

    [Fact]
    public void Update_WhileWaitingForKey_SoundTimerStillTicks()
    {
        var emulator = CreateEmulator(out var clock, out _);
        emulator.Start();
        Chip8InstructionSet.SetRegisterValue(emulator, 0x6005);
        Chip8InstructionSet.SetSoundTimer(emulator, 0xF018);
        Chip8InstructionSet.WaitForKeyPress(emulator, 0xF10A);
        clock.Timestamp = clock.Frequency / 60;

        clock.Tick();

        Assert.Equal(4, emulator.Debugger.SoundTimer);
    }

    [Fact]
    public void TimerIns_Dispatches0AToWaitForKeyPress()
    {
        var emulator = CreateEmulator();

        Chip8InstructionSet.TimerInstructions(emulator, 0xF10A);

        Assert.True(emulator.Debugger.IsWaitingForKey);
    }

    [Fact]
    public void TimerIns_Dispatches1EToAddVxToI()
    {
        var emulator = CreateEmulator();
        Chip8InstructionSet.SetIndexRegisterIns(emulator, 0xA020);
        Chip8InstructionSet.SetRegisterValue(emulator, 0x6103);

        Chip8InstructionSet.TimerInstructions(emulator, 0xF11E);

        Assert.Equal(0x023, emulator.Debugger.IndexRegister);
    }

    [Fact]
    public void TimerIns_Dispatches29ToLoadFontCharacter()
    {
        var emulator = CreateEmulator();
        Chip8InstructionSet.SetRegisterValue(emulator, 0x6103);

        Chip8InstructionSet.TimerInstructions(emulator, 0xF129);

        Assert.Equal(0x050 + 5 * 3, emulator.Debugger.IndexRegister);
    }

    [Fact]
    public void StoreRegisters_WritesV0ThroughVxIntoMemoryAtI()
    {
        var emulator = CreateEmulator();
        Chip8InstructionSet.SetRegisterValue(emulator, 0x6011);
        Chip8InstructionSet.SetRegisterValue(emulator, 0x6122);
        Chip8InstructionSet.SetRegisterValue(emulator, 0x6233);
        Chip8InstructionSet.SetRegisterValue(emulator, 0x6344);
        Chip8InstructionSet.SetIndexRegisterIns(emulator, 0xA300);

        Chip8InstructionSet.ExecuteStoreRegisters(emulator, 0xF355);

        Assert.Equal(0x11, emulator.Debugger.Memory[0x300]);
        Assert.Equal(0x22, emulator.Debugger.Memory[0x301]);
        Assert.Equal(0x33, emulator.Debugger.Memory[0x302]);
        Assert.Equal(0x44, emulator.Debugger.Memory[0x303]);
        Assert.Equal(0x00, emulator.Debugger.Memory[0x304]);
    }

    [Fact]
    public void StoreRegisters_V0Only_WritesSingleByte()
    {
        var emulator = CreateEmulator();
        Chip8InstructionSet.SetRegisterValue(emulator, 0x60AB);
        Chip8InstructionSet.SetRegisterValue(emulator, 0x61CD);
        Chip8InstructionSet.SetIndexRegisterIns(emulator, 0xA300);

        Chip8InstructionSet.ExecuteStoreRegisters(emulator, 0xF055);

        Assert.Equal(0xAB, emulator.Debugger.Memory[0x300]);
        Assert.Equal(0x00, emulator.Debugger.Memory[0x301]);
    }

    [Fact]
    public void LoadRegisters_ReadsMemoryAtIIntoV0ThroughVx()
    {
        var emulator = CreateEmulator();
        Chip8InstructionSet.SetIndexRegisterIns(emulator, 0xA300);
        emulator.WriteMemory(0x300, [0x11, 0x22, 0x33, 0x44, 0xFF]);

        Chip8InstructionSet.ExecuteLoadRegisters(emulator, 0xF365);

        Assert.Equal(0x11, emulator.Debugger.Registers[0]);
        Assert.Equal(0x22, emulator.Debugger.Registers[1]);
        Assert.Equal(0x33, emulator.Debugger.Registers[2]);
        Assert.Equal(0x44, emulator.Debugger.Registers[3]);
        Assert.Equal(0x00, emulator.Debugger.Registers[4]);
    }

    [Fact]
    public void LoadRegisters_V0Only_ReadsSingleByte()
    {
        var emulator = CreateEmulator();
        Chip8InstructionSet.SetIndexRegisterIns(emulator, 0xA300);
        emulator.WriteMemory(0x300, [0xAB, 0xCD]);

        Chip8InstructionSet.ExecuteLoadRegisters(emulator, 0xF065);

        Assert.Equal(0xAB, emulator.Debugger.Registers[0]);
        Assert.Equal(0x00, emulator.Debugger.Registers[1]);
    }

    [Fact]
    public void StoreThenLoad_RoundTripsRegisters()
    {
        var emulator = CreateEmulator();
        Chip8InstructionSet.SetRegisterValue(emulator, 0x6012);
        Chip8InstructionSet.SetRegisterValue(emulator, 0x6134);
        Chip8InstructionSet.SetRegisterValue(emulator, 0x6256);
        Chip8InstructionSet.SetIndexRegisterIns(emulator, 0xA200);
        Chip8InstructionSet.ExecuteStoreRegisters(emulator, 0xF255);

        Chip8InstructionSet.SetRegisterValue(emulator, 0x6000);
        Chip8InstructionSet.SetRegisterValue(emulator, 0x6100);
        Chip8InstructionSet.SetRegisterValue(emulator, 0x6200);
        Chip8InstructionSet.ExecuteLoadRegisters(emulator, 0xF265);

        Assert.Equal(0x12, emulator.Debugger.Registers[0]);
        Assert.Equal(0x34, emulator.Debugger.Registers[1]);
        Assert.Equal(0x56, emulator.Debugger.Registers[2]);
    }

    [Fact]
    public void TimerIns_Dispatches55ToStoreRegisters()
    {
        var emulator = CreateEmulator();
        Chip8InstructionSet.SetRegisterValue(emulator, 0x6099);
        Chip8InstructionSet.SetIndexRegisterIns(emulator, 0xA400);

        Chip8InstructionSet.TimerInstructions(emulator, 0xF055);

        Assert.Equal(0x99, emulator.Debugger.Memory[0x400]);
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
        var emulator = CreateEmulator();
        Chip8InstructionSet.SetRegisterValue(emulator, 0x6100 | vx);
        Chip8InstructionSet.SetIndexRegisterIns(emulator, 0xA300);

        Chip8InstructionSet.StoreBcdInMemory(emulator, 0xF133);

        Assert.Equal(hundreds, emulator.Debugger.Memory[0x300]);
        Assert.Equal(tens, emulator.Debugger.Memory[0x301]);
        Assert.Equal(ones, emulator.Debugger.Memory[0x302]);
    }

    [Fact]
    public void StoreBcdInMemory_DoesNotModifyRegisterOrIndex()
    {
        var emulator = CreateEmulator();
        Chip8InstructionSet.SetRegisterValue(emulator, 0x617B);
        Chip8InstructionSet.SetIndexRegisterIns(emulator, 0xA300);

        Chip8InstructionSet.StoreBcdInMemory(emulator, 0xF133);

        Assert.Equal(0x7B, emulator.Debugger.Registers[1]);
        Assert.Equal(0x300, emulator.Debugger.IndexRegister);
    }

    [Fact]
    public void TimerIns_Dispatches33ToStoreBcdInMemory()
    {
        var emulator = CreateEmulator();
        Chip8InstructionSet.SetRegisterValue(emulator, 0x61C8);
        Chip8InstructionSet.SetIndexRegisterIns(emulator, 0xA400);

        Chip8InstructionSet.TimerInstructions(emulator, 0xF133);

        Assert.Equal(2, emulator.Debugger.Memory[0x400]);
        Assert.Equal(0, emulator.Debugger.Memory[0x401]);
        Assert.Equal(0, emulator.Debugger.Memory[0x402]);
    }

    [Fact]
    public void TimerIns_Dispatches65ToLoadRegisters()
    {
        var emulator = CreateEmulator();
        Chip8InstructionSet.SetIndexRegisterIns(emulator, 0xA400);
        emulator.WriteMemory(0x400, [0x77]);

        Chip8InstructionSet.TimerInstructions(emulator, 0xF065);

        Assert.Equal(0x77, emulator.Debugger.Registers[0]);
    }

    [Fact]
    public void ClearDisplay_ZerosAllDisplayPixels()
    {
        var emulator = CreateEmulator();
        Chip8InstructionSet.SetIndexRegisterIns(emulator, 0xA000);
        emulator.WriteMemory(0, [0xFF]);
        Chip8InstructionSet.DrawToScreen(emulator, 0xD001);
        Assert.Contains(emulator.Display.Pixels.ToArray(), p => p == 1);

        Chip8InstructionSet.ClearDisplay(emulator, 0x00E0);

        foreach (var p in emulator.Display.Pixels.Span)
            Assert.Equal(0, p);
    }

}
