using Emulator.Impl;

namespace Emulator.Api;

public static class Chip8
{
    public static IChip8Builder Builder()
    {
        return new Chip8EmulatorBuilder();
    }
}