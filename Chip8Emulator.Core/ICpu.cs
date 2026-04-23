namespace Chip8Emulator.Core;

public interface ICpu
{
    IAudio Audio { get; }
    IDisplay Display { get; }
    IInput Input { get; }
    IMemory Memory { get; }
    IStack Stack { get; }
    IRegisters Registers { get; }
    
    byte SelectedPlanes { get; set; }
    bool ShiftUsesVy { get; }
    bool SpritesWrap { get; }
    bool DisplayWait { get; }
    bool VfResultWrittenLast { get; }
    bool JumpUsesVx { get; }
    bool LoadStoreIncrementsI { get; }
    bool LogicResetsVf { get; }
    
    int ReadProgramCounter();
    void WriteProgramCounter(int value);
    void AdvanceProgramCounter();
    void BeginWaitForKey(int registerIndex);
    void BeginWaitForVBlank();
    void SaveFlags(int count);
    void LoadFlags(int count);
    
    // IAudio
    void LoadAudioPattern();
    void SetPitch(byte pitch);
}
