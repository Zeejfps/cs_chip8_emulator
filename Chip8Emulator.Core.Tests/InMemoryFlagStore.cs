using Chip8Emulator.Core.Spec;

namespace Chip8Emulator.Core.Tests;

public sealed class InMemoryFlagStore : IFlagStore
{
    private readonly byte[] _bytes = new byte[Chip8Interpreter.FlagBytes];

    public void LoadInto(Span<byte> destination)
    {
        _bytes.AsSpan(0, Math.Min(destination.Length, _bytes.Length)).CopyTo(destination);
    }

    public void SaveFrom(ReadOnlySpan<byte> source)
    {
        source[..Math.Min(source.Length, _bytes.Length)].CopyTo(_bytes);
    }
}
