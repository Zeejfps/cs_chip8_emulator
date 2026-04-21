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
            IsHighRes = true;
            Width = HighRestWidth;
            Height = HighRestHeight;
        }
    }
    
    public void Clear()
    {
        Array.Clear(_pixels);
    }
}