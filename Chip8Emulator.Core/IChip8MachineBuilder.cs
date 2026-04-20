namespace Chip8Emulator.Core;

public interface IChip8MachineBuilder
{
    IChip8MachineBuilder WithDisplay(IDisplay display);
    IChip8MachineBuilder WithInput();
    IChip8MachineBuilder WithAudio(IAudio audio);
    IChip8MachineBuilder WithClock(IClock clock);
    IChip8Machine Build();
}