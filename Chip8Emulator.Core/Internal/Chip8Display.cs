namespace Chip8Emulator.Core.Internal;

internal sealed class Chip8Display : IDisplay
{
    public const int HighResWidth = 128;
    public const int HighResHeight = 64;
    public const int ClassicHiresWidth = 64;
    public const int ClassicHiresHeight = 64;
    public const int LowResWidth = 64;
    public const int LowResHeight = 32;

    // Pixel encoding: bit 0 = plane 0, bit 1 = plane 1. Value range 0..3.
    public const byte Plane0Mask = 0x01;
    public const byte Plane1Mask = 0x02;
    public const byte AllPlanesMask = 0x03;

    public int Width { get; private set; }
    public int Height { get; private set; }
    public bool IsHighRes { get; private set; }
    public ReadOnlyMemory<byte> VMem => _pixels.AsMemory();

    private readonly byte[] _pixels;
    
    public Chip8Display()
    {
        Width = LowResWidth;
        Height = LowResHeight;
        const int requiredSize = HighResWidth * HighResHeight;
        _pixels = new byte[requiredSize];
    }

    public void WritePixels(Action<Span<byte>> writeAction)
    {
        writeAction(_pixels.AsSpan());
    }

    // XO-Chip FX01 plane mask; defaults to plane 0 so legacy (CHIP-8/SCHIP) drawing works.

    public byte SelectedPlanes
    {
        get;
        set => field = (byte)(value & AllPlanesMask);
    } = Plane0Mask;
    
    public void Reset()
    {
        IsHighRes = false;
        Width = LowResWidth;
        Height = LowResHeight;
        SelectedPlanes = Plane0Mask;
        Array.Clear(_pixels);
    }

    public void Clear()
    {
        var mask = (byte)(SelectedPlanes & AllPlanesMask);
        if (mask == 0) return;
        if (mask == AllPlanesMask)
        {
            Array.Clear(_pixels);
            return;
        }
        var keep = (byte)(~mask & AllPlanesMask);
        for (var i = 0; i < _pixels.Length; i++)
        {
            _pixels[i] = (byte)(_pixels[i] & keep);
        }
    }

    public void EnableHighResMode()
    {
        IsHighRes = true;
        Width = HighResWidth;
        Height = HighResHeight;
    }

    public void DisableHighResMode()
    {
        IsHighRes = false;
        Width = LowResWidth;
        Height = LowResHeight;
    }

    public void EnableClassicHiresMode()
    {
        IsHighRes = false;
        Width = ClassicHiresWidth;
        Height = ClassicHiresHeight;
    }

    public void ScrollLeft(int n)
    {
        if (n <= 0) return;
        var mask = (byte)(SelectedPlanes & AllPlanesMask);
        if (mask == 0) return;
        var keep = (byte)(~mask & AllPlanesMask);

        if (n >= Width)
        {
            ClearSelectedPlanes(mask);
            return;
        }

        var pixels = _pixels;
        for (var y = 0; y < Height; y++)
        {
            var row = y * Width;
            for (var x = 0; x < Width - n; x++)
            {
                var src = pixels[row + x + n];
                var dst = pixels[row + x];
                pixels[row + x] = (byte)((src & mask) | (dst & keep));
            }
            for (var x = Width - n; x < Width; x++)
            {
                pixels[row + x] = (byte)(pixels[row + x] & keep);
            }
        }
    }

    public void ScrollRight(int n)
    {
        if (n <= 0) return;
        var mask = (byte)(SelectedPlanes & AllPlanesMask);
        if (mask == 0) return;
        var keep = (byte)(~mask & AllPlanesMask);

        if (n >= Width)
        {
            ClearSelectedPlanes(mask);
            return;
        }

        var pixels = _pixels;
        for (var y = 0; y < Height; y++)
        {
            var row = y * Width;
            for (var x = Width - 1; x >= n; x--)
            {
                var src = pixels[row + x - n];
                var dst = pixels[row + x];
                pixels[row + x] = (byte)((src & mask) | (dst & keep));
            }
            for (var x = 0; x < n; x++)
            {
                pixels[row + x] = (byte)(pixels[row + x] & keep);
            }
        }
    }

    public void ScrollDown(int n)
    {
        if (n <= 0) return;
        var mask = (byte)(SelectedPlanes & AllPlanesMask);
        if (mask == 0) return;
        var keep = (byte)(~mask & AllPlanesMask);

        if (n >= Height)
        {
            ClearSelectedPlanes(mask);
            return;
        }

        var pixels = _pixels;
        for (var y = Height - 1; y >= n; y--)
        {
            var srcRow = (y - n) * Width;
            var dstRow = y * Width;
            for (var x = 0; x < Width; x++)
            {
                var src = pixels[srcRow + x];
                var dst = pixels[dstRow + x];
                pixels[dstRow + x] = (byte)((src & mask) | (dst & keep));
            }
        }

        for (var y = 0; y < n; y++)
        {
            var row = y * Width;
            for (var x = 0; x < Width; x++)
            {
                pixels[row + x] = (byte)(pixels[row + x] & keep);
            }
        }
    }

    public void ScrollUp(int n)
    {
        if (n <= 0) return;
        var mask = (byte)(SelectedPlanes & AllPlanesMask);
        if (mask == 0) return;
        var keep = (byte)(~mask & AllPlanesMask);

        if (n >= Height)
        {
            ClearSelectedPlanes(mask);
            return;
        }

        var pixels = _pixels;
        for (var y = 0; y < Height - n; y++)
        {
            var srcRow = (y + n) * Width;
            var dstRow = y * Width;
            for (var x = 0; x < Width; x++)
            {
                var src = pixels[srcRow + x];
                var dst = pixels[dstRow + x];
                pixels[dstRow + x] = (byte)((src & mask) | (dst & keep));
            }
        }

        for (var y = Height - n; y < Height; y++)
        {
            var row = y * Width;
            for (var x = 0; x < Width; x++)
            {
                pixels[row + x] = (byte)(pixels[row + x] & keep);
            }
        }
    }

    private void ClearSelectedPlanes(byte mask)
    {
        var pixels = _pixels;
        if (mask == AllPlanesMask)
        {
            Array.Clear(pixels);
            return;
        }
        var keep = (byte)(~mask & AllPlanesMask);
        for (var i = 0; i < _pixels.Length; i++)
        {
            pixels[i] = (byte)(pixels[i] & keep);
        }
    }
}
