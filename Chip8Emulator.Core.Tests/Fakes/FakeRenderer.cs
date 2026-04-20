namespace Chip8Emulator.Core.Tests.Fakes;

internal sealed class FakeRenderer : IRenderer
{
    public int DrawCount { get; private set; }
    public byte[] LastPixels { get; private set; } = [];

    public void Render(ReadOnlySpan<byte> pixels)
    {
        DrawCount++;
        LastPixels = pixels.ToArray();
    }
}
