namespace Chip8Emulator.Core;

public interface IChip8Machine
{
    ReadOnlySpan<byte> Memory { get; }
    IDisplay Display { get; }
    public int ProgramCounter { get; }
    void LoadProgram(ReadOnlySpan<byte> program);
    void Update();
}
