namespace Chip8Emulator.Core;

public interface IChip8Machine
{
    ReadOnlySpan<byte> Memory { get; }
    IDisplay Display { get; }
    int ProgramCounter { get; }
    int InstructionsPerSecond { get; }
    void LoadProgram(ReadOnlySpan<byte> program);
    void Update();
}
