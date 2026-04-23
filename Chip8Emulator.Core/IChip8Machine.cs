namespace Chip8Emulator.Core;

public interface IChip8Machine
{
    IDisplay Display { get; }
    IMemory Memory { get; }
    IRegisters Registers { get; }
    IStack Stack { get; }
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
