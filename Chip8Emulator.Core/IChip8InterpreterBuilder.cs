namespace Chip8Emulator.Core;

public interface IChip8InterpreterBuilder
{
    IChip8InterpreterBuilder WithInput(IInput input);
    IChip8InterpreterBuilder WithAudio(IAudio audio);
    IChip8InterpreterBuilder WithClock(IClock clock);
    IChip8InterpreterBuilder WithDisplay(IDisplay display);
    IChip8InterpreterBuilder WithStack(IStack stack);
    IChip8InterpreterBuilder WithMemory(IMemory memory);
    IChip8InterpreterBuilder WithRegisters(IRegisters registers);
    IChip8InterpreterBuilder WithPersistentFlags(IPersistentFlags flags);
    IChip8Interpreter Build();
}