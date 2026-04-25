namespace Chip8Emulator.Core;

public interface IFlagStore
{
    void LoadInto(Span<byte> destination);
    void SaveFrom(ReadOnlySpan<byte> source);
}