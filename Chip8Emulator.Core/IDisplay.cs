namespace Chip8Emulator.Core;

public interface IDisplay
{
    byte SelectedPlanes { get; set; }
    int Width { get; }
    int Height { get; }
    bool IsHighRes { get; }

    void WritePixels(Action<Span<byte>> writeAction);

    void Reset();
    void Clear();
    void EnableClassicHiresMode();
    void EnableHighResMode();
    void DisableHighResMode();
    void ScrollDown(int n);
    void ScrollUp(int n);
    void ScrollLeft(int n);
    void ScrollRight(int n);
}
