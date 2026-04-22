namespace Chip8Emulator.Core;

public interface IMachineDebugger
{
    int ProgramCounter { get; }
    int IndexRegister { get; }                                                                                   
    int StackPointer { get; }                                                                                    
    byte DelayTimer { get; }                                                                                     
    byte SoundTimer { get; }                                                                                     
    bool IsWaitingForKey { get; }                                                                           
    bool IsWaitingForVBlank { get; }                                                                             
                  
    ReadOnlySpan<byte> Memory { get; }
    ReadOnlySpan<byte> Registers { get; }   // V0..VF, 16 bytes                                                  
    ReadOnlySpan<int> Stack { get; } 
    
    void StepInstruction();
}