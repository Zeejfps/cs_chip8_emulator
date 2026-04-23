namespace Chip8Emulator.Core;

public interface IDisplay
{
    Memory<byte> Pixels { get; }
    int Width { get; }
    int Height { get; }
    bool IsHighRes { get; }
    void Clear();
    void EnableHighResMode();
    void DisableHighResMode();
    void ScrollDown(int n);
    void ScrollUp(int n);
    void ScrollLeft(int n);
    void ScrollRight(int n);
}
