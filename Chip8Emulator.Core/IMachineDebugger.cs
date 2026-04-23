namespace Chip8Emulator.Core;

public interface IMachineDebugger
{
    int ProgramCounter { get; }
    bool IsWaitingForKey { get; }
    bool IsWaitingForVBlank { get; }

    void StepInstruction();
}
