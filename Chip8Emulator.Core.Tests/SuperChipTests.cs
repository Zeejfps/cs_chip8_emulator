using Chip8Emulator.Core.Routines;
using Chip8Emulator.Core.Tests.Fakes;

namespace Chip8Emulator.Core.Tests;

public class SuperChipTests
{
    private const int LowResWidth = 64;
    private const int LowResHeight = 32;
    private const int HighResWidth = 128;
    private const int HighResHeight = 64;

    private static Chip8Machine CreateEmulator()
        => new(new FakeRenderer(), new FakeAudio(), new FakeClock(), new FakeInput());

    private static byte PixelAt(Chip8Machine emulator, int x, int y)
        => emulator.Display.Pixels.Span[y * emulator.Display.Width + x];

    private static int CountLitPixels(Chip8Machine emulator)
    {
        var count = 0;
        foreach (var p in emulator.Display.Pixels.Span)
            if (p == 1) count++;
        return count;
    }

    // ---- 00FF / 00FE : high-res mode toggle ----------------------------------

    [Fact]
    public void EnableHighResModeIns_SetsDisplayTo128x64()
    {
        var emulator = CreateEmulator();

        emulator.SystemRoutines[0x00FF & 0x00FF](emulator, 0x00FF);

        Assert.Equal(HighResWidth, emulator.Display.Width);
        Assert.Equal(HighResHeight, emulator.Display.Height);
    }

    [Fact]
    public void DisableHighResModeIns_SetsDisplayTo64x32()
    {
        var emulator = CreateEmulator();
        emulator.SystemRoutines[0x00FF & 0x00FF](emulator, 0x00FF);

        emulator.SystemRoutines[0x00FE & 0x00FF](emulator, 0x00FE);

        Assert.Equal(LowResWidth, emulator.Display.Width);
        Assert.Equal(LowResHeight, emulator.Display.Height);
    }

    // ---- 00CN : scroll down N rows ------------------------------------------

    [Fact]
    public void ScrollDownIns_MovesPixelsDownByNRows()
    {
        var emulator = CreateEmulator();
        emulator.Memory.Write(0x300, [0xFF]);
        Chip8Routines.SetIndexRegisterIns(emulator, 0xA300);
        Chip8Routines.DrawToScreen(emulator, 0xD001); // row 8 lit pixels at y=0

        emulator.SystemRoutines[0x00C3 & 0x00FF](emulator, 0x00C3); // scroll down 3

        for (var x = 0; x < 8; x++)
        {
            Assert.Equal(0, PixelAt(emulator, x, 0));
            Assert.Equal(0, PixelAt(emulator, x, 1));
            Assert.Equal(0, PixelAt(emulator, x, 2));
            Assert.Equal(1, PixelAt(emulator, x, 3));
        }
        Assert.Equal(8, CountLitPixels(emulator));
    }

    [Fact]
    public void ScrollDownIns_ClearsTopNRows()
    {
        var emulator = CreateEmulator();
        emulator.Memory.Write(0x300, [0xFF]);
        Chip8Routines.SetIndexRegisterIns(emulator, 0xA300);
        Chip8Routines.DrawToScreen(emulator, 0xD001);

        emulator.SystemRoutines[0x00C2 & 0x00FF](emulator, 0x00C2);

        for (var x = 0; x < 8; x++)
        {
            Assert.Equal(0, PixelAt(emulator, x, 0));
            Assert.Equal(0, PixelAt(emulator, x, 1));
        }
    }

    [Fact]
    public void ScrollDownIns_WithZeroIsNoOp()
    {
        var emulator = CreateEmulator();
        emulator.Memory.Write(0x300, [0xFF]);
        Chip8Routines.SetIndexRegisterIns(emulator, 0xA300);
        Chip8Routines.DrawToScreen(emulator, 0xD001);

        emulator.SystemRoutines[0x00C0 & 0x00FF](emulator, 0x00C0);

        for (var x = 0; x < 8; x++)
            Assert.Equal(1, PixelAt(emulator, x, 0));
    }

    // ---- 00DN : scroll up N rows (XO-CHIP) ----------------------------------

    [Fact]
    public void ScrollUpIns_MovesPixelsUpByNRows()
    {
        var emulator = CreateEmulator();
        emulator.Memory.Write(0x300, [0xFF]);
        Chip8Routines.SetIndexRegisterIns(emulator, 0xA300);
        Chip8Routines.SetRegisterValue(emulator, 0x6000); // V0 = 0
        Chip8Routines.SetRegisterValue(emulator, 0x6105); // V1 = 5 -> draw at y=5
        Chip8Routines.DrawToScreen(emulator, 0xD011);

        emulator.SystemRoutines[0x00D3 & 0x00FF](emulator, 0x00D3); // scroll up 3

        for (var x = 0; x < 8; x++)
        {
            Assert.Equal(1, PixelAt(emulator, x, 2));
            Assert.Equal(0, PixelAt(emulator, x, 3));
            Assert.Equal(0, PixelAt(emulator, x, 4));
            Assert.Equal(0, PixelAt(emulator, x, 5));
        }
        Assert.Equal(8, CountLitPixels(emulator));
    }

    [Fact]
    public void ScrollUpIns_ClearsBottomNRows()
    {
        var emulator = CreateEmulator();
        emulator.Memory.Write(0x300, [0xFF]);
        Chip8Routines.SetIndexRegisterIns(emulator, 0xA300);
        Chip8Routines.SetRegisterValue(emulator, 0x6000); // V0 = 0
        Chip8Routines.SetRegisterValue(emulator, 0x611F); // V1 = 31 -> bottom row of 64x32
        Chip8Routines.DrawToScreen(emulator, 0xD011);

        emulator.SystemRoutines[0x00D2 & 0x00FF](emulator, 0x00D2); // scroll up 2

        for (var x = 0; x < LowResWidth; x++)
        {
            Assert.Equal(0, PixelAt(emulator, x, LowResHeight - 1));
            Assert.Equal(0, PixelAt(emulator, x, LowResHeight - 2));
        }
    }

    [Fact]
    public void ScrollUpIns_WithZeroIsNoOp()
    {
        var emulator = CreateEmulator();
        emulator.Memory.Write(0x300, [0xFF]);
        Chip8Routines.SetIndexRegisterIns(emulator, 0xA300);
        Chip8Routines.DrawToScreen(emulator, 0xD001); // row of 8 lit at y=0

        emulator.SystemRoutines[0x00D0 & 0x00FF](emulator, 0x00D0);

        for (var x = 0; x < 8; x++)
            Assert.Equal(1, PixelAt(emulator, x, 0));
    }

    // ---- 00FB : scroll right 4 pixels ---------------------------------------

    [Fact]
    public void ScrollRightIns_MovesPixelsRightBy4()
    {
        var emulator = CreateEmulator();
        emulator.Memory.Write(0x300, [0xFF]);
        Chip8Routines.SetIndexRegisterIns(emulator, 0xA300);
        Chip8Routines.DrawToScreen(emulator, 0xD001); // x=0..7 lit at y=0

        emulator.SystemRoutines[0x00FB & 0x00FF](emulator, 0x00FB);

        for (var x = 0; x < 4; x++)
            Assert.Equal(0, PixelAt(emulator, x, 0));
        for (var x = 4; x < 12; x++)
            Assert.Equal(1, PixelAt(emulator, x, 0));
        Assert.Equal(8, CountLitPixels(emulator));
    }

    [Fact]
    public void ScrollRightIns_PixelsFallingOffRightEdgeAreLost()
    {
        var emulator = CreateEmulator();
        emulator.Memory.Write(0x300, [0xFF]);
        Chip8Routines.SetIndexRegisterIns(emulator, 0xA300);
        Chip8Routines.SetRegisterValue(emulator, 0x6038); // V0 = 56 -> sprite at x=56..63
        Chip8Routines.SetRegisterValue(emulator, 0x6100); // V1 = 0
        Chip8Routines.DrawToScreen(emulator, 0xD011);

        emulator.SystemRoutines[0x00FB & 0x00FF](emulator, 0x00FB);

        // Four rightmost sprite pixels fall off; remaining four are at x=60..63
        Assert.Equal(4, CountLitPixels(emulator));
        for (var x = 60; x < LowResWidth; x++)
            Assert.Equal(1, PixelAt(emulator, x, 0));
    }

    // ---- 00FC : scroll left 4 pixels ----------------------------------------

    [Fact]
    public void ScrollLeftIns_MovesPixelsLeftBy4()
    {
        var emulator = CreateEmulator();
        emulator.Memory.Write(0x300, [0xFF]);
        Chip8Routines.SetIndexRegisterIns(emulator, 0xA300);
        Chip8Routines.SetRegisterValue(emulator, 0x6008); // V0 = 8
        Chip8Routines.SetRegisterValue(emulator, 0x6100); // V1 = 0
        Chip8Routines.DrawToScreen(emulator, 0xD011); // x=8..15 lit at y=0

        emulator.SystemRoutines[0x00FC & 0x00FF](emulator, 0x00FC);

        for (var x = 4; x < 12; x++)
            Assert.Equal(1, PixelAt(emulator, x, 0));
        for (var x = 12; x < 16; x++)
            Assert.Equal(0, PixelAt(emulator, x, 0));
        Assert.Equal(8, CountLitPixels(emulator));
    }

    [Fact]
    public void ScrollLeftIns_PixelsFallingOffLeftEdgeAreLost()
    {
        var emulator = CreateEmulator();
        emulator.Memory.Write(0x300, [0xFF]);
        Chip8Routines.SetIndexRegisterIns(emulator, 0xA300);
        Chip8Routines.DrawToScreen(emulator, 0xD001); // x=0..7 at y=0

        emulator.SystemRoutines[0x00FC & 0x00FF](emulator, 0x00FC);

        Assert.Equal(4, CountLitPixels(emulator));
        for (var x = 0; x < 4; x++)
            Assert.Equal(1, PixelAt(emulator, x, 0));
    }

    // ---- FX30 : load high-res (10-byte) font character ----------------------

    [Fact]
    public void LoadHighResFontCharacterIns_SetsIndexToCharAddress()
    {
        var emulator = CreateEmulator();
        Chip8Routines.SetRegisterValue(emulator, 0x6005); // V0 = 5

        emulator.TimerRoutines[0xF030 & 0x00FF](emulator, 0xF030);

        // High-res font base 0x0A0, each glyph is 10 bytes -> 0xA0 + 5*10 = 0xD2
        Assert.Equal(0xA0 + 5 * 10, emulator.Debugger.IndexRegister);
    }

    [Fact]
    public void LoadHighResFontCharacterIns_CharZeroStartsAtFontBase()
    {
        var emulator = CreateEmulator();
        Chip8Routines.SetRegisterValue(emulator, 0x6200); // V2 = 0

        emulator.TimerRoutines[0xF230 & 0x00FF](emulator, 0xF230);

        Assert.Equal(0xA0, emulator.Debugger.IndexRegister);
    }

    // ---- DXY0 : 16x16 high-res sprite drawing -------------------------------

    [Fact]
    public void DxY0_DrawsSixteenBySixteenSprite()
    {
        var emulator = CreateEmulator();
        emulator.SystemRoutines[0x00FF & 0x00FF](emulator, 0x00FF); // enable high-res

        var sprite = new byte[32];
        for (var i = 0; i < 32; i++) sprite[i] = 0xFF; // all bits set
        emulator.Memory.Write(0x300, sprite);
        Chip8Routines.SetIndexRegisterIns(emulator, 0xA300);

        Chip8Routines.DrawToScreen(emulator, 0xD000);

        for (var y = 0; y < 16; y++)
            for (var x = 0; x < 16; x++)
                Assert.Equal(1, PixelAt(emulator, x, y));
        Assert.Equal(256, CountLitPixels(emulator));
    }

    [Fact]
    public void DxY0_EachRowUsesTwoBytes()
    {
        var emulator = CreateEmulator();
        emulator.SystemRoutines[0x00FF & 0x00FF](emulator, 0x00FF);

        // Row 0: 0x8000 (bit 15 set) — leftmost pixel only.
        // Row 1: 0x0001 (bit 0 set) — rightmost pixel only.
        var sprite = new byte[32];
        sprite[0] = 0x80; sprite[1] = 0x00;
        sprite[2] = 0x00; sprite[3] = 0x01;
        emulator.Memory.Write(0x300, sprite);
        Chip8Routines.SetIndexRegisterIns(emulator, 0xA300);

        Chip8Routines.DrawToScreen(emulator, 0xD000);

        Assert.Equal(1, PixelAt(emulator, 0, 0));
        for (var x = 1; x < 16; x++) Assert.Equal(0, PixelAt(emulator, x, 0));

        for (var x = 0; x < 15; x++) Assert.Equal(0, PixelAt(emulator, x, 1));
        Assert.Equal(1, PixelAt(emulator, 15, 1));

        Assert.Equal(2, CountLitPixels(emulator));
    }

    [Fact]
    public void DxY0_ExtractsAllBitsLeftToRight()
    {
        var emulator = CreateEmulator();
        emulator.SystemRoutines[0x00FF & 0x00FF](emulator, 0x00FF);

        // Single row: 0xA55A = 1010 0101 0101 1010
        var sprite = new byte[32];
        sprite[0] = 0xA5; sprite[1] = 0x5A;
        emulator.Memory.Write(0x300, sprite);
        Chip8Routines.SetIndexRegisterIns(emulator, 0xA300);

        Chip8Routines.DrawToScreen(emulator, 0xD000);

        int[] expected = [1, 0, 1, 0, 0, 1, 0, 1, 0, 1, 0, 1, 1, 0, 1, 0];
        for (var bit = 0; bit < 16; bit++)
            Assert.Equal(expected[bit], PixelAt(emulator, bit, 0));
    }

    [Fact]
    public void DxY0_DrawsAtVxVyCoordinates()
    {
        var emulator = CreateEmulator();
        emulator.SystemRoutines[0x00FF & 0x00FF](emulator, 0x00FF);

        var sprite = new byte[32];
        sprite[0] = 0x80; sprite[1] = 0x00;
        emulator.Memory.Write(0x300, sprite);
        Chip8Routines.SetIndexRegisterIns(emulator, 0xA300);
        Chip8Routines.SetRegisterValue(emulator, 0x6314); // V3 = 20
        Chip8Routines.SetRegisterValue(emulator, 0x640A); // V4 = 10

        Chip8Routines.DrawToScreen(emulator, 0xD340);

        Assert.Equal(1, PixelAt(emulator, 20, 10));
        Assert.Equal(1, CountLitPixels(emulator));
    }

    [Fact]
    public void DxY0_ClipsAtBottomEdge()
    {
        var emulator = CreateEmulator();
        emulator.SystemRoutines[0x00FF & 0x00FF](emulator, 0x00FF);

        var sprite = new byte[32];
        for (var i = 0; i < 32; i++) sprite[i] = 0xFF;
        emulator.Memory.Write(0x300, sprite);
        Chip8Routines.SetIndexRegisterIns(emulator, 0xA300);
        Chip8Routines.SetRegisterValue(emulator, 0x6000); // V0 = 0
        Chip8Routines.SetRegisterValue(emulator, 0x6138); // V1 = 56 -> only 8 rows fit

        Chip8Routines.DrawToScreen(emulator, 0xD010);

        Assert.Equal(16 * 8, CountLitPixels(emulator));
    }

    [Fact]
    public void DxY0_ClipsAtRightEdge()
    {
        var emulator = CreateEmulator();
        emulator.SystemRoutines[0x00FF & 0x00FF](emulator, 0x00FF);

        var sprite = new byte[32];
        for (var i = 0; i < 32; i++) sprite[i] = 0xFF;
        emulator.Memory.Write(0x300, sprite);
        Chip8Routines.SetIndexRegisterIns(emulator, 0xA300);
        Chip8Routines.SetRegisterValue(emulator, 0x6078); // V0 = 120 -> only 8 cols fit
        Chip8Routines.SetRegisterValue(emulator, 0x6100); // V1 = 0

        Chip8Routines.DrawToScreen(emulator, 0xD010);

        Assert.Equal(16 * 8, CountLitPixels(emulator));
    }

    [Fact]
    public void DxY0_NoCollisionClearsVf()
    {
        var emulator = CreateEmulator();
        emulator.SystemRoutines[0x00FF & 0x00FF](emulator, 0x00FF);
        Chip8Routines.SetRegisterValue(emulator, 0x6F01); // dirty VF

        var sprite = new byte[32];
        for (var i = 0; i < 32; i++) sprite[i] = 0xFF;
        emulator.Memory.Write(0x300, sprite);
        Chip8Routines.SetIndexRegisterIns(emulator, 0xA300);

        Chip8Routines.DrawToScreen(emulator, 0xD000);

        Assert.Equal(0, emulator.Debugger.Registers[0xF]);
    }

    [Fact]
    public void DxY0_VfIsNumberOfCollidingRows()
    {
        var emulator = CreateEmulator();
        emulator.SystemRoutines[0x00FF & 0x00FF](emulator, 0x00FF);

        var sprite = new byte[32];
        for (var i = 0; i < 32; i++) sprite[i] = 0xFF;
        emulator.Memory.Write(0x300, sprite);
        Chip8Routines.SetIndexRegisterIns(emulator, 0xA300);
        Chip8Routines.DrawToScreen(emulator, 0xD000);

        Chip8Routines.DrawToScreen(emulator, 0xD000);

        // All 16 rows of an all-on sprite collide on second draw.
        Assert.Equal(16, emulator.Debugger.Registers[0xF]);
    }

    [Fact]
    public void DxY0_VfCountsBottomClippedRows()
    {
        var emulator = CreateEmulator();
        emulator.SystemRoutines[0x00FF & 0x00FF](emulator, 0x00FF);

        var sprite = new byte[32];
        for (var i = 0; i < 32; i++) sprite[i] = 0xFF;
        emulator.Memory.Write(0x300, sprite);
        Chip8Routines.SetIndexRegisterIns(emulator, 0xA300);
        Chip8Routines.SetRegisterValue(emulator, 0x6000); // V0 = 0
        Chip8Routines.SetRegisterValue(emulator, 0x6138); // V1 = 56 -> 8 rows fit, 8 clipped

        Chip8Routines.DrawToScreen(emulator, 0xD010);

        // No on-screen collision (fresh display), but 8 rows clipped off the bottom.
        Assert.Equal(8, emulator.Debugger.Registers[0xF]);
    }

    [Fact]
    public void DxY0_VfIsCollidingRowsPlusClippedRows()
    {
        var emulator = CreateEmulator();
        emulator.SystemRoutines[0x00FF & 0x00FF](emulator, 0x00FF);

        var sprite = new byte[32];
        for (var i = 0; i < 32; i++) sprite[i] = 0xFF;
        emulator.Memory.Write(0x300, sprite);
        Chip8Routines.SetIndexRegisterIns(emulator, 0xA300);
        Chip8Routines.SetRegisterValue(emulator, 0x6000); // V0 = 0
        Chip8Routines.SetRegisterValue(emulator, 0x6138); // V1 = 56 -> 8 rows fit, 8 clipped

        Chip8Routines.DrawToScreen(emulator, 0xD010);
        Chip8Routines.DrawToScreen(emulator, 0xD010);

        // 8 rows collide on second draw + 8 rows clipped off the bottom.
        Assert.Equal(16, emulator.Debugger.Registers[0xF]);
    }
}
