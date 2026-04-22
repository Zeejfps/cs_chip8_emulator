namespace Chip8Emulator.Core.Impl;

internal sealed class Chip8MachineBuilder : IChip8MachineBuilder
{
    private IRenderer? _renderer;
    private IAudio? _audio;
    private IClock? _clock;
    private IInput? _input;

    public IChip8MachineBuilder WithRenderer(IRenderer renderer)
    {
        _renderer = renderer;
        return this;
    }

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

    public IChip8Machine Build()
    {
        var renderer = _renderer ?? throw new InvalidOperationException(
            $"{nameof(WithRenderer)} must be called before {nameof(Build)}.");
        var audio = _audio ?? throw new InvalidOperationException(
            $"{nameof(WithAudio)} must be called before {nameof(Build)}.");
        var clock = _clock ?? throw new InvalidOperationException(
            $"{nameof(WithClock)} must be called before {nameof(Build)}.");
        var input = _input ?? throw new InvalidOperationException(
            $"{nameof(WithInput)} must be called before {nameof(Build)}.");
        return new Chip8Machine(renderer, audio, clock, input);
    }
}
