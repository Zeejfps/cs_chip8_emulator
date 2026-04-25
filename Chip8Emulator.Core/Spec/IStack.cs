namespace Chip8Emulator.Core.Spec;

internal interface IStack : IReadOnlyStack
{
    void Push(int value);
    int Pop();
    void Clear();
}