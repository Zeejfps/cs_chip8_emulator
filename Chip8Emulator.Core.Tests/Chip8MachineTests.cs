using Chip8Emulator.Core.Impl;
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

        Assert.Equal(0, emulator.ProgramCounter);
        Assert.Equal(0, emulator.IndexRegister);
        Assert.Equal(0, emulator.DelayTimer);
        Assert.Equal(0, emulator.SoundTimer);
        Assert.Equal(4096, emulator.Memory.Length);
    }

    [Fact]
    public void InitialState_AllRegistersAreZero()
    {
        var emulator = CreateEmulator();

        for (var i = 0; i < 16; i++)
        {
            Assert.Equal(0, emulator.ReadRegister(i));
        }
    }

    [Fact]
    public void InitialState_MemoryIsZeroOutsideFontRegion()
    {
        var emulator = CreateEmulator();

        for (var i = 0; i < emulator.Memory.Length; i++)
        {
            if (i >= 0x050 && i < 0x050 + 80) continue;
            Assert.Equal(0, emulator.Memory[i]);
        }
    }

    [Fact]
    public void InitialState_FontLoadedAt0x050()
    {
        var emulator = CreateEmulator();

        var zeroSprite = emulator.Memory.Slice(0x050, 5);
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

        emulator.ExecuteSetRegisterValueIns(instruction);

        Assert.Equal(expected, emulator.ReadRegister(x));
    }

    [Fact]
    public void SetRegisterValue_DoesNotAffectOtherRegisters()
    {
        var emulator = CreateEmulator();

        emulator.ExecuteSetRegisterValueIns(0x6342);

        for (var i = 0; i < 16; i++)
        {
            if (i == 3) continue;
            Assert.Equal(0, emulator.ReadRegister(i));
        }
    }

    [Fact]
    public void AddValueToRegister_AddsNnToVx()
    {
        var emulator = CreateEmulator();
        emulator.ExecuteSetRegisterValueIns(0x6205);

        emulator.ExecuteAddValueToRegisterIns(0x7203);

        Assert.Equal(8, emulator.ReadRegister(2));
    }

    [Fact]
    public void AddValueToRegister_WrapsOnByteOverflow()
    {
        var emulator = CreateEmulator();
        emulator.ExecuteSetRegisterValueIns(0x62FF);

        emulator.ExecuteAddValueToRegisterIns(0x7202);

        Assert.Equal(0x01, emulator.ReadRegister(2));
    }

    [Theory]
    [InlineData(0xA000, 0x000)]
    [InlineData(0xA123, 0x123)]
    [InlineData(0xAFFF, 0xFFF)]
    public void SetIndexRegister_StoresNnn(int instruction, int expected)
    {
        var emulator = CreateEmulator();

        emulator.ExecuteSetIndexRegisterIns(instruction);

        Assert.Equal(expected, emulator.IndexRegister);
    }

    [Theory]
    [InlineData(0x1000, 0x000)]
    [InlineData(0x1200, 0x200)]
    [InlineData(0x1FFF, 0xFFF)]
    public void JumpToAddress_SetsProgramCounterToNnn(int instruction, int expected)
    {
        var emulator = CreateEmulator();

        emulator.ExecuteJumpToAddressIns(instruction);

        Assert.Equal(expected, emulator.ProgramCounter);
    }

    [Fact]
    public void SkipIfRegisterEqualsValue_SkipsNextInstruction_WhenEqual()
    {
        var emulator = CreateEmulator();
        emulator.ExecuteSetRegisterValueIns(0x6242);
        var pcBefore = emulator.ProgramCounter;

        emulator.ExecuteSkipNextInsIfRegisterValueEqualsValueIns(0x3242);

        Assert.Equal(pcBefore + 2, emulator.ProgramCounter);
    }

    [Fact]
    public void SkipIfRegisterEqualsValue_DoesNotSkip_WhenNotEqual()
    {
        var emulator = CreateEmulator();
        emulator.ExecuteSetRegisterValueIns(0x6242);
        var pcBefore = emulator.ProgramCounter;

        emulator.ExecuteSkipNextInsIfRegisterValueEqualsValueIns(0x3201);

        Assert.Equal(pcBefore, emulator.ProgramCounter);
    }

    [Fact]
    public void SkipIfRegisterNotEqualsValue_SkipsNextInstruction_WhenNotEqual()
    {
        var emulator = CreateEmulator();
        emulator.ExecuteSetRegisterValueIns(0x6242);
        var pcBefore = emulator.ProgramCounter;

        emulator.ExecuteSkipNextInsIfRegisterValueNotEqualsValueIns(0x4201);

        Assert.Equal(pcBefore + 2, emulator.ProgramCounter);
    }

    [Fact]
    public void SkipIfRegisterNotEqualsValue_DoesNotSkip_WhenEqual()
    {
        var emulator = CreateEmulator();
        emulator.ExecuteSetRegisterValueIns(0x6242);
        var pcBefore = emulator.ProgramCounter;

        emulator.ExecuteSkipNextInsIfRegisterValueNotEqualsValueIns(0x4242);

        Assert.Equal(pcBefore, emulator.ProgramCounter);
    }

    [Fact]
    public void SkipIfRegisterEqualsRegister_SkipsNextInstruction_WhenEqual()
    {
        var emulator = CreateEmulator();
        emulator.ExecuteSetRegisterValueIns(0x6142);
        emulator.ExecuteSetRegisterValueIns(0x6242);
        var pcBefore = emulator.ProgramCounter;

        emulator.ExecuteSkipNextInsIfRegisterValueEqualsRegisterValue(0x5120);

        Assert.Equal(pcBefore + 2, emulator.ProgramCounter);
    }

    [Fact]
    public void SkipIfRegisterEqualsRegister_DoesNotSkip_WhenNotEqual()
    {
        var emulator = CreateEmulator();
        emulator.ExecuteSetRegisterValueIns(0x6142);
        emulator.ExecuteSetRegisterValueIns(0x6201);
        var pcBefore = emulator.ProgramCounter;

        emulator.ExecuteSkipNextInsIfRegisterValueEqualsRegisterValue(0x5120);

        Assert.Equal(pcBefore, emulator.ProgramCounter);
    }

    [Fact]
    public void CallSubroutine_JumpsToAddressAndPushesReturnAddress()
    {
        var emulator = CreateEmulator();
        emulator.ExecuteJumpToAddressIns(0x1246);
        var spBefore = emulator.StackPointer;

        emulator.ExecuteCallSubroutineIns(0x2ABC);

        Assert.Equal(0xABC, emulator.ProgramCounter);
        Assert.Equal(spBefore + 1, emulator.StackPointer);
        Assert.Equal(0x246, emulator.PeekStack());
    }

    [Fact]
    public void ReturnFromSubroutine_RestoresProgramCounterFromStack()
    {
        var emulator = CreateEmulator();
        emulator.ExecuteJumpToAddressIns(0x1246);
        emulator.ExecuteCallSubroutineIns(0x2ABC);

        emulator.ExecuteReturnFromSubroutineIns();

        Assert.Equal(0x246, emulator.ProgramCounter);
    }

    [Fact]
    public void SetRegisterValueFromRegister_CopiesVyIntoVx()
    {
        var emulator = CreateEmulator();
        emulator.ExecuteSetRegisterValueIns(0x6142);
        emulator.ExecuteSetRegisterValueIns(0x62AB);

        emulator.ExecuteSetRegisterValueFromRegisterIns(0x8120);

        Assert.Equal(0xAB, emulator.ReadRegister(1));
        Assert.Equal(0xAB, emulator.ReadRegister(2));
    }

    [Fact]
    public void BitwiseOrOnRegisters_StoresVxOrVyIntoVx()
    {
        var emulator = CreateEmulator();
        emulator.ExecuteSetRegisterValueIns(0x61F0);
        emulator.ExecuteSetRegisterValueIns(0x620F);

        emulator.ExecuteBitwiseOrOnRegistersIns(0x8121);

        Assert.Equal(0xFF, emulator.ReadRegister(1));
        Assert.Equal(0x0F, emulator.ReadRegister(2));
    }

    [Fact]
    public void BitwiseAndOnRegisters_StoresVxAndVyIntoVx()
    {
        var emulator = CreateEmulator();
        emulator.ExecuteSetRegisterValueIns(0x61FC);
        emulator.ExecuteSetRegisterValueIns(0x620F);

        emulator.ExecuteBitwiseAndOnRegistersIns(0x8122);

        Assert.Equal(0x0C, emulator.ReadRegister(1));
        Assert.Equal(0x0F, emulator.ReadRegister(2));
    }

    [Fact]
    public void XorRegisterValueFromRegister_StoresVxXorVyIntoVx()
    {
        var emulator = CreateEmulator();
        emulator.ExecuteSetRegisterValueIns(0x61FC);
        emulator.ExecuteSetRegisterValueIns(0x620F);

        emulator.ExecuteXorRegisterValueFromRegisterIns(0x8123);

        Assert.Equal(0xF3, emulator.ReadRegister(1));
        Assert.Equal(0x0F, emulator.ReadRegister(2));
    }

    [Fact]
    public void AddValueToRegisterWithCarry_NoOverflow_StoresSumAndClearsVf()
    {
        var emulator = CreateEmulator();
        emulator.ExecuteSetRegisterValueIns(0x6105);
        emulator.ExecuteSetRegisterValueIns(0x6203);
        emulator.ExecuteSetRegisterValueIns(0x6F01);

        emulator.ExecuteAddValueToRegisterWithCarryIns(0x8124);

        Assert.Equal(0x08, emulator.ReadRegister(1));
        Assert.Equal(0x03, emulator.ReadRegister(2));
        Assert.Equal(0, emulator.ReadRegister(0xF));
    }

    [Fact]
    public void AddValueToRegisterWithCarry_Overflow_WrapsAndSetsVf()
    {
        var emulator = CreateEmulator();
        emulator.ExecuteSetRegisterValueIns(0x61FF);
        emulator.ExecuteSetRegisterValueIns(0x6202);

        emulator.ExecuteAddValueToRegisterWithCarryIns(0x8124);

        Assert.Equal(0x01, emulator.ReadRegister(1));
        Assert.Equal(0x02, emulator.ReadRegister(2));
        Assert.Equal(1, emulator.ReadRegister(0xF));
    }

    [Fact]
    public void AddValueToRegisterWithCarry_AtBoundary_DoesNotSetVf()
    {
        var emulator = CreateEmulator();
        emulator.ExecuteSetRegisterValueIns(0x61F0);
        emulator.ExecuteSetRegisterValueIns(0x620F);

        emulator.ExecuteAddValueToRegisterWithCarryIns(0x8124);

        Assert.Equal(0xFF, emulator.ReadRegister(1));
        Assert.Equal(0, emulator.ReadRegister(0xF));
    }

    [Fact]
    public void VxSubVy_NoBorrow_StoresDifferenceAndSetsVf()
    {
        var emulator = CreateEmulator();
        emulator.ExecuteSetRegisterValueIns(0x610A);
        emulator.ExecuteSetRegisterValueIns(0x6203);

        emulator.ExecuteVxSubVyIns(0x8125);

        Assert.Equal(0x07, emulator.ReadRegister(1));
        Assert.Equal(0x03, emulator.ReadRegister(2));
        Assert.Equal(1, emulator.ReadRegister(0xF));
    }

    [Fact]
    public void VxSubVy_Borrow_WrapsAndClearsVf()
    {
        var emulator = CreateEmulator();
        emulator.ExecuteSetRegisterValueIns(0x6103);
        emulator.ExecuteSetRegisterValueIns(0x620A);

        emulator.ExecuteVxSubVyIns(0x8125);

        Assert.Equal(0xF9, emulator.ReadRegister(1));
        Assert.Equal(0x0A, emulator.ReadRegister(2));
        Assert.Equal(0, emulator.ReadRegister(0xF));
    }

    [Fact]
    public void VxSubVy_WhenEqual_SetsVf()
    {
        var emulator = CreateEmulator();
        emulator.ExecuteSetRegisterValueIns(0x6142);
        emulator.ExecuteSetRegisterValueIns(0x6242);

        emulator.ExecuteVxSubVyIns(0x8125);

        Assert.Equal(0x00, emulator.ReadRegister(1));
        Assert.Equal(1, emulator.ReadRegister(0xF));
    }

    [Fact]
    public void VySubVx_NoBorrow_StoresDifferenceAndSetsVf()
    {
        var emulator = CreateEmulator();
        emulator.ExecuteSetRegisterValueIns(0x6103);
        emulator.ExecuteSetRegisterValueIns(0x620A);

        emulator.ExecuteVySubVxIns(0x8127);

        Assert.Equal(0x07, emulator.ReadRegister(1));
        Assert.Equal(0x0A, emulator.ReadRegister(2));
        Assert.Equal(1, emulator.ReadRegister(0xF));
    }

    [Fact]
    public void VySubVx_Borrow_WrapsAndClearsVf()
    {
        var emulator = CreateEmulator();
        emulator.ExecuteSetRegisterValueIns(0x610A);
        emulator.ExecuteSetRegisterValueIns(0x6203);

        emulator.ExecuteVySubVxIns(0x8127);

        Assert.Equal(0xF9, emulator.ReadRegister(1));
        Assert.Equal(0x03, emulator.ReadRegister(2));
        Assert.Equal(0, emulator.ReadRegister(0xF));
    }

    [Fact]
    public void ShiftRight_EvenValue_ShiftsAndClearsVf()
    {
        var emulator = CreateEmulator();
        emulator.ExecuteSetRegisterValueIns(0x6108);

        emulator.ExecuteShiftRightIns(0x8106);

        Assert.Equal(0x04, emulator.ReadRegister(1));
        Assert.Equal(0, emulator.ReadRegister(0xF));
    }

    [Fact]
    public void ShiftRight_OddValue_ShiftsAndSetsVf()
    {
        var emulator = CreateEmulator();
        emulator.ExecuteSetRegisterValueIns(0x6109);

        emulator.ExecuteShiftRightIns(0x8106);

        Assert.Equal(0x04, emulator.ReadRegister(1));
        Assert.Equal(1, emulator.ReadRegister(0xF));
    }

    [Fact]
    public void ShiftLeft_MsbClear_ShiftsAndClearsVf()
    {
        var emulator = CreateEmulator();
        emulator.ExecuteSetRegisterValueIns(0x6141);

        emulator.ExecuteShiftLeftIns(0x810E);

        Assert.Equal(0x82, emulator.ReadRegister(1));
        Assert.Equal(0, emulator.ReadRegister(0xF));
    }

    [Fact]
    public void ShiftLeft_MsbSet_ShiftsAndSetsVf()
    {
        var emulator = CreateEmulator();
        emulator.ExecuteSetRegisterValueIns(0x6181);

        emulator.ExecuteShiftLeftIns(0x810E);

        Assert.Equal(0x02, emulator.ReadRegister(1));
        Assert.Equal(1, emulator.ReadRegister(0xF));
    }

    [Fact]
    public void SkipIfRegisterNotEqualsRegister_SkipsNextInstruction_WhenNotEqual()
    {
        var emulator = CreateEmulator();
        emulator.ExecuteSetRegisterValueIns(0x6142);
        emulator.ExecuteSetRegisterValueIns(0x6201);
        var pcBefore = emulator.ProgramCounter;

        emulator.ExecuteSkipNextInsIfRegisterValueNotEqualsRegisterValue(0x9120);

        Assert.Equal(pcBefore + 2, emulator.ProgramCounter);
    }

    [Fact]
    public void SkipIfRegisterNotEqualsRegister_DoesNotSkip_WhenEqual()
    {
        var emulator = CreateEmulator();
        emulator.ExecuteSetRegisterValueIns(0x6142);
        emulator.ExecuteSetRegisterValueIns(0x6242);
        var pcBefore = emulator.ProgramCounter;

        emulator.ExecuteSkipNextInsIfRegisterValueNotEqualsRegisterValue(0x9120);

        Assert.Equal(pcBefore, emulator.ProgramCounter);
    }

    [Theory]
    [InlineData(0x8120, 0x05, 0xAA, 0xAA)]
    [InlineData(0x8121, 0xF0, 0x0F, 0xFF)]
    [InlineData(0x8122, 0xFC, 0x0F, 0x0C)]
    [InlineData(0x8123, 0xFC, 0x0F, 0xF3)]
    public void ArithmeticOperation_DispatchesToCorrectOperation(int instruction, byte vx, byte vy, byte expected)
    {
        var emulator = CreateEmulator();
        emulator.ExecuteSetRegisterValueIns(0x6100 | vx);
        emulator.ExecuteSetRegisterValueIns(0x6200 | vy);

        emulator.ExecuteArithmeticOperationIns(instruction);

        Assert.Equal(expected, emulator.ReadRegister(1));
    }

    [Fact]
    public void SkipIfKeyIsPressed_SkipsNextInstruction_WhenPressed()
    {
        var emulator = CreateEmulator(out var input);
        emulator.ExecuteSetRegisterValueIns(0x6105);
        input.Press(0x5);
        var pcBefore = emulator.ProgramCounter;

        emulator.ExecuteSkipNextInsIfKeyIsPressed(0xE19E);

        Assert.Equal(pcBefore + 2, emulator.ProgramCounter);
    }

    [Fact]
    public void SkipIfKeyIsPressed_DoesNotSkip_WhenNotPressed()
    {
        var emulator = CreateEmulator(out var input);
        emulator.ExecuteSetRegisterValueIns(0x6105);
        input.Press(0x3);
        var pcBefore = emulator.ProgramCounter;

        emulator.ExecuteSkipNextInsIfKeyIsPressed(0xE19E);

        Assert.Equal(pcBefore, emulator.ProgramCounter);
    }

    [Fact]
    public void SkipIfKeyIsReleased_SkipsNextInstruction_WhenNotPressed()
    {
        var emulator = CreateEmulator(out var input);
        emulator.ExecuteSetRegisterValueIns(0x6105);
        input.Press(0x3);
        var pcBefore = emulator.ProgramCounter;

        emulator.ExecuteSkipNextInsIfKeyIsReleased(0xE1A1);

        Assert.Equal(pcBefore + 2, emulator.ProgramCounter);
    }

    [Fact]
    public void SkipIfKeyIsReleased_DoesNotSkip_WhenPressed()
    {
        var emulator = CreateEmulator(out var input);
        emulator.ExecuteSetRegisterValueIns(0x6105);
        input.Press(0x5);
        var pcBefore = emulator.ProgramCounter;

        emulator.ExecuteSkipNextInsIfKeyIsReleased(0xE1A1);

        Assert.Equal(pcBefore, emulator.ProgramCounter);
    }

    [Fact]
    public void SkipIfKeyIsPressedOrReleased_Dispatches9EToIsPressed()
    {
        var emulator = CreateEmulator(out var input);
        emulator.ExecuteSetRegisterValueIns(0x6107);
        input.Press(0x7);
        var pcBefore = emulator.ProgramCounter;

        emulator.ExecuteSkipNextInsIfKeyIsPressedOrReleased(0xE19E);

        Assert.Equal(pcBefore + 2, emulator.ProgramCounter);
    }

    [Fact]
    public void SkipIfKeyIsPressedOrReleased_DispatchesA1ToIsReleased()
    {
        var emulator = CreateEmulator(out var input);
        emulator.ExecuteSetRegisterValueIns(0x6107);
        var pcBefore = emulator.ProgramCounter;

        emulator.ExecuteSkipNextInsIfKeyIsPressedOrReleased(0xE1A1);

        Assert.Equal(pcBefore + 2, emulator.ProgramCounter);
    }

    [Fact]
    public void SkipIfKeyIsPressedOrReleased_UnknownSubOp_Throws()
    {
        var emulator = CreateEmulator(out var input);
        emulator.ExecuteSetRegisterValueIns(0x6107);
        input.Press(0x7);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            emulator.ExecuteSkipNextInsIfKeyIsPressedOrReleased(0xE100));
    }

    [Fact]
    public void SetDelayTimer_StoresVxIntoDelayTimer()
    {
        var emulator = CreateEmulator();
        emulator.ExecuteSetRegisterValueIns(0x613C);

        emulator.ExecuteSetDelayTimer(0xF115);

        Assert.Equal(0x3C, emulator.DelayTimer);
    }

    [Fact]
    public void SetSoundTimer_StoresVxIntoSoundTimer()
    {
        var emulator = CreateEmulator();
        emulator.ExecuteSetRegisterValueIns(0x6120);

        emulator.ExecuteSetSoundTimer(0xF118);

        Assert.Equal(0x20, emulator.SoundTimer);
    }

    [Fact]
    public void ReadDelayTimer_StoresDelayTimerIntoVx()
    {
        var emulator = CreateEmulator();
        emulator.ExecuteSetRegisterValueIns(0x612A);
        emulator.ExecuteSetDelayTimer(0xF115);

        emulator.ExecuteReadDelayTimer(0xF207);

        Assert.Equal(0x2A, emulator.ReadRegister(2));
    }

    [Fact]
    public void TimerIns_Dispatches07ToReadDelayTimer()
    {
        var emulator = CreateEmulator();
        emulator.ExecuteSetRegisterValueIns(0x6155);
        emulator.ExecuteSetDelayTimer(0xF115);

        emulator.ExecuteTimerIns(0xF207);

        Assert.Equal(0x55, emulator.ReadRegister(2));
    }

    [Fact]
    public void TimerIns_Dispatches15ToSetDelayTimer()
    {
        var emulator = CreateEmulator();
        emulator.ExecuteSetRegisterValueIns(0x6199);

        emulator.ExecuteTimerIns(0xF115);

        Assert.Equal(0x99, emulator.DelayTimer);
    }

    [Fact]
    public void TimerIns_Dispatches18ToSetSoundTimer()
    {
        var emulator = CreateEmulator();
        emulator.ExecuteSetRegisterValueIns(0x617F);

        emulator.ExecuteTimerIns(0xF118);

        Assert.Equal(0x7F, emulator.SoundTimer);
    }

    [Fact]
    public void TimerIns_UnknownSubOp_Throws()
    {
        var emulator = CreateEmulator();
        emulator.ExecuteSetRegisterValueIns(0x61AA);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            emulator.ExecuteTimerIns(0xF100));
    }

    [Fact]
    public void AddVxToI_AddsVxToIndexRegister()
    {
        var emulator = CreateEmulator();
        emulator.ExecuteSetIndexRegisterIns(0xA100);
        emulator.ExecuteSetRegisterValueIns(0x6125);

        emulator.ExecuteAddVxToI(0xF11E);

        Assert.Equal(0x125, emulator.IndexRegister);
    }

    [Fact]
    public void AddVxToI_AccumulatesAcrossCalls()
    {
        var emulator = CreateEmulator();
        emulator.ExecuteSetIndexRegisterIns(0xA010);
        emulator.ExecuteSetRegisterValueIns(0x6105);

        emulator.ExecuteAddVxToI(0xF11E);
        emulator.ExecuteAddVxToI(0xF11E);

        Assert.Equal(0x01A, emulator.IndexRegister);
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
        emulator.ExecuteSetRegisterValueIns(0x6100 | vx);

        emulator.ExecuteLoadFontCharacter(0xF129);

        Assert.Equal(expectedIndex, emulator.IndexRegister);
    }

    [Fact]
    public void LoadFontCharacter_IndexPointsAtFontSpriteData()
    {
        var emulator = CreateEmulator();
        emulator.ExecuteSetRegisterValueIns(0x6100);

        emulator.ExecuteLoadFontCharacter(0xF129);

        var sprite = emulator.Memory.Slice(emulator.IndexRegister, 5);
        Assert.Equal(new byte[] { 0xF0, 0x90, 0x90, 0x90, 0xF0 }, sprite.ToArray());
    }

    [Fact]
    public void WaitForKeyPress_SetsIsWaitingForKeyPressFlag()
    {
        var emulator = CreateEmulator();

        emulator.ExecuteWaitForKeyPress(0xF20A);

        Assert.True(emulator.IsWaitingForKeyPress);
    }

    [Fact]
    public void WaitForKeyPress_DoesNotImmediatelyWriteToRegister()
    {
        var emulator = CreateEmulator();

        emulator.ExecuteWaitForKeyPress(0xF20A);

        Assert.Equal(0, emulator.ReadRegister(2));
    }

    [Fact]
    public void Update_WhileWaitingForKey_AndNoKeyPressed_StaysWaiting()
    {
        var emulator = CreateEmulator(out var clock, out _);
        emulator.ExecuteWaitForKeyPress(0xF20A);
        clock.Timestamp = clock.Frequency / 60;

        emulator.Update();

        Assert.True(emulator.IsWaitingForKeyPress);
        Assert.Equal(0, emulator.ReadRegister(2));
    }

    [Fact]
    public void Update_WhileWaitingForKey_AndKeyPressed_StoresKeyAndResumes()
    {
        var emulator = CreateEmulator(out var clock, out var input);
        emulator.WriteMemory(0, [0x10, 0x00]);
        emulator.ExecuteWaitForKeyPress(0xF20A);
        input.QueueKeyPressEvent(0xA);
        clock.Timestamp = clock.Frequency / 60;

        emulator.Update();

        Assert.False(emulator.IsWaitingForKeyPress);
        Assert.Equal(0xA, emulator.ReadRegister(2));
    }

    [Fact]
    public void Update_WhileWaitingForKey_DelayTimerStillTicks()
    {
        var emulator = CreateEmulator(out var clock, out _);
        emulator.ExecuteSetRegisterValueIns(0x600A);
        emulator.ExecuteSetDelayTimer(0xF015);
        emulator.ExecuteWaitForKeyPress(0xF10A);
        clock.Timestamp = clock.Frequency / 60;

        emulator.Update();

        Assert.Equal(9, emulator.DelayTimer);
        Assert.True(emulator.IsWaitingForKeyPress);
    }

    [Fact]
    public void Update_WhileWaitingForKey_SoundTimerStillTicks()
    {
        var emulator = CreateEmulator(out var clock, out _);
        emulator.ExecuteSetRegisterValueIns(0x6005);
        emulator.ExecuteSetSoundTimer(0xF018);
        emulator.ExecuteWaitForKeyPress(0xF10A);
        clock.Timestamp = clock.Frequency / 60;

        emulator.Update();

        Assert.Equal(4, emulator.SoundTimer);
    }

    [Fact]
    public void TimerIns_Dispatches0AToWaitForKeyPress()
    {
        var emulator = CreateEmulator();

        emulator.ExecuteTimerIns(0xF10A);

        Assert.True(emulator.IsWaitingForKeyPress);
    }

    [Fact]
    public void TimerIns_Dispatches1EToAddVxToI()
    {
        var emulator = CreateEmulator();
        emulator.ExecuteSetIndexRegisterIns(0xA020);
        emulator.ExecuteSetRegisterValueIns(0x6103);

        emulator.ExecuteTimerIns(0xF11E);

        Assert.Equal(0x023, emulator.IndexRegister);
    }

    [Fact]
    public void TimerIns_Dispatches29ToLoadFontCharacter()
    {
        var emulator = CreateEmulator();
        emulator.ExecuteSetRegisterValueIns(0x6103);

        emulator.ExecuteTimerIns(0xF129);

        Assert.Equal(0x050 + 5 * 3, emulator.IndexRegister);
    }

    [Fact]
    public void StoreRegisters_WritesV0ThroughVxIntoMemoryAtI()
    {
        var emulator = CreateEmulator();
        emulator.ExecuteSetRegisterValueIns(0x6011);
        emulator.ExecuteSetRegisterValueIns(0x6122);
        emulator.ExecuteSetRegisterValueIns(0x6233);
        emulator.ExecuteSetRegisterValueIns(0x6344);
        emulator.ExecuteSetIndexRegisterIns(0xA300);

        emulator.ExecuteStoreRegisters(0xF355);

        Assert.Equal(0x11, emulator.Memory[0x300]);
        Assert.Equal(0x22, emulator.Memory[0x301]);
        Assert.Equal(0x33, emulator.Memory[0x302]);
        Assert.Equal(0x44, emulator.Memory[0x303]);
        Assert.Equal(0x00, emulator.Memory[0x304]);
    }

    [Fact]
    public void StoreRegisters_V0Only_WritesSingleByte()
    {
        var emulator = CreateEmulator();
        emulator.ExecuteSetRegisterValueIns(0x60AB);
        emulator.ExecuteSetRegisterValueIns(0x61CD);
        emulator.ExecuteSetIndexRegisterIns(0xA300);

        emulator.ExecuteStoreRegisters(0xF055);

        Assert.Equal(0xAB, emulator.Memory[0x300]);
        Assert.Equal(0x00, emulator.Memory[0x301]);
    }

    [Fact]
    public void LoadRegisters_ReadsMemoryAtIIntoV0ThroughVx()
    {
        var emulator = CreateEmulator();
        emulator.ExecuteSetIndexRegisterIns(0xA300);
        emulator.WriteMemory(0x300, [0x11, 0x22, 0x33, 0x44, 0xFF]);

        emulator.ExecuteLoadRegisters(0xF365);

        Assert.Equal(0x11, emulator.ReadRegister(0));
        Assert.Equal(0x22, emulator.ReadRegister(1));
        Assert.Equal(0x33, emulator.ReadRegister(2));
        Assert.Equal(0x44, emulator.ReadRegister(3));
        Assert.Equal(0x00, emulator.ReadRegister(4));
    }

    [Fact]
    public void LoadRegisters_V0Only_ReadsSingleByte()
    {
        var emulator = CreateEmulator();
        emulator.ExecuteSetIndexRegisterIns(0xA300);
        emulator.WriteMemory(0x300, [0xAB, 0xCD]);

        emulator.ExecuteLoadRegisters(0xF065);

        Assert.Equal(0xAB, emulator.ReadRegister(0));
        Assert.Equal(0x00, emulator.ReadRegister(1));
    }

    [Fact]
    public void StoreThenLoad_RoundTripsRegisters()
    {
        var emulator = CreateEmulator();
        emulator.ExecuteSetRegisterValueIns(0x6012);
        emulator.ExecuteSetRegisterValueIns(0x6134);
        emulator.ExecuteSetRegisterValueIns(0x6256);
        emulator.ExecuteSetIndexRegisterIns(0xA200);
        emulator.ExecuteStoreRegisters(0xF255);

        emulator.ExecuteSetRegisterValueIns(0x6000);
        emulator.ExecuteSetRegisterValueIns(0x6100);
        emulator.ExecuteSetRegisterValueIns(0x6200);
        emulator.ExecuteLoadRegisters(0xF265);

        Assert.Equal(0x12, emulator.ReadRegister(0));
        Assert.Equal(0x34, emulator.ReadRegister(1));
        Assert.Equal(0x56, emulator.ReadRegister(2));
    }

    [Fact]
    public void TimerIns_Dispatches55ToStoreRegisters()
    {
        var emulator = CreateEmulator();
        emulator.ExecuteSetRegisterValueIns(0x6099);
        emulator.ExecuteSetIndexRegisterIns(0xA400);

        emulator.ExecuteTimerIns(0xF055);

        Assert.Equal(0x99, emulator.Memory[0x400]);
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
        emulator.ExecuteSetRegisterValueIns(0x6100 | vx);
        emulator.ExecuteSetIndexRegisterIns(0xA300);

        emulator.ExecuteStoreBcdInMemory(0xF133);

        Assert.Equal(hundreds, emulator.Memory[0x300]);
        Assert.Equal(tens, emulator.Memory[0x301]);
        Assert.Equal(ones, emulator.Memory[0x302]);
    }

    [Fact]
    public void StoreBcdInMemory_DoesNotModifyRegisterOrIndex()
    {
        var emulator = CreateEmulator();
        emulator.ExecuteSetRegisterValueIns(0x617B);
        emulator.ExecuteSetIndexRegisterIns(0xA300);

        emulator.ExecuteStoreBcdInMemory(0xF133);

        Assert.Equal(0x7B, emulator.ReadRegister(1));
        Assert.Equal(0x300, emulator.IndexRegister);
    }

    [Fact]
    public void TimerIns_Dispatches33ToStoreBcdInMemory()
    {
        var emulator = CreateEmulator();
        emulator.ExecuteSetRegisterValueIns(0x61C8);
        emulator.ExecuteSetIndexRegisterIns(0xA400);

        emulator.ExecuteTimerIns(0xF133);

        Assert.Equal(2, emulator.Memory[0x400]);
        Assert.Equal(0, emulator.Memory[0x401]);
        Assert.Equal(0, emulator.Memory[0x402]);
    }

    [Fact]
    public void TimerIns_Dispatches65ToLoadRegisters()
    {
        var emulator = CreateEmulator();
        emulator.ExecuteSetIndexRegisterIns(0xA400);
        emulator.WriteMemory(0x400, [0x77]);

        emulator.ExecuteTimerIns(0xF065);

        Assert.Equal(0x77, emulator.ReadRegister(0));
    }

    [Fact]
    public void ClearDisplay_ZerosAllDisplayPixels()
    {
        var emulator = CreateEmulator();
        emulator.ExecuteSetIndexRegisterIns(0xA000);
        emulator.WriteMemory(0, [0xFF]);
        emulator.ExeuteDrawToScreenIns(0xD001);
        Assert.Contains(emulator.Pixels.ToArray(), p => p == 1);

        emulator.ExecuteClearDisplayIns();

        foreach (var p in emulator.Pixels.Span)
            Assert.Equal(0, p);
    }

}
