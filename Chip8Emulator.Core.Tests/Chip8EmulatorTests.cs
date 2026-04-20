using Chip8Emulator.Core.Impl;
using Chip8Emulator.Core.Tests.Fakes;

namespace Chip8Emulator.Core.Tests;

public class Chip8EmulatorTests
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
    [InlineData(0x000)]
    [InlineData(0x200)]
    [InlineData(0xFFF)]
    public void JumpToAddress_SetsProgramCounter(int address)
    {
        var emulator = CreateEmulator();

        emulator.ExecuteJumpToAddressIns(address);

        Assert.Equal(address, emulator.ProgramCounter);
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

    [Fact]
    public void ReturnFromSubroutine_NotYetImplemented()
    {
        var emulator = CreateEmulator();

        Assert.Throws<NotImplementedException>(() => emulator.ExecuteReturnFromSubroutineIns());
    }
}
