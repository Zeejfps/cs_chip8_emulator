using Chip8Emulator.Core.Spec;

namespace Chip8Emulator.Core;

public static class Chip8
{
    public static IChip8InterpreterBuilder Builder()
    {
        return new Chip8InterpreterBuilder();
    }
}