namespace Chip8Emulator.Core.Impl;

internal sealed class Display : IDisplay
{
    public const int HighRestWidth = 128;
    public const int HighRestHeight = 64;
    public const int LowRestWidth = 64;
    public const int LowRestHeight = 32;
    
    public Memory<byte> Pixels => _pixels;
    public int Width { get; private set; }
    public int Height { get; private set; }
    public bool IsHighRes { get; private set; }
    
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
        Width = LowRestWidth;
        Height = LowRestHeight;
        Clear();
    }

    public void ToggleHighRest()
    {
        if (IsHighRes)
        {
            IsHighRes = false;
            Width = LowRestWidth;
            Height = LowRestHeight;
        }
        else
        {
            EnableHighResMode();
        }
    }
    
    public void Clear()
    {
        Array.Clear(_pixels);
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

    public void ScrollLeft(int n)
    {
        if (n <= 0) return;

        if (n >= Width)
        {
            Clear();
            return;
        }

        for (var y = 0; y < Height; y++)
        {
            var row = y * Width;
            Array.Copy(_pixels, row + n, _pixels, row, Width - n);
            Array.Clear(_pixels, row + Width - n, n);
        }
    }

    public void ScrollRight(int n)
    {
        if (n <= 0) return;

        if (n >= Width)
        {
            Clear();
            return;
        }

        for (var y = 0; y < Height; y++)
        {
            var row = y * Width;
            Array.Copy(_pixels, row, _pixels, row + n, Width - n);
            Array.Clear(_pixels, row, n);
        }
    }

    public void ScrollDown(int n)
    {
        if (n <= 0) return;

        if (n >= Height)
        {
            Clear();
            return;
        }

        for (var y = Height - 1; y >= n; y--)
        {
            var srcRow = (y - n) * Width;
            var dstRow = y * Width;
            Array.Copy(_pixels, srcRow, _pixels, dstRow, Width);
        }

        for (var y = 0; y < n; y++)
        {
            Array.Clear(_pixels, y * Width, Width);
        }
    }
}