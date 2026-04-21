namespace Chip8Emulator.Core;

public interface IDebugger
{
    ReadOnlySpan<byte> Memory { get; }
    int ProgramCounter { get; }
}