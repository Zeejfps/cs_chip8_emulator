using static Chip8Emulator.Core.Chip8Disassembler;

namespace Chip8Emulator.Core.Routines;

// SUPER-CHIP 1.1 additions: hi-res mode, 4-pixel scrolls, scroll down N,
// 16x16 sprite drawing (DXY0), 10-byte high-res font, persistent user flags.
internal static class SChipRoutines
{
    // ---- 00FF / 00FE : high-res mode toggle ---------------------------------

    public static void EnableHiresMode(Chip8Cpu cpu, int ins)
    {
        cpu.Display.EnableHighResMode();
    }

    public static void DisableHiresMode(Chip8Cpu cpu, int ins)
    {
        cpu.Display.DisableHighResMode();
    }

    // ---- 00FB / 00FC / 00CN : scroll ----------------------------------------

    public static void ScrollRight(Chip8Cpu cpu, int ins)
    {
        cpu.Display.ScrollRight(4);
    }

    public static void ScrollLeft(Chip8Cpu cpu, int ins)
    {
        cpu.Display.ScrollLeft(4);
    }

    public static void ScrollDown(Chip8Cpu cpu, int ins)
    {
        cpu.Display.ScrollDown(ins & 0x0F);
    }

    // ---- DXY0 : 16x16 hi-res sprite -----------------------------------------

    // Called from Chip8Cpu.DrawToScreenIns when the display is in hi-res
    // mode and N == 0. Extended for XO-Chip bitplanes (mask param).
    public static void DrawHighResSprite(Chip8Cpu cpu, int x, int y, byte planeMask)
    {
        // S-CHIP 1.1 DXY0 hi-res collision semantics (extended for XO-Chip bitplanes):
        // VF = number of sprite rows with at least one collision in any selected plane
        //    + number of sprite rows clipped off the bottom edge (when not wrapping).
        cpu.Display.WritePixels(displayPixels =>
        {
            Span<bool> rowCollisions = stackalloc bool[16];
            var anyClipped = 0;
            var spriteBase = 0;

            for (var planeBit = 0; planeBit < 2; planeBit++)
            {
                var planeBitMask = (byte)(1 << planeBit);
                if ((planeMask & planeBitMask) == 0) continue;

                var clipped = DrawHighResPlane(cpu, displayPixels, x, y, spriteBase, planeBitMask, rowCollisions);
                anyClipped = Math.Max(anyClipped, clipped);

                // 16 rows * 2 bytes per row = next plane's sprite data.
                spriteBase += 32;
            }

            var collidedRows = 0;
            for (var i = 0; i < 16; i++)
                if (rowCollisions[i]) collidedRows++;

            cpu.Registers.WriteV(0xF, (byte)(collidedRows + anyClipped));
        });
    }

    private static int DrawHighResPlane(
        Chip8Cpu cpu, Span<byte> displayPixels, int x, int y,
        int spriteBase, byte planeBitMask, Span<bool> rowCollisions)
    {
        var display = cpu.Display;
        var width = display.Width;
        var height = display.Height;
        var wrap = cpu.SpritesWrap;

        for (var i = 0; i < 16; i++)
        {
            var dstY = y + i;
            if (wrap)
            {
                dstY %= height;
            }
            else if (dstY >= height)
            {
                return 16 - i;
            }

            var spriteRow = ReadHighResSpriteRow(cpu, spriteBase + i * 2);
            if (DrawHighResRow(displayPixels, spriteRow, x, dstY, width, wrap, planeBitMask))
                rowCollisions[i] = true;
        }

        return 0;
    }

    private static ushort ReadHighResSpriteRow(Chip8Cpu cpu, int offset)
    {
        var hi = cpu.Memory.Read(cpu.Registers.ReadIWithOffset(offset));
        var lo = cpu.Memory.Read(cpu.Registers.ReadIWithOffset(offset + 1));
        return (ushort)((hi << 8) | lo);
    }

    private static bool DrawHighResRow(
        Span<byte> displayPixels, ushort spriteRow, int x, int dstY,
        int width, bool wrap, byte planeBitMask)
    {
        var collided = false;
        for (var bit = 0; bit < 16; bit++)
        {
            var dstX = x + bit;
            if (wrap) dstX %= width;
            else if (dstX >= width) break;

            var spriteBitOn = ((spriteRow >> (15 - bit)) & 1) != 0;
            if (!spriteBitOn) continue;

            var dstIndex = dstY * width + dstX;
            var before = displayPixels[dstIndex];
            if ((before & planeBitMask) != 0) collided = true;
            displayPixels[dstIndex] = (byte)(before ^ planeBitMask);
        }
        return collided;
    }

    // ---- FX30 : load hi-res font character ----------------------------------

    public static void LoadHighResFontCharacter(Chip8Cpu cpu, int ins)
    {
        var x = ExtractX(ins);
        var value = cpu.Registers.ReadV(x);
        cpu.Registers.WriteI((value & 0x0F) * Chip8Interpreter.HighRestFontCharWidth + Chip8Interpreter.HighResFontBaseAddress);
    }

    // ---- FX75 / FX85 : persistent user flags --------------------------------

    public static void SaveFlags(Chip8Cpu cpu, int ins)
    {
        cpu.SaveFlags(ExtractX(ins));
    }

    public static void LoadFlags(Chip8Cpu cpu, int ins)
    {
        cpu.LoadFlags(ExtractX(ins));
    }
}
