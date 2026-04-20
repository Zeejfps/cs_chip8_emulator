namespace Chip8Emulator.Core.Tests.Fakes;

internal sealed class FakeInput : IInput
{
    private readonly HashSet<byte> _pressedKeys = new();
    private byte? _pendingKey;

    public void Press(byte key) => _pressedKeys.Add(key);
    public void Release(byte key) => _pressedKeys.Remove(key);

    public void QueueKeyPressEvent(byte key) => _pendingKey = key;

    public bool IsKeyPressed(byte key) => _pressedKeys.Contains(key);

    public bool WasAnyKeyPressed(out byte key)
    {
        if (_pendingKey is { } pending)
        {
            key = pending;
            _pendingKey = null;
            return true;
        }
        key = 0;
        return false;
    }
}
