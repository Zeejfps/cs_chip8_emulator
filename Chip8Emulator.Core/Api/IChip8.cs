namespace Chip8Emulator.Core.Api;

public interface IChip8
{
    void Execute(ReadOnlySpan<byte> program);
}