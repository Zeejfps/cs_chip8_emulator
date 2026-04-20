namespace Chip8Emulator.Core.Tests.Fakes;

internal sealed class FakeInput : IInput
{
    private readonly HashSet<byte> _pressedKeys = new();

    public void Press(byte key) => _pressedKeys.Add(key);
    public void Release(byte key) => _pressedKeys.Remove(key);

    public bool IsKeyPressed(byte key) => _pressedKeys.Contains(key);
}
