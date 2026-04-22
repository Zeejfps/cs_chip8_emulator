namespace Chip8Emulator.Core.Tests.Fakes;

internal sealed class FakeInput : IInput
{
    private readonly HashSet<byte> _pressedKeys = new();
    private readonly HashSet<byte> _armedKeys = new();
    private byte? _pendingKey;

    public void Press(byte key)
    {
        if (_pressedKeys.Add(key))
        {
            _armedKeys.Add(key);
        }
    }

    public void Release(byte key)
    {
        if (_pressedKeys.Remove(key) && _armedKeys.Remove(key))
        {
            _pendingKey = key;
        }
    }

    public void QueueKeyPressAndReleaseEvent(byte key) => _pendingKey = key;

    public bool IsKeyPressed(byte key) => _pressedKeys.Contains(key);

    public bool WasAnyKeyPressedAndReleased(out byte key)
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
