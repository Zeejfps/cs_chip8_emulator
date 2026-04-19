namespace Emulator.Api;

public interface IChip8Builder
{
    IChip8Builder WithDisplay();
    IChip8Builder WithInput();
    IChip8 Build();
}