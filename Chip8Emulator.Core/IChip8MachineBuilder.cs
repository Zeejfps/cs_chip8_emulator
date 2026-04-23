namespace Chip8Emulator.Core;

public interface IChip8MachineBuilder
{
    IChip8MachineBuilder WithInput(IInput input);
    IChip8MachineBuilder WithAudio(IAudio audio);
    IChip8MachineBuilder WithClock(IClock clock);
    IChip8MachineBuilder WithDisplay(IDisplay display);
    IChip8MachineBuilder WithStack(IStack stack);
    IChip8MachineBuilder WithMemory(IMemory memory);
    IChip8MachineBuilder WithRegisters(IRegisters registers);
    IChip8MachineBuilder WithPersistentFlags(IPersistentFlags flags);
    IChip8Machine Build();
}