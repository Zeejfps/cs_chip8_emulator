namespace Chip8Emulator.Core;

public interface IChip8Machine
{
    IMachineDebugger Debugger { get; }
    ReadOnlySpan<byte> Memory { get; }
    IDisplay Display { get; }
    int ProgramCounter { get; }
    int InstructionsPerSecond { get; set; }
    bool ShiftUsesVy { get; set; }
    bool JumpUsesVx { get; set; }
    bool LoadStoreIncrementsI { get; set; }
    bool LogicResetsVf { get; set; }
    bool SpritesWrap { get; set; }
    bool DisplayWait { get; set; }
    void LoadProgram(ReadOnlySpan<byte> program);
    void Update();
}
