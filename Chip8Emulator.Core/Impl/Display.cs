namespace Chip8Emulator.Core.Impl;

internal sealed class Display : IDisplay
{
    public const int HighRestWidth = 128;
    public const int HighRestHeight = 64;
    public const int ClassicHiresWidth = 64;
    public const int ClassicHiresHeight = 64;
    public const int LowRestWidth = 64;
    public const int LowRestHeight = 32;

    // Pixel encoding: bit 0 = plane 0, bit 1 = plane 1. Value range 0..3.
    public const byte Plane0Mask = 0x01;
    public const byte Plane1Mask = 0x02;
    public const byte AllPlanesMask = 0x03;

    public Memory<byte> Pixels => _pixels;
    public int Width { get; private set; }
    public int Height { get; private set; }
    public bool IsHighRes { get; private set; }

    // XO-Chip FX01 plane mask; defaults to plane 0 so legacy (CHIP-8/SCHIP) drawing works.
    public byte SelectedPlanes { get; set; } = Plane0Mask;

    private readonly byte[] _pixels;

    public Display()
    {
        Width = LowRestWidth;
        Height = LowRestHeight;
        //NOTE (Zee): Allocate enough space for the high resolution display.
        _pixels = new byte[HighRestWidth * HighRestHeight];
    }

    public void Reset()
    {
        IsHighRes = false;
        Width = LowRestWidth;
        Height = LowRestHeight;
        SelectedPlanes = Plane0Mask;
        Array.Clear(_pixels);
    }

    public void ToggleHighRest()
    {
        if (IsHighRes)
        {
            DisableHighResMode();
        }
        else
        {
            EnableHighResMode();
        }
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
        Width = HighRestWidth;
        Height = HighRestHeight;
    }

    public void DisableHighResMode()
    {
        IsHighRes = false;
        Width = LowRestWidth;
        Height = LowRestHeight;
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

        for (var y = 0; y < Height; y++)
        {
            var row = y * Width;
            for (var x = 0; x < Width - n; x++)
            {
                var src = _pixels[row + x + n];
                var dst = _pixels[row + x];
                _pixels[row + x] = (byte)((src & mask) | (dst & keep));
            }
            for (var x = Width - n; x < Width; x++)
            {
                _pixels[row + x] = (byte)(_pixels[row + x] & keep);
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

        for (var y = 0; y < Height; y++)
        {
            var row = y * Width;
            for (var x = Width - 1; x >= n; x--)
            {
                var src = _pixels[row + x - n];
                var dst = _pixels[row + x];
                _pixels[row + x] = (byte)((src & mask) | (dst & keep));
            }
            for (var x = 0; x < n; x++)
            {
                _pixels[row + x] = (byte)(_pixels[row + x] & keep);
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

        for (var y = Height - 1; y >= n; y--)
        {
            var srcRow = (y - n) * Width;
            var dstRow = y * Width;
            for (var x = 0; x < Width; x++)
            {
                var src = _pixels[srcRow + x];
                var dst = _pixels[dstRow + x];
                _pixels[dstRow + x] = (byte)((src & mask) | (dst & keep));
            }
        }

        for (var y = 0; y < n; y++)
        {
            var row = y * Width;
            for (var x = 0; x < Width; x++)
            {
                _pixels[row + x] = (byte)(_pixels[row + x] & keep);
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

        for (var y = 0; y < Height - n; y++)
        {
            var srcRow = (y + n) * Width;
            var dstRow = y * Width;
            for (var x = 0; x < Width; x++)
            {
                var src = _pixels[srcRow + x];
                var dst = _pixels[dstRow + x];
                _pixels[dstRow + x] = (byte)((src & mask) | (dst & keep));
            }
        }

        for (var y = Height - n; y < Height; y++)
        {
            var row = y * Width;
            for (var x = 0; x < Width; x++)
            {
                _pixels[row + x] = (byte)(_pixels[row + x] & keep);
            }
        }
    }

    private void ClearSelectedPlanes(byte mask)
    {
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
}
