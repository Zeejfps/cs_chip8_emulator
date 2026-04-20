using Emulator.Impl;
using Emulator.Tests.Fakes;

namespace Emulator.Tests;

public class DrawToScreenTests
{
    private const int ScreenWidth = 64;
    private const int ScreenHeight = 32;

    private static Chip8Emulator CreateEmulator()
        => new(new FakeDisplay(), new FakeAudio());

    private static byte PixelAt(Chip8Emulator emulator, int x, int y)
        => emulator.DisplayPixels[y * ScreenWidth + x];

    private static int CountLitPixels(Chip8Emulator emulator)
    {
        var count = 0;
        foreach (var p in emulator.DisplayPixels)
            if (p == 1) count++;
        return count;
    }

    [Fact]
    public void DrawsSpriteBitsIntoFramebuffer()
    {
        var emulator = CreateEmulator();
        emulator.WriteMemory(0x300, [0b10100101]);
        emulator.SetIndexRegister(0xA300);

        emulator.DrawToScreen(0xD001);

        Assert.Equal(1, PixelAt(emulator, 0, 0));
        Assert.Equal(0, PixelAt(emulator, 1, 0));
        Assert.Equal(1, PixelAt(emulator, 2, 0));
        Assert.Equal(0, PixelAt(emulator, 3, 0));
        Assert.Equal(0, PixelAt(emulator, 4, 0));
        Assert.Equal(1, PixelAt(emulator, 5, 0));
        Assert.Equal(0, PixelAt(emulator, 6, 0));
        Assert.Equal(1, PixelAt(emulator, 7, 0));
    }

    [Fact]
    public void DrawsMultipleRows()
    {
        var emulator = CreateEmulator();
        emulator.WriteMemory(0x300, [0xFF, 0x81, 0xFF]);
        emulator.SetIndexRegister(0xA300);

        emulator.DrawToScreen(0xD003);

        for (var x = 0; x < 8; x++)
        {
            Assert.Equal(1, PixelAt(emulator, x, 0));
            Assert.Equal(1, PixelAt(emulator, x, 2));
        }
        Assert.Equal(1, PixelAt(emulator, 0, 1));
        Assert.Equal(0, PixelAt(emulator, 1, 1));
        Assert.Equal(1, PixelAt(emulator, 7, 1));
    }

    [Fact]
    public void DrawsAtCoordinatesFromVxVy()
    {
        var emulator = CreateEmulator();
        emulator.WriteMemory(0x300, [0x80]);
        emulator.SetIndexRegister(0xA300);
        emulator.SetRegisterValue(0x620A); // V2 = 10
        emulator.SetRegisterValue(0x6305); // V3 = 5

        emulator.DrawToScreen(0xD231);

        Assert.Equal(1, PixelAt(emulator, 10, 5));
        Assert.Equal(1, CountLitPixels(emulator));
    }

    [Fact]
    public void StartCoordinateIsTakenModuloScreenSize()
    {
        var emulator = CreateEmulator();
        emulator.WriteMemory(0x300, [0x80]);
        emulator.SetIndexRegister(0xA300);
        emulator.SetRegisterValue(0x6246); // V2 = 70 -> x = 70 % 64 = 6
        emulator.SetRegisterValue(0x6328); // V3 = 40 -> y = 40 % 32 = 8

        emulator.DrawToScreen(0xD231);

        Assert.Equal(1, PixelAt(emulator, 6, 8));
        Assert.Equal(1, CountLitPixels(emulator));
    }

    [Fact]
    public void NoCollisionClearsVf()
    {
        var emulator = CreateEmulator();
        emulator.SetRegisterValue(0x6F01); // VF = 1 from some earlier op
        emulator.WriteMemory(0x300, [0xFF]);
        emulator.SetIndexRegister(0xA300);

        emulator.DrawToScreen(0xD001);

        Assert.Equal(0, emulator.ReadRegister(0xF));
    }

    [Fact]
    public void CollisionSetsVfToOne()
    {
        var emulator = CreateEmulator();
        emulator.WriteMemory(0x300, [0xFF]);
        emulator.SetIndexRegister(0xA300);
        emulator.DrawToScreen(0xD001);

        emulator.DrawToScreen(0xD001);

        Assert.Equal(1, emulator.ReadRegister(0xF));
    }

    [Fact]
    public void DrawingSameSpriteTwiceErasesIt()
    {
        var emulator = CreateEmulator();
        emulator.WriteMemory(0x300, [0xFF, 0xFF]);
        emulator.SetIndexRegister(0xA300);
        emulator.DrawToScreen(0xD002);

        emulator.DrawToScreen(0xD002);

        Assert.Equal(0, CountLitPixels(emulator));
    }

    [Fact]
    public void PartialOverlapSetsVfAndLeavesNonOverlappingBits()
    {
        var emulator = CreateEmulator();
        // Row 1: 0xF0 = 11110000
        emulator.WriteMemory(0x300, [0xF0]);
        emulator.SetIndexRegister(0xA300);
        emulator.DrawToScreen(0xD001);

        // Row 2: 0x0F = 00001111 — no overlap with row 1
        emulator.WriteMemory(0x301, [0x0F]);
        emulator.SetIndexRegister(0xA301);
        emulator.DrawToScreen(0xD001);
        Assert.Equal(0, emulator.ReadRegister(0xF)); // no collision

        // Row 3: 0x81 = 10000001 — overlaps both existing bits
        emulator.WriteMemory(0x302, [0x81]);
        emulator.SetIndexRegister(0xA302);
        emulator.DrawToScreen(0xD001);
        Assert.Equal(1, emulator.ReadRegister(0xF)); // collision

        // After row 3 XOR: bits 0 and 7 flipped off, rest unchanged -> 01111110
        Assert.Equal(0, PixelAt(emulator, 0, 0));
        Assert.Equal(1, PixelAt(emulator, 1, 0));
        Assert.Equal(1, PixelAt(emulator, 2, 0));
        Assert.Equal(1, PixelAt(emulator, 3, 0));
        Assert.Equal(1, PixelAt(emulator, 4, 0));
        Assert.Equal(1, PixelAt(emulator, 5, 0));
        Assert.Equal(1, PixelAt(emulator, 6, 0));
        Assert.Equal(0, PixelAt(emulator, 7, 0));
    }

    [Fact]
    public void ClipsAtRightEdge()
    {
        var emulator = CreateEmulator();
        emulator.WriteMemory(0x300, [0xFF]);
        emulator.SetIndexRegister(0xA300);
        emulator.SetRegisterValue(0x623C); // V2 = 60 -> only 4 bits fit on screen
        emulator.SetRegisterValue(0x6300); // V3 = 0

        emulator.DrawToScreen(0xD231);

        for (var x = 60; x < ScreenWidth; x++)
            Assert.Equal(1, PixelAt(emulator, x, 0));
        Assert.Equal(4, CountLitPixels(emulator));
    }

    [Fact]
    public void ClipsAtBottomEdge()
    {
        var emulator = CreateEmulator();
        emulator.WriteMemory(0x300, [0x80, 0x80, 0x80, 0x80]);
        emulator.SetIndexRegister(0xA300);
        emulator.SetRegisterValue(0x6200); // V2 = 0
        emulator.SetRegisterValue(0x631E); // V3 = 30 -> only rows 30,31 fit

        emulator.DrawToScreen(0xD234);

        Assert.Equal(1, PixelAt(emulator, 0, 30));
        Assert.Equal(1, PixelAt(emulator, 0, 31));
        Assert.Equal(2, CountLitPixels(emulator));
    }

    [Fact]
    public void ZeroHeightSpriteDrawsNothingAndClearsVf()
    {
        var emulator = CreateEmulator();
        emulator.SetRegisterValue(0x6F01); // VF = 1
        emulator.WriteMemory(0x300, [0xFF]);
        emulator.SetIndexRegister(0xA300);

        emulator.DrawToScreen(0xD000); // n = 0

        Assert.Equal(0, CountLitPixels(emulator));
        Assert.Equal(0, emulator.ReadRegister(0xF));
    }

    [Fact]
    public void ReadsSpriteFromIndexRegister()
    {
        var emulator = CreateEmulator();
        emulator.WriteMemory(0x050, [0b11000011]);
        emulator.SetIndexRegister(0xA050);

        emulator.DrawToScreen(0xD001);

        Assert.Equal(1, PixelAt(emulator, 0, 0));
        Assert.Equal(1, PixelAt(emulator, 1, 0));
        Assert.Equal(0, PixelAt(emulator, 2, 0));
        Assert.Equal(0, PixelAt(emulator, 3, 0));
        Assert.Equal(0, PixelAt(emulator, 4, 0));
        Assert.Equal(0, PixelAt(emulator, 5, 0));
        Assert.Equal(1, PixelAt(emulator, 6, 0));
        Assert.Equal(1, PixelAt(emulator, 7, 0));
    }
}
