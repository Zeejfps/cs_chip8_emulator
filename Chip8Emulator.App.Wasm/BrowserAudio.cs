using System.Runtime.InteropServices.JavaScript;
using Chip8Emulator.Core;

namespace Chip8Emulator.App.Wasm;

internal sealed partial class BrowserAudio : IAudio
{
    public void PlaySound() => PlaySoundJs();
    public void StopSound() => StopSoundJs();

    [JSImport("audio.playSound", "main.js")]
    private static partial void PlaySoundJs();

    [JSImport("audio.stopSound", "main.js")]
    private static partial void StopSoundJs();
}
