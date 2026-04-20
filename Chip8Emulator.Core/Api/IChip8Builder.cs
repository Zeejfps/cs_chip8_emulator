namespace Chip8Emulator.Core.Api;

public interface IChip8Builder
{
    IChip8Builder WithDisplay(IDisplay display);
    IChip8Builder WithInput();
    IChip8Builder WithAudio(IAudio audio);
    IChip8Machine Build();
}