namespace Chip8Emulator.Core;

public interface IInterpreter
{
    ICpu Cpu { get; }
    int InstructionsPerSecond { get; set; }
    void LoadProgram(ReadOnlySpan<byte> program);
    void Start();
    void Stop();
}
