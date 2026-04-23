using Chip8Emulator.Core;

namespace Chip8Emulator.Web;

internal sealed class BrowserInput : IInput
{
    private readonly bool[] _keys = new bool[16];
    private readonly bool[] _armed = new bool[16];
    private readonly bool[] _pressAndReleasePending = new bool[16];

    public void SetKey(byte key, bool pressed)
    {
        if (key >= 16) return;

        if (pressed && !_keys[key])
        {
            _armed[key] = true;
        }
        else if (!pressed && _keys[key] && _armed[key])
        {
            _armed[key] = false;
            _pressAndReleasePending[key] = true;
        }

        _keys[key] = pressed;
    }

    public bool IsKeyPressed(byte key) => key < 16 && _keys[key];

    public bool WasAnyKeyPressedAndReleased(out byte key)
    {
        for (byte i = 0; i < 16; i++)
        {
            if (_pressAndReleasePending[i])
            {
                _pressAndReleasePending[i] = false;
                key = i;
                return true;
            }
        }
        key = 0;
        return false;
    }
}
