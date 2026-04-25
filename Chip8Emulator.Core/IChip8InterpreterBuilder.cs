namespace Chip8Emulator.Core;

public interface IChip8InterpreterBuilder
{
    IChip8InterpreterBuilder WithInput(IInput input);
    IChip8InterpreterBuilder WithAudio(IAudio audio);
    IChip8InterpreterBuilder WithClock(IClock clock);
    IChip8InterpreterBuilder WithFlagStore(IFlagStore flagStore);
    IChip8InterpreterBuilder WithRenderer(IRenderer renderer);
    IInterpreter Build();
}