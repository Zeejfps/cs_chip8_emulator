namespace Chip8Emulator.Core;

public interface IFlagStore
{
    const int Capacity = 16;

    void LoadInto(Span<byte> destination);
    void SaveFrom(ReadOnlySpan<byte> source);
}