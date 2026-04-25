namespace Chip8Emulator.Core;

public interface IStack : IReadOnlyStack
{
    void Push(int value);
    int Pop();
    void Clear();
}