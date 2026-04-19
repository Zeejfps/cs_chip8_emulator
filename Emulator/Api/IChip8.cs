namespace Emulator.Api;

public interface IChip8
{
    void Execute(ReadOnlySpan<byte> program);
}