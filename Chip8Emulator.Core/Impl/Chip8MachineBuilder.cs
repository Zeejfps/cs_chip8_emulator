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
        // TODO: validate
        return new Chip8Machine(_renderer, _audio, _clock, _input);
    }
}