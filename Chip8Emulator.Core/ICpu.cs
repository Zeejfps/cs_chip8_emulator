namespace Chip8Emulator.Core;

public interface ICpu
{
    IRegisters Registers { get; }
    IStack Stack { get; }

    bool ShiftUsesVy { get; set; }
    bool JumpUsesVx { get; set; }
    bool LoadStoreIncrementsI { get; set; }
    bool LogicResetsVf { get; set; }
    bool SpritesWrap { get; set; }
    bool DisplayWait { get; set; }
    bool VfResultWrittenLast { get; set; }

    int ReadProgramCounter();
    void WriteProgramCounter(int value);
    void AdvanceProgramCounter();

    void FetchDecodeExecute();
}
