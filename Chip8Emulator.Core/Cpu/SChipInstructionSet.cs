using static Chip8Emulator.Core.Chip8Disassembler;

namespace Chip8Emulator.Core.Cpu;

// SUPER-CHIP 1.1 additions: hi-res mode, 4-pixel scrolls, scroll down N,
// 16x16 sprite drawing (DXY0), 10-byte high-res font, persistent user flags.
internal static class SChipInstructionSet
{
    // ---- 00FF / 00FE : high-res mode toggle ---------------------------------

    public static void ExecuteEnableHiresModeIns(Chip8Machine machine, int ins)
    {
        machine.EnableHighResMode();
    }

    public static void ExecuteDisableHiresModeIns(Chip8Machine machine, int ins)
    {
        machine.DisableHighResMode();
    }

    // ---- 00FB / 00FC / 00CN : scroll ----------------------------------------

    public static void ExecuteScrollRightIns(Chip8Machine machine, int ins)
    {
        machine.ScrollDisplayRight(4);
    }

    public static void ExecuteScrollLeftIns(Chip8Machine machine, int ins)
    {
        machine.ScrollDisplayLeft(4);
    }

    public static void ScrollDown(Chip8Machine machine, int ins)
    {
        machine.ScrollDisplayDown(ins & 0x0F);
    }

    // ---- DXY0 : 16x16 hi-res sprite -----------------------------------------

    // Called from Chip8Cpu.ExeuteDrawToScreenIns when the display is in hi-res
    // mode and N == 0. Extended for XO-Chip bitplanes (mask param).
    internal static void DrawHighResSprite(Chip8Machine machine, int x, int y, byte planeMask)
    {
        // S-CHIP 1.1 DXY0 hi-res collision semantics (extended for XO-Chip bitplanes):
        // VF = number of sprite rows with at least one collision in any selected plane
        //    + number of sprite rows clipped off the bottom edge (when not wrapping).
        var display = machine.Display;
        var displayPixels = display.Pixels.Span;
        var width = display.Width;
        var height = display.Height;
        var wrap = machine.SpritesWrap;
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
                var spritePixelsRow = (ushort)(machine.ReadMemory(machine.ReadIndexRegisterWithOffset(offset)) << 8 |
                                               machine.ReadMemory(machine.ReadIndexRegisterWithOffset(offset + 1)));
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

        machine.WriteGeneralPurposeRegister(0xF, (byte)(collidedRows + clippedRows));
    }

    // ---- FX30 : load hi-res font character ----------------------------------

    public static void ExecuteLoadHighResFontCharacter(Chip8Machine machine, int ins)
    {
        var x = ExtractX(ins);
        var value = machine.ReadGeneralPurposeRegister(x);
        machine.WriteIndexRegister((value & 0x0F) * Chip8Machine.HighRestFontCharWidth + Chip8Machine.HighResFontBaseAddress);
    }

    // ---- FX75 / FX85 : persistent user flags --------------------------------

    public static void ExecuteSaveFlagsIns(Chip8Machine machine, int ins)
    {
        machine.SaveFlags(ExtractX(ins));
    }

    public static void ExecuteLoadFlagsIns(Chip8Machine machine, int ins)
    {
        machine.LoadFlags(ExtractX(ins));
    }
}
