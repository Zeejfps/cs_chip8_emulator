namespace Chip8Emulator.Core;

internal sealed class Chip8Stack : IStack
{
    public ReadOnlyMemory<int> Frames => _buffer.AsMemory();
    public int StackPointer => _stackPointer;

    private readonly int[] _buffer;
    private int _stackPointer = -1;

    public Chip8Stack()
    {
        _buffer = new int[16];   
    }
    
    public void Push(int value)
    {
        var nextStackPointer = _stackPointer + 1;
        if (nextStackPointer >= _buffer.Length)
            throw new InvalidOperationException("Stack overflow");
        _stackPointer = nextStackPointer;
        _buffer[_stackPointer] = value;
    }

    public int Pop()
    {
        if (_stackPointer < 0)
            throw new InvalidOperationException("Stack underflow");

        var value = _buffer[_stackPointer];
        _stackPointer--;
        return value;
    }

    public void Clear()
    {
        Array.Clear(_buffer);
        _stackPointer = -1;
    }
}