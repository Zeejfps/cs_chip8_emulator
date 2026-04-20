namespace Chip8Emulator.Core;

public interface IChip8Machine
{
    IDisplay Display { get; }
    void LoadProgram(ReadOnlySpan<byte> program);
    void Update();
}
