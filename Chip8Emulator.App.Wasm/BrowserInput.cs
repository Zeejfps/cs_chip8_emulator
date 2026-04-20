using Chip8Emulator.Core;

namespace Chip8Emulator.App.Wasm;

internal sealed class BrowserInput : IInput
{
    private readonly bool[] _keys = new bool[16];
    private readonly bool[] _pressedSinceLastPoll = new bool[16];

    public void SetKey(byte key, bool pressed)
    {
        if (key >= 16) return;
        if (pressed && !_keys[key])
        {
            _pressedSinceLastPoll[key] = true;
        }
        _keys[key] = pressed;
    }

    public bool IsKeyPressed(byte key) => key < 16 && _keys[key];

    public bool WasAnyKeyPressed(out byte key)
    {
        for (byte i = 0; i < 16; i++)
        {
            if (_pressedSinceLastPoll[i])
            {
                _pressedSinceLastPoll[i] = false;
                key = i;
                return true;
            }
        }
        key = 0;
        return false;
    }
}
