namespace Chip8Emulator.Core;

public interface IChip8Machine
{
    Memory<byte> DisplayPixels { get; }
    int DisplayWidth { get; }
    int DisplayHeight { get; }
    void LoadProgram(ReadOnlySpan<byte> program);
    void Update();
}
