namespace Chip8Emulator.Core;

public sealed class Chip8Stack : IStack
{
    public int StackPointer => _stackPointer;

    private readonly Memory<int> _buffer;
    private int _stackPointer = -1;

    public Chip8Stack(Func<int, Memory<int>> alloc)
    {
        const int requiredSize = 16;
        _buffer = alloc(requiredSize);
        if (_buffer.Length < requiredSize)
            throw new InvalidOperationException($"Allocator returned {_buffer.Length} ints, expected at least {requiredSize}.");
    }
    
    public void Push(int value)
    {
        var nextStackPointer = _stackPointer + 1;
        if (nextStackPointer >= _buffer.Length)
            throw new InvalidOperationException("Stack overflow");
        _stackPointer = nextStackPointer;
        _buffer.Span[_stackPointer] = value;
    }

    public int Pop()
    {
        if (_stackPointer < 0)
            throw new InvalidOperationException("Stack underflow");

        var value = _buffer.Span[_stackPointer];
        _stackPointer--;
        return value;
    }

    public void Clear()
    {
        _buffer.Span.Clear();
        _stackPointer = -1;
    }
}