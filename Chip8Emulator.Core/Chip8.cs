using Chip8Emulator.Core.Cpu;

namespace Chip8Emulator.Core;

public static class Chip8
{
    public static IChip8MachineBuilder Builder()
    {
        return new Chip8MachineBuilder();
    }
}