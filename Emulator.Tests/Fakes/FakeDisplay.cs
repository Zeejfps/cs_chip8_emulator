using Emulator.Api;

namespace Emulator.Tests.Fakes;

internal sealed class FakeDisplay : IDisplay
{
    public void Draw(ReadOnlySpan<byte> pixels)
    {
        throw new NotImplementedException();
    }
}
