using System.Runtime.InteropServices.JavaScript;
using Chip8Emulator.Core;

namespace Chip8Emulator.Web;

internal sealed partial class BrowserAudio : IAudio
{
    private const byte DefaultPitch = 64;

    private byte _pitch;
    public byte Pitch
    {
        get => _pitch;
        set
        {
            if (_pitch == value)
                return;

            _pitch = value;
            _audioFrequencyHz = CalculateAudioFrequency(_pitch);
            SetPatternJs(_patternBuffer, _audioFrequencyHz);
        }
    }

    private readonly byte[] _patternBuffer = new byte[16];
    private double _audioFrequencyHz;

    public BrowserAudio()
    {
        _pitch = DefaultPitch;
        _audioFrequencyHz = CalculateAudioFrequency(_pitch);
    }
    
    public void WritePattern(Action<Span<byte>> writeAction)
    {
        writeAction(_patternBuffer.AsSpan());
        SetPatternJs(_patternBuffer, _audioFrequencyHz);
    }

    public bool IsPlaying { get; private set; }
    public void PlaySound() { PlaySoundJs(); IsPlaying = true; }
    public void StopSound() { StopSoundJs(); IsPlaying = false; }
    public void Reset()
    {
        _pitch = DefaultPitch;
        _audioFrequencyHz = CalculateAudioFrequency(_pitch);
        Array.Clear(_patternBuffer);
        SetPatternJs(_patternBuffer, _audioFrequencyHz);
        IsPlaying = false;
    }

    private static double CalculateAudioFrequency(byte pitch)
    {
        return 4000.0 * Math.Pow(2.0, (pitch - 64) / 48.0);
    }

    [JSImport("audio.playSound", "main.js")]
    private static partial void PlaySoundJs();

    [JSImport("audio.stopSound", "main.js")]
    private static partial void StopSoundJs();

    [JSImport("audio.setPattern", "main.js")]
    private static partial void SetPatternJs(byte[] pattern, double frequencyHz);
}
