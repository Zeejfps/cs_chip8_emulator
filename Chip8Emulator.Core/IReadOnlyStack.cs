namespace Chip8Emulator.Core;

public interface IReadOnlyStack
{
    ReadOnlyMemory<int> Frames { get; }
    int StackPointer { get; }
}