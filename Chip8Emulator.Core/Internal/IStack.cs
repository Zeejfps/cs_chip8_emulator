namespace Chip8Emulator.Core.Internal;

internal interface IStack : IReadOnlyStack
{
    void Push(int value);
    int Pop();
    void Clear();
}