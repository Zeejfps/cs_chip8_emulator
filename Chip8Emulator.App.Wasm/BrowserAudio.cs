using System.Runtime.InteropServices.JavaScript;
using Chip8Emulator.Core;

namespace Chip8Emulator.App.Wasm;

internal sealed partial class BrowserAudio : IAudio
{
    public void Beep() => BeepTick();

    [JSImport("audio.beepTick", "main.js")]
    private static partial void BeepTick();
}
