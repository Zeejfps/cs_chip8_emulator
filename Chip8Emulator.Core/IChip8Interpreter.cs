namespace Chip8Emulator.Core;

public interface IChip8Interpreter
{
    ICpu Cpu { get; }
    int InstructionsPerSecond { get; set; }
    void LoadProgram(ReadOnlySpan<byte> program);
    void Start();
    void Stop();
}
