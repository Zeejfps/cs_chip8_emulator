namespace Chip8Emulator.Core;

internal sealed class Chip8MachineBuilder : IChip8MachineBuilder
{
    private IDisplay? _display;
    private IAudio? _audio;
    private IClock? _clock;
    private IInput? _input;
    private IStack? _stack;
    private IMemory? _memory;
    private IRegisters? _registers;
    private IPersistentFlags? _persistentFlags;

    public IChip8MachineBuilder WithInput(IInput input)
    {
        _input = input;
        return this;
    }

    public IChip8MachineBuilder WithAudio(IAudio audio)
    {
        _audio = audio;
        return this;
    }

    public IChip8MachineBuilder WithClock(IClock clock)
    {
        _clock = clock;
        return this;
    }

    public IChip8MachineBuilder WithStack(IStack stack)
    {
        _stack = stack;
        return this;
    }

    public IChip8MachineBuilder WithRegisters(IRegisters registers)
    {
        _registers = registers;
        return this;
    }

    public IChip8MachineBuilder WithPersistentFlags(IPersistentFlags flags)
    {
        _persistentFlags = flags;
        return this;
    }

    public IChip8MachineBuilder WithMemory(IMemory memory)
    {
        _memory = memory;
        return this;
    }

    public IChip8MachineBuilder WithDisplay(IDisplay display)
    {
        _display = display;
        return this;
    }

    public IChip8Interpreter Build()
    {
        var audio = _audio ?? throw new InvalidOperationException(
            $"{nameof(WithAudio)} must be called before {nameof(Build)}.");
        var clock = _clock ?? throw new InvalidOperationException(
            $"{nameof(WithClock)} must be called before {nameof(Build)}.");
        var input = _input ?? throw new InvalidOperationException(
            $"{nameof(WithInput)} must be called before {nameof(Build)}.");
        var stack = _stack ?? throw new InvalidOperationException(
            $"{nameof(WithStack)} must be called before {nameof(Build)}.");
        var registers = _registers ?? throw new InvalidOperationException(
            $"{nameof(WithRegisters)} must be called before {nameof(Build)}.");
        var memory = _memory ?? throw new InvalidOperationException(
            $"{nameof(WithMemory)} must be called before {nameof(Build)}.");
        var display = _display ?? throw new InvalidOperationException(
            $"{nameof(WithDisplay)} must be called before {nameof(Build)}.");
        var persistentFlags = _persistentFlags ?? new EmulatedPersistentFlags();

        var bus = new EmulatorBus();
        var cpu = new EmulatedCpu(memory, display, registers, stack, persistentFlags, bus);
        return new Chip8Interpreter(clock, display, memory, audio, input, bus, cpu);
    }
}
