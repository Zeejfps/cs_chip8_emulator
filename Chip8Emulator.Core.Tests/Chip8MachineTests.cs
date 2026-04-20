using Chip8Emulator.Core.Impl;
using Chip8Emulator.Core.Tests.Fakes;

namespace Chip8Emulator.Core.Tests;

public class Chip8MachineTests
{
    private static Chip8Machine CreateEmulator(out FakeDisplay display, out FakeAudio audio, out FakeClock clock)
    {
        display = new FakeDisplay();
        audio = new FakeAudio();
        clock = new FakeClock();
        return new Chip8Machine(display, audio, clock);
    }

    private static Chip8Machine CreateEmulator() => CreateEmulator(out _, out _, out _);

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
    public void InitialState_AllMemoryIsZero()
    {
        var emulator = CreateEmulator();

        foreach (var b in emulator.Memory)
        {
            Assert.Equal(0, b);
        }
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
    public void ClearDisplay_ZerosAllDisplayPixels()
    {
        var emulator = CreateEmulator();
        emulator.ExecuteSetIndexRegisterIns(0xA000);
        emulator.WriteMemory(0, [0xFF]);
        emulator.ExeuteDrawToScreenIns(0xD001);
        Assert.Contains(emulator.DisplayPixels.ToArray(), p => p == 1);

        emulator.ExecuteClearDisplayIns();

        foreach (var p in emulator.DisplayPixels)
            Assert.Equal(0, p);
    }

}
