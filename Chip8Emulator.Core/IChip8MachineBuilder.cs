namespace Chip8Emulator.Core;

public interface IChip8MachineBuilder
{
    IChip8MachineBuilder WithDisplay(IRenderer renderer);
    IChip8MachineBuilder WithInput(IInput input);
    IChip8MachineBuilder WithAudio(IAudio audio);
    IChip8MachineBuilder WithClock(IClock clock);
    IChip8Machine Build();
}