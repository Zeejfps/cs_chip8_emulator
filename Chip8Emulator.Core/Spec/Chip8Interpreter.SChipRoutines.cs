namespace Chip8Emulator.Core.Spec;

// SUPER-CHIP 1.1 additions: hi-res mode, 4-pixel scrolls, scroll down N,
// 16x16 sprite drawing (DXY0), 10-byte high-res font, persistent user flags.
internal sealed partial class Chip8Interpreter
{
    // ---- 00FF / 00FE : high-res mode toggle ---------------------------------

    internal void EnableHiresMode(in DecodedOp op)
    {
        Display.EnableHighResMode();
    }

    internal void DisableHiresMode(in DecodedOp op)
    {
        Display.DisableHighResMode();
    }

    // ---- 00FB / 00FC / 00CN : scroll ----------------------------------------

    internal void ScrollDisplayRight(in DecodedOp op)
    {
        Display.ScrollRight(4);
    }

    internal void ScrollDisplayLeft(in DecodedOp op)
    {
        Display.ScrollLeft(4);
    }

    internal void ScrollDisplayDown(in DecodedOp op)
    {
        Display.ScrollDown(op.N);
    }

    // ---- DXY0 : 16x16 hi-res sprite -----------------------------------------

    // Called from DrawToScreen when the display is in hi-res mode and N == 0.
    // Extended for XO-Chip bitplanes (mask param).
    private void DrawHighResSprite(int x, int y, byte planeMask)
    {
        // S-CHIP 1.1 DXY0 hi-res collision semantics (extended for XO-Chip bitplanes):
        // VF = number of sprite rows with at least one collision in any selected plane
        //    + number of sprite rows clipped off the bottom edge (when not wrapping).
        Display.WritePixels(displayPixels =>
        {
            Span<bool> rowCollisions = stackalloc bool[16];
            var anyClipped = 0;
            var spriteBase = 0;

            for (var planeBit = 0; planeBit < 2; planeBit++)
            {
                var planeBitMask = (byte)(1 << planeBit);
                if ((planeMask & planeBitMask) == 0) continue;

                var clipped = DrawHighResPlane(displayPixels, x, y, spriteBase, planeBitMask, rowCollisions);
                anyClipped = Math.Max(anyClipped, clipped);

                // 16 rows * 2 bytes per row = next plane's sprite data.
                spriteBase += 32;
            }

            var collidedRows = 0;
            for (var i = 0; i < 16; i++)
                if (rowCollisions[i]) collidedRows++;

            Registers.WriteV(0xF, (byte)(collidedRows + anyClipped));
        });
    }

    private int DrawHighResPlane(
        Span<byte> displayPixels, int x, int y,
        int spriteBase, byte planeBitMask, Span<bool> rowCollisions)
    {
        var width = Display.Width;
        var height = Display.Height;
        var wrap = SpritesWrap;

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

            var spriteRow = ReadHighResSpriteRow(spriteBase + i * 2);
            if (DrawHighResRow(displayPixels, spriteRow, x, dstY, width, wrap, planeBitMask))
                rowCollisions[i] = true;
        }

        return 0;
    }

    private ushort ReadHighResSpriteRow(int offset)
    {
        var hi = Memory.Read(Registers.ReadIWithOffset(offset));
        var lo = Memory.Read(Registers.ReadIWithOffset(offset + 1));
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

    internal void LoadHighResFontCharacter(in DecodedOp op)
    {
        var value = Registers.ReadV(op.X);
        Registers.WriteI((value & 0x0F) * HighResFontCharWidth + HighResFontBaseAddress);
    }

    // ---- FX75 / FX85 : persistent user flags --------------------------------

    internal void SaveFlagsIns(in DecodedOp op)
    {
        SaveFlags(op.X);
    }

    internal void LoadFlagsIns(in DecodedOp op)
    {
        LoadFlags(op.X);
    }
}
