namespace Emulator.Api;

public interface IDisplay
{
    void Draw(ReadOnlySpan<byte> pixels);
}