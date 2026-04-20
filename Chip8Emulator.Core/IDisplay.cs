namespace Chip8Emulator.Core;

public interface IDisplay
{
    IntPtr PixelData { get; }
    int PixelDataLength { get; }
    int Width { get; }
    int Height { get; }
}
