namespace Chip8Emulator.Core;

public interface IPersistentFlags
{
    const int Capacity = 16;

    void Read(Span<byte> destination);
    void Write(ReadOnlySpan<byte> source);
}