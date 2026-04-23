namespace Chip8Emulator.Core;

public interface ICpu
{
    int ProgramCounter { get; }
    bool IsWaitingForKey { get; }
    IRegisters Registers { get; }
    IStack Stack { get; }

    bool ShiftUsesVy { get; set; }
    bool JumpUsesVx { get; set; }
    bool LoadStoreIncrementsI { get; set; }
    bool LogicResetsVf { get; set; }
    bool SpritesWrap { get; set; }
    bool DisplayWait { get; set; }
    bool VfResultWrittenLast { get; set; }

    void StepInstruction();
}
