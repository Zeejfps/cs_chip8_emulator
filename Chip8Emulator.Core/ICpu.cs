namespace Chip8Emulator.Core;

public interface ICpu
{
    IDisplay Display { get; }
    IInput Input { get; }
    IMemory Memory { get; }
    byte SelectedPlanes { get; set; }
    bool ShiftUsesVy { get; }
    bool SpritesWrap { get; }
    bool DisplayWait { get; }
    bool VfResultWrittenLast { get; }
    bool JumpUsesVx { get; }
    bool LoadStoreIncrementsI { get; }
    bool LogicResetsVf { get; }

    byte ReadGeneralPurposeRegister(int register);
    void WriteGeneralPurposeRegister(int register, byte value);
    int ReadIndexRegister();
    void WriteIndexRegister(int value);
    int ReadIndexRegisterWithOffset(int offset);
    int ReadProgramCounter();
    void WriteProgramCounter(int value);
    void AdvanceProgramCounter();
    byte ReadDelayTimer();
    void WriteDelayTimer(byte value);
    void WriteSoundTimer(byte value);
    void PushStack(int value);
    int PopStack();
    void BeginWaitForKey(int registerIndex);
    void BeginWaitForVBlank();
    void ClearDisplay();
    void EnableHighResMode();
    void DisableHighResMode();
    void ScrollDisplayDown(int n);
    void ScrollDisplayUp(int n);
    void ScrollDisplayLeft(int n);
    void ScrollDisplayRight(int n);
    void SaveFlags(int count);
    void LoadFlags(int count);
    void LoadAudioPattern();
    void SetPitch(byte pitch);
    void DispatchSystemInstruction(int ins);
    void DispatchArithmeticInstruction(int ins);
    void DispatchKeyCheckInstruction(int ins);
    void DispatchTimerInstruction(int ins);
    void DispatchFiveOpInstruction(int ins);
}
