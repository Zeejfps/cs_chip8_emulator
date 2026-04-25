using Avalonia.Input;
using Chip8Emulator.Core;

namespace Chip8Emulator.Desktop;

internal sealed class DesktopInput : IInput
{
    private readonly bool[] _keys = new bool[16];
    private readonly bool[] _armed = new bool[16];
    private readonly bool[] _pressAndReleasePending = new bool[16];

    private static readonly Dictionary<Key, byte> KeyMap = new()
    {
        { Key.D1, 0x1 }, { Key.D2, 0x2 }, { Key.D3, 0x3 }, { Key.D4, 0xC },
        { Key.Q,  0x4 }, { Key.W,  0x5 }, { Key.E,  0x6 }, { Key.R,  0xD },
        { Key.A,  0x7 }, { Key.S,  0x8 }, { Key.D,  0x9 }, { Key.F,  0xE },
        { Key.Z,  0xA }, { Key.X,  0x0 }, { Key.C,  0xB }, { Key.V,  0xF },
    };

    public bool TryHandleKeyDown(Key key)
    {
        if (!KeyMap.TryGetValue(key, out var chipKey)) return false;
        SetKey(chipKey, true);
        return true;
    }

    public bool TryHandleKeyUp(Key key)
    {
        if (!KeyMap.TryGetValue(key, out var chipKey)) return false;
        SetKey(chipKey, false);
        return true;
    }

    public void Reset()
    {
        Array.Clear(_keys);
        Array.Clear(_armed);
        Array.Clear(_pressAndReleasePending);
    }

    private void SetKey(byte key, bool pressed)
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
