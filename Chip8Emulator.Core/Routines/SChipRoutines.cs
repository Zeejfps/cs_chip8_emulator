using static Chip8Emulator.Core.Chip8Disassembler;

namespace Chip8Emulator.Core.Routines;

// SUPER-CHIP 1.1 additions: hi-res mode, 4-pixel scrolls, scroll down N,
// 16x16 sprite drawing (DXY0), 10-byte high-res font, persistent user flags.
internal static class SChipRoutines
{
    // ---- 00FF / 00FE : high-res mode toggle ---------------------------------

    public static void EnableHiresMode(EmulatedCpu cpu, int ins)
    {
        cpu.Display.EnableHighResMode();
    }

    public static void DisableHiresMode(EmulatedCpu cpu, int ins)
    {
        cpu.Display.DisableHighResMode();
    }

    // ---- 00FB / 00FC / 00CN : scroll ----------------------------------------

    public static void ScrollRight(EmulatedCpu cpu, int ins)
    {
        cpu.Display.ScrollRight(4);
    }

    public static void ScrollLeft(EmulatedCpu cpu, int ins)
    {
        cpu.Display.ScrollLeft(4);
    }

    public static void ScrollDown(EmulatedCpu cpu, int ins)
    {
        cpu.Display.ScrollDown(ins & 0x0F);
    }

    // ---- DXY0 : 16x16 hi-res sprite -----------------------------------------

    // Called from Chip8Cpu.ExeuteDrawToScreenIns when the display is in hi-res
    // mode and N == 0. Extended for XO-Chip bitplanes (mask param).
    public static void DrawHighResSprite(EmulatedCpu cpu, int x, int y, byte planeMask)
    {
        // S-CHIP 1.1 DXY0 hi-res collision semantics (extended for XO-Chip bitplanes):
        // VF = number of sprite rows with at least one collision in any selected plane
        //    + number of sprite rows clipped off the bottom edge (when not wrapping).
        var display = cpu.Display;
        display.WritePixels(displayPixels =>
        {
            var width = display.Width;
            var height = display.Height;
            var wrap = cpu.SpritesWrap;
            var collidedRows = 0;
            var clippedRows = 0;

            // Rows-per-plane: 16 for a single plane, 32 total when both planes selected
            // (first 32 bytes = plane 0, next 32 = plane 1).
            var planeStride = 32;
            var spriteBase = 0;

            Span<bool> rowCollisions = stackalloc bool[16];
            var anyClipped = 0;

            for (var planeBit = 0; planeBit < 2; planeBit++)
            {
                var planeBitMask = (byte)(1 << planeBit);
                if ((planeMask & planeBitMask) == 0) continue;

                for (var i = 0; i < 16; i++)
                {
                    var dstY = y + i;
                    if (wrap)
                    {
                        dstY %= height;
                    }
                    else if (dstY >= height)
                    {
                        anyClipped = Math.Max(anyClipped, 16 - i);
                        break;
                    }

                    var offset = spriteBase + i * 2;
                    var spritePixelsRow = (ushort)(cpu.Memory.Read(cpu.Registers.ReadIWithOffset(offset)) << 8 |
                                                   cpu.Memory.Read(cpu.Registers.ReadIWithOffset(offset + 1)));
                    for (var bit = 0; bit < 16; bit++)
                    {
                        var dstX = x + bit;
                        if (wrap) dstX %= width;
                        else if (dstX >= width) break;

                        var spriteBitOn = ((spritePixelsRow >> (15 - bit)) & 1) != 0;
                        if (!spriteBitOn) continue;

                        var dstIndex = dstY * width + dstX;
                        var before = displayPixels[dstIndex];
                        if ((before & planeBitMask) != 0) rowCollisions[i] = true;
                        displayPixels[dstIndex] = (byte)(before ^ planeBitMask);
                    }
                }

                spriteBase += planeStride;
            }

            for (var i = 0; i < 16; i++)
                if (rowCollisions[i]) collidedRows++;
            
            clippedRows = anyClipped;
            cpu.Registers.WriteV(0xF, (byte)(collidedRows + clippedRows));
        });
    }

    // ---- FX30 : load hi-res font character ----------------------------------

    public static void LoadHighResFontCharacter(EmulatedCpu cpu, int ins)
    {
        var x = ExtractX(ins);
        var value = cpu.Registers.ReadV(x);
        cpu.Registers.WriteI((value & 0x0F) * Chip8Machine.HighRestFontCharWidth + Chip8Machine.HighResFontBaseAddress);
    }

    // ---- FX75 / FX85 : persistent user flags --------------------------------

    public static void SaveFlags(EmulatedCpu cpu, int ins)
    {
        cpu.SaveFlags(ExtractX(ins));
    }

    public static void LoadFlags(EmulatedCpu cpu, int ins)
    {
        cpu.LoadFlags(ExtractX(ins));
    }
}
