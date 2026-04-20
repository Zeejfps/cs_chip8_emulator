namespace Chip8Emulator.Core;

public interface IChip8Machine
{
    void LoadProgram(ReadOnlySpan<byte> program);
    void Update();
}