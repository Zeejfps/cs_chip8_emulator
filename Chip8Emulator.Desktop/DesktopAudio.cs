using Chip8Emulator.Core;
using Silk.NET.OpenAL;

namespace Chip8Emulator.Desktop;

internal sealed unsafe class DesktopAudio : IAudio, IDisposable
{
    private const byte DefaultPitch = 64;
    private const int SampleRate = 22050;
    private const float DefaultBeepHz = 440.0f;

    private readonly AL _al;
    private readonly ALContext _alc;
    private readonly Device* _device;
    private readonly Context* _context;
    private readonly uint _source;
    private uint _buffer;

    private byte _pitch = DefaultPitch;
    private double _frequencyHz = CalculateFrequency(DefaultPitch);
    private readonly byte[] _pattern = new byte[16];
    private bool _patternIsCustom;
    private bool _bufferDirty = true;
    private bool _isPlaying;

    public DesktopAudio()
    {
        _alc = ALContext.GetApi();
        _al = AL.GetApi();
        _device = _alc.OpenDevice("");
        _context = _alc.CreateContext(_device, null);
        _alc.MakeContextCurrent(_context);
        _source = _al.GenSource();
        _buffer = _al.GenBuffer();
        _al.SetSourceProperty(_source, SourceBoolean.Looping, true);
    }

    public byte Pitch
    {
        get => _pitch;
        set
        {
            if (_pitch == value) return;
            _pitch = value;
            _frequencyHz = CalculateFrequency(_pitch);
            _bufferDirty = true;
            if (_isPlaying) RebindBuffer();
        }
    }

    public bool IsPlaying => _isPlaying;

    public void WritePattern(Action<Span<byte>> writeAction)
    {
        writeAction(_pattern.AsSpan());
        _patternIsCustom = false;
        foreach (var b in _pattern)
        {
            if (b != 0) { _patternIsCustom = true; break; }
        }
        _bufferDirty = true;
        if (_isPlaying) RebindBuffer();
    }

    public void PlaySound()
    {
        if (_isPlaying) return;
        RebindBuffer();
        _al.SourcePlay(_source);
        _isPlaying = true;
    }

    public void StopSound()
    {
        if (!_isPlaying) return;
        _al.SourceStop(_source);
        _isPlaying = false;
    }

    public void Reset()
    {
        StopSound();
        _pitch = DefaultPitch;
        _frequencyHz = CalculateFrequency(_pitch);
        Array.Clear(_pattern);
        _patternIsCustom = false;
        _bufferDirty = true;
    }

    private void RebindBuffer()
    {
        if (!_bufferDirty) return;

        // Detach current buffer before re-uploading.
        _al.SetSourceProperty(_source, SourceInteger.Buffer, 0);

        var pcm = RenderPcm();
        fixed (byte* ptr = pcm)
        {
            _al.BufferData(_buffer, BufferFormat.Mono16, ptr, pcm.Length, SampleRate);
        }
        _al.SetSourceProperty(_source, SourceInteger.Buffer, (int)_buffer);
        _bufferDirty = false;
    }

    private byte[] RenderPcm()
    {
        // Render ~100ms of audio (2205 samples). Looping covers the rest.
        // For square wave: align length to integer multiple of period to minimise loop click.
        // For pattern: render the full 128-bit cycle at the bit rate (= _frequencyHz).
        if (_patternIsCustom)
        {
            // Pattern repeats every 128 bits, each bit lasts 1/_frequencyHz seconds.
            var bitsPerSecond = _frequencyHz;
            if (bitsPerSecond <= 0) bitsPerSecond = 1;
            var samplesPerCycle = (int)Math.Round(SampleRate * 128.0 / bitsPerSecond);
            if (samplesPerCycle < 2) samplesPerCycle = 2;
            var pcm = new byte[samplesPerCycle * 2];
            for (var i = 0; i < samplesPerCycle; i++)
            {
                var t = i / (double)SampleRate;
                var bitIndex = (int)(t * bitsPerSecond) & 0x7F;
                var byteIndex = bitIndex >> 3;
                var bitInByte = 7 - (bitIndex & 7);
                var on = (_pattern[byteIndex] >> bitInByte) & 1;
                short sample = (short)(on != 0 ? 16000 : -16000);
                pcm[i * 2] = (byte)(sample & 0xFF);
                pcm[i * 2 + 1] = (byte)((sample >> 8) & 0xFF);
            }
            return pcm;
        }

        // Square wave at DefaultBeepHz (Chip8 default beeper). Use integer-cycle length
        // so loop is seamless.
        var freq = DefaultBeepHz;
        var samplesPerHz = SampleRate / (double)freq;
        // 10 cycles for a smooth loop.
        var lenSamples = (int)Math.Round(samplesPerHz * 10);
        if (lenSamples < 4) lenSamples = 4;
        var buf = new byte[lenSamples * 2];
        for (var i = 0; i < lenSamples; i++)
        {
            var phase = (i * freq / SampleRate) - Math.Floor(i * freq / SampleRate);
            short sample = (short)(phase < 0.5 ? 16000 : -16000);
            buf[i * 2] = (byte)(sample & 0xFF);
            buf[i * 2 + 1] = (byte)((sample >> 8) & 0xFF);
        }
        return buf;
    }

    private static double CalculateFrequency(byte pitch)
    {
        return 4000.0 * Math.Pow(2.0, (pitch - 64) / 48.0);
    }

    public void Dispose()
    {
        try
        {
            _al.SourceStop(_source);
            _al.DeleteSource(_source);
            _al.DeleteBuffer(_buffer);
        }
        catch { }
        _alc.MakeContextCurrent(null);
        if (_context != null) _alc.DestroyContext(_context);
        if (_device != null) _alc.CloseDevice(_device);
        _al.Dispose();
        _alc.Dispose();
    }
}
