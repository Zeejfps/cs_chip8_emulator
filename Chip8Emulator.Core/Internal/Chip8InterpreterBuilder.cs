using Chip8Emulator.Core.Internal;

namespace Chip8Emulator.Core;

internal sealed class Chip8InterpreterBuilder : IChip8InterpreterBuilder
{
    private IAudio? _audio;
    private IClock? _clock;
    private IInput? _input;
    private IFlagStore? _flagStore;
    private IRenderer? _renderer;

    public IChip8InterpreterBuilder WithInput(IInput input)
    {
        _input = input;
        return this;
    }

    public IChip8InterpreterBuilder WithAudio(IAudio audio)
    {
        _audio = audio;
        return this;
    }

    public IChip8InterpreterBuilder WithClock(IClock clock)
    {
        _clock = clock;
        return this;
    }

    public IChip8InterpreterBuilder WithFlagStore(IFlagStore flagStore)
    {
        _flagStore = flagStore;
        return this;
    }

    public IChip8InterpreterBuilder WithRenderer(IRenderer renderer)
    {
        _renderer = renderer;
        return this;
    }

    public IInterpreter Build()
    {
        var audio = _audio ?? throw new InvalidOperationException(
            $"{nameof(WithAudio)} must be called before {nameof(Build)}.");
        var clock = _clock ?? throw new InvalidOperationException(
            $"{nameof(WithClock)} must be called before {nameof(Build)}.");
        var input = _input ?? throw new InvalidOperationException(
            $"{nameof(WithInput)} must be called before {nameof(Build)}.");
        var renderer = _renderer ?? throw new InvalidOperationException(
            $"{nameof(WithRenderer)} must be called before {nameof(Build)}.");
        var flagStore = _flagStore ?? throw new InvalidOperationException(
            $"{nameof(WithFlagStore)} must be called before {nameof(Build)}.");

        var stack = new Chip8Stack();
        var registers = new Chip8Registers();
        var memory = new Chip8Memory();
        var display = new Chip8Display();

        return new Chip8Interpreter(clock, display, memory, audio, input, registers, stack, flagStore, renderer);
    }
}
