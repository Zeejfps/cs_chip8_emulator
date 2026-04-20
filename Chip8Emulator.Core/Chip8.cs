using Chip8Emulator.Core.Impl;

namespace Chip8Emulator.Core;

public static class Chip8
{
    public static IChip8Builder Builder()
    {
        return new Chip8EmulatorBuilder();
    }
}