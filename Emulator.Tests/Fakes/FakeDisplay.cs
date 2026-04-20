using Emulator.Api;

namespace Emulator.Tests.Fakes;

internal sealed class FakeDisplay : IDisplay
{
    public int DrawCount { get; private set; }
    public byte[] LastPixels { get; private set; } = [];

    public void Draw(ReadOnlySpan<byte> pixels)
    {
        DrawCount++;
        LastPixels = pixels.ToArray();
    }
}
