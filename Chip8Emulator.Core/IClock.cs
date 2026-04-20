namespace Chip8Emulator.Core;

public interface IClock
{
    long Timestamp { get; }
    long Frequency { get; }
}
