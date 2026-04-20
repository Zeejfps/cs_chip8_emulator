namespace Chip8Emulator.Core.Impl;

internal sealed class Chip8MachineBuilder : IChip8MachineBuilder
{
    private IDisplay? _display;
    private IAudio? _audio;
    private IClock? _clock;
    private IInput? _input;

    public IChip8MachineBuilder WithDisplay(IDisplay display)
    {
        _display = display;
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
        return new Chip8Machine(_display, _audio, _clock, _input);
    }
}