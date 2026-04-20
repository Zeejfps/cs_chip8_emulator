using Chip8Emulator.Core.Api;

namespace Chip8Emulator.Core.Impl;

internal sealed class Chip8EmulatorBuilder : IChip8Builder
{
    private IDisplay? _display;
    private IAudio? _audio;
    
    public IChip8Builder WithDisplay(IDisplay display)
    {
        _display = display;
        return this;
    }

    public IChip8Builder WithInput()
    {
        return this;
    }

    public IChip8Builder WithAudio(IAudio audio)
    {
        _audio = audio;
        return this;
    }

    public IChip8 Build()
    {
        // TODO: validate
        return new Chip8Machine(_display, _audio);
    }
}