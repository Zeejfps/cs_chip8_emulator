using Emulator.Api;

namespace Emulator.Tests.Fakes;

internal sealed class FakeDisplay : IDisplay
{
    public int ClearCount { get; private set; }
    public int UpdateCount { get; private set; }

    public void Clear() => ClearCount++;
    public void Update() => UpdateCount++;
    
    public int BlitRow(byte x, byte y, byte row)
    {
        throw new NotImplementedException();
    }
}
