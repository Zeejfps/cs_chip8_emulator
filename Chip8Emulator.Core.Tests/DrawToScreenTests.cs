using Chip8Emulator.Core.Routines;
using Chip8Emulator.Core.Tests.Fakes;

namespace Chip8Emulator.Core.Tests;

public class DrawToScreenTests
{
    private const int ScreenWidth = 64;
    private const int ScreenHeight = 32;

    private readonly byte[] _pixelBuffer = new byte[EmulatedDisplay.HighRestWidth * EmulatedDisplay.HighRestHeight];

    private Chip8Machine CreateEmulator()
        => new(new EmulatedDisplay(size => _pixelBuffer.AsMemory(0, size)),
            new FakeAudio(), new FakeClock(), new FakeInput(),
            new EmulatedStack(size => new int[size]),
            new EmulatedMemory(size => new byte[size]),
            new EmulatedRegisters(size => new byte[size]),
            new EmulatedPersistentFlags());

    private byte PixelAt(Chip8Machine emulator, int x, int y)
        => _pixelBuffer[y * ScreenWidth + x];

    private int CountLitPixels(Chip8Machine emulator)
    {
        var count = 0;
        foreach (var p in _pixelBuffer)
            if (p == 1) count++;
        return count;
    }

    [Fact]
    public void DrawsSpriteBitsIntoFramebuffer()
    {
        var emulator = CreateEmulator();
        emulator.Memory.Write(0x300, [0b10100101]);
        Chip8Routines.SetIndexRegisterIns(emulator, 0xA300);

        Chip8Routines.DrawToScreen(emulator, 0xD001);

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
        emulator.Memory.Write(0x300, [0xFF, 0x81, 0xFF]);
        Chip8Routines.SetIndexRegisterIns(emulator, 0xA300);

        Chip8Routines.DrawToScreen(emulator, 0xD003);

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
        emulator.Memory.Write(0x300, [0x80]);
        Chip8Routines.SetIndexRegisterIns(emulator, 0xA300);
        Chip8Routines.SetRegisterValue(emulator, 0x620A); // V2 = 10
        Chip8Routines.SetRegisterValue(emulator, 0x6305); // V3 = 5

        Chip8Routines.DrawToScreen(emulator, 0xD231);

        Assert.Equal(1, PixelAt(emulator, 10, 5));
        Assert.Equal(1, CountLitPixels(emulator));
    }

    [Fact]
    public void StartCoordinateIsTakenModuloScreenSize()
    {
        var emulator = CreateEmulator();
        emulator.Memory.Write(0x300, [0x80]);
        Chip8Routines.SetIndexRegisterIns(emulator, 0xA300);
        Chip8Routines.SetRegisterValue(emulator, 0x6246); // V2 = 70 -> x = 70 % 64 = 6
        Chip8Routines.SetRegisterValue(emulator, 0x6328); // V3 = 40 -> y = 40 % 32 = 8

        Chip8Routines.DrawToScreen(emulator, 0xD231);

        Assert.Equal(1, PixelAt(emulator, 6, 8));
        Assert.Equal(1, CountLitPixels(emulator));
    }

    [Fact]
    public void NoCollisionClearsVf()
    {
        var emulator = CreateEmulator();
        Chip8Routines.SetRegisterValue(emulator, 0x6F01); // VF = 1 from some earlier op
        emulator.Memory.Write(0x300, [0xFF]);
        Chip8Routines.SetIndexRegisterIns(emulator, 0xA300);

        Chip8Routines.DrawToScreen(emulator, 0xD001);

        Assert.Equal(0, emulator.Registers.ReadV(0xF));
    }

    [Fact]
    public void CollisionSetsVfToOne()
    {
        var emulator = CreateEmulator();
        emulator.Memory.Write(0x300, [0xFF]);
        Chip8Routines.SetIndexRegisterIns(emulator, 0xA300);
        Chip8Routines.DrawToScreen(emulator, 0xD001);

        Chip8Routines.DrawToScreen(emulator, 0xD001);

        Assert.Equal(1, emulator.Registers.ReadV(0xF));
    }

    [Fact]
    public void DrawingSameSpriteTwiceErasesIt()
    {
        var emulator = CreateEmulator();
        emulator.Memory.Write(0x300, [0xFF, 0xFF]);
        Chip8Routines.SetIndexRegisterIns(emulator, 0xA300);
        Chip8Routines.DrawToScreen(emulator, 0xD002);

        Chip8Routines.DrawToScreen(emulator, 0xD002);

        Assert.Equal(0, CountLitPixels(emulator));
    }

    [Fact]
    public void PartialOverlapSetsVfAndLeavesNonOverlappingBits()
    {
        var emulator = CreateEmulator();
        // Row 1: 0xF0 = 11110000
        emulator.Memory.Write(0x300, [0xF0]);
        Chip8Routines.SetIndexRegisterIns(emulator, 0xA300);
        Chip8Routines.DrawToScreen(emulator, 0xD001);

        // Row 2: 0x0F = 00001111 — no overlap with row 1
        emulator.Memory.Write(0x301, [0x0F]);
        Chip8Routines.SetIndexRegisterIns(emulator, 0xA301);
        Chip8Routines.DrawToScreen(emulator, 0xD001);
        Assert.Equal(0, emulator.Registers.ReadV(0xF)); // no collision

        // Row 3: 0x81 = 10000001 — overlaps both existing bits
        emulator.Memory.Write(0x302, [0x81]);
        Chip8Routines.SetIndexRegisterIns(emulator, 0xA302);
        Chip8Routines.DrawToScreen(emulator, 0xD001);
        Assert.Equal(1, emulator.Registers.ReadV(0xF)); // collision

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
        emulator.Memory.Write(0x300, [0xFF]);
        Chip8Routines.SetIndexRegisterIns(emulator, 0xA300);
        Chip8Routines.SetRegisterValue(emulator, 0x623C); // V2 = 60 -> only 4 bits fit on screen
        Chip8Routines.SetRegisterValue(emulator, 0x6300); // V3 = 0

        Chip8Routines.DrawToScreen(emulator, 0xD231);

        for (var x = 60; x < ScreenWidth; x++)
            Assert.Equal(1, PixelAt(emulator, x, 0));
        Assert.Equal(4, CountLitPixels(emulator));
    }

    [Fact]
    public void ClipsAtBottomEdge()
    {
        var emulator = CreateEmulator();
        emulator.Memory.Write(0x300, [0x80, 0x80, 0x80, 0x80]);
        Chip8Routines.SetIndexRegisterIns(emulator, 0xA300);
        Chip8Routines.SetRegisterValue(emulator, 0x6200); // V2 = 0
        Chip8Routines.SetRegisterValue(emulator, 0x631E); // V3 = 30 -> only rows 30,31 fit

        Chip8Routines.DrawToScreen(emulator, 0xD234);

        Assert.Equal(1, PixelAt(emulator, 0, 30));
        Assert.Equal(1, PixelAt(emulator, 0, 31));
        Assert.Equal(2, CountLitPixels(emulator));
    }

    [Fact]
    public void WrapsAtRightEdgeWhenSpritesWrapIsOn()
    {
        var emulator = CreateEmulator();
        emulator.SpritesWrap = true;
        emulator.Memory.Write(0x300, [0xFF]);
        Chip8Routines.SetIndexRegisterIns(emulator, 0xA300);
        Chip8Routines.SetRegisterValue(emulator, 0x623C); // V2 = 60 -> bits 0..3 at x=60..63, bits 4..7 wrap to x=0..3
        Chip8Routines.SetRegisterValue(emulator, 0x6300); // V3 = 0

        Chip8Routines.DrawToScreen(emulator, 0xD231);

        for (var x = 60; x < ScreenWidth; x++)
            Assert.Equal(1, PixelAt(emulator, x, 0));
        for (var x = 0; x < 4; x++)
            Assert.Equal(1, PixelAt(emulator, x, 0));
        Assert.Equal(8, CountLitPixels(emulator));
    }

    [Fact]
    public void WrapsAtBottomEdgeWhenSpritesWrapIsOn()
    {
        var emulator = CreateEmulator();
        emulator.SpritesWrap = true;
        emulator.Memory.Write(0x300, [0x80, 0x80, 0x80, 0x80]);
        Chip8Routines.SetIndexRegisterIns(emulator, 0xA300);
        Chip8Routines.SetRegisterValue(emulator, 0x6200); // V2 = 0
        Chip8Routines.SetRegisterValue(emulator, 0x631E); // V3 = 30 -> rows at y=30,31, then wrap to y=0,1

        Chip8Routines.DrawToScreen(emulator, 0xD234);

        Assert.Equal(1, PixelAt(emulator, 0, 30));
        Assert.Equal(1, PixelAt(emulator, 0, 31));
        Assert.Equal(1, PixelAt(emulator, 0, 0));
        Assert.Equal(1, PixelAt(emulator, 0, 1));
        Assert.Equal(4, CountLitPixels(emulator));
    }

    [Fact]
    public void WrapsAtBothEdgesSimultaneouslyWhenSpritesWrapIsOn()
    {
        var emulator = CreateEmulator();
        emulator.SpritesWrap = true;
        emulator.Memory.Write(0x300, [0b11000000, 0b11000000]); // 2x2 block in top-left of sprite
        Chip8Routines.SetIndexRegisterIns(emulator, 0xA300);
        Chip8Routines.SetRegisterValue(emulator, 0x623F); // V2 = 63 -> x=63, then wrap to 0
        Chip8Routines.SetRegisterValue(emulator, 0x631F); // V3 = 31 -> y=31, then wrap to 0

        Chip8Routines.DrawToScreen(emulator, 0xD232);

        Assert.Equal(1, PixelAt(emulator, 63, 31));
        Assert.Equal(1, PixelAt(emulator, 0, 31));
        Assert.Equal(1, PixelAt(emulator, 63, 0));
        Assert.Equal(1, PixelAt(emulator, 0, 0));
        Assert.Equal(4, CountLitPixels(emulator));
    }

    [Fact]
    public void WrappedPixelSetsCollisionVf()
    {
        var emulator = CreateEmulator();
        emulator.SpritesWrap = true;
        emulator.Memory.Write(0x300, [0x80]); // single lit pixel at sprite x=0
        Chip8Routines.SetIndexRegisterIns(emulator, 0xA300);

        // First draw: lights pixel at (0,0)
        Chip8Routines.DrawToScreen(emulator, 0xD001);
        Assert.Equal(0, emulator.Registers.ReadV(0xF));

        // Second draw from x=64: wraps to x=0 -> collides with (0,0)
        Chip8Routines.SetRegisterValue(emulator, 0x6240); // V2 = 64 -> wraps to 0
        Chip8Routines.SetRegisterValue(emulator, 0x6300);
        Chip8Routines.DrawToScreen(emulator, 0xD231);

        Assert.Equal(1, emulator.Registers.ReadV(0xF));
        Assert.Equal(0, CountLitPixels(emulator)); // XOR erased it
    }

    [Fact]
    public void SpritesWrapDefaultsToFalseAndClipsAtRightEdge()
    {
        var emulator = CreateEmulator();
        Assert.False(emulator.SpritesWrap);

        emulator.Memory.Write(0x300, [0xFF]);
        Chip8Routines.SetIndexRegisterIns(emulator, 0xA300);
        Chip8Routines.SetRegisterValue(emulator, 0x623C); // V2 = 60
        Chip8Routines.SetRegisterValue(emulator, 0x6300);

        Chip8Routines.DrawToScreen(emulator, 0xD231);

        // Only 4 pixels fit; bits 4..7 should NOT wrap to x=0..3
        for (var x = 0; x < 4; x++)
            Assert.Equal(0, PixelAt(emulator, x, 0));
        Assert.Equal(4, CountLitPixels(emulator));
    }

    [Fact]
    public void ReadsSpriteFromIndexRegister()
    {
        var emulator = CreateEmulator();
        emulator.Memory.Write(0x050, [0b11000011]);
        Chip8Routines.SetIndexRegisterIns(emulator, 0xA050);

        Chip8Routines.DrawToScreen(emulator, 0xD001);

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
