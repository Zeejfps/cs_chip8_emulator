namespace Chip8Emulator.Core;

public interface IInterpreter
{
    IReadOnlyStack Stack { get; }
    IReadOnlyDisplay Display { get; }
    IReadOnlyMemory Memory { get; }
    IReadOnlyRegisters Registers { get; }

    int InstructionsPerSecond { get; set; }
    bool ShiftUsesVy { get; set; }
    bool JumpUsesVx { get; set; }
    bool LoadStoreIncrementsI { get; set; }
    bool LogicResetsVf { get; set; }
    bool SpritesWrap { get; set; }
    bool DisplayWait { get; set; }
    bool VfResultWrittenLast { get; set; }

    void LoadProgram(ReadOnlySpan<byte> program);
    void Start();
    void Stop();
}
