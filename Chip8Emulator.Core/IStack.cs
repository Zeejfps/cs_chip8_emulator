namespace Chip8Emulator.Core;

public interface IStack
{
    int StackPointer { get; }
    void Push(int value);
    int Pop();
    void Clear();
    ReadOnlySpan<int> AsReadOnlySpan();
}