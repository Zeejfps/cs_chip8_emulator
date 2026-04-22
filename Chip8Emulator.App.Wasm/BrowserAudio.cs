using System.Runtime.InteropServices.JavaScript;
using Chip8Emulator.Core;

namespace Chip8Emulator.App.Wasm;

internal sealed partial class BrowserAudio : IAudio
{
    public void PlaySound() => PlaySoundJs();
    public void StopSound() => StopSoundJs();

    public void SetPattern(ReadOnlySpan<byte> pattern, double frequencyHz)
    {
        Span<byte> buffer = stackalloc byte[16];
        pattern[..Math.Min(pattern.Length, buffer.Length)].CopyTo(buffer);
        SetPatternJs(buffer.ToArray(), frequencyHz);
    }

    [JSImport("audio.playSound", "main.js")]
    private static partial void PlaySoundJs();

    [JSImport("audio.stopSound", "main.js")]
    private static partial void StopSoundJs();

    [JSImport("audio.setPattern", "main.js")]
    private static partial void SetPatternJs(byte[] pattern, double frequencyHz);
}
