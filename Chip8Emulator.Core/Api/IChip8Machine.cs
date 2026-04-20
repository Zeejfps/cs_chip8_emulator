namespace Chip8Emulator.Core.Api;

public interface IChip8Machine
{
    void LoadProgram(ReadOnlySpan<byte> program);
    void Update();
}