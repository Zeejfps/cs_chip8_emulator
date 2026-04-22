namespace Chip8Emulator.Core;

public interface IPersistentFlags
{
    const int Capacity = 16;

    void Read(Span<byte> destination);
    void Write(ReadOnlySpan<byte> source);
}

public sealed class InMemoryPersistentFlags : IPersistentFlags
{
    private readonly byte[] _bytes = new byte[IPersistentFlags.Capacity];

    public void Read(Span<byte> destination)
    {
        _bytes.AsSpan(0, Math.Min(destination.Length, _bytes.Length)).CopyTo(destination);
    }

    public void Write(ReadOnlySpan<byte> source)
    {
        source[..Math.Min(source.Length, _bytes.Length)].CopyTo(_bytes);
    }
}
