using System.Diagnostics;
using Emulator.Api;

namespace Emulator.Impl;

internal sealed class Chip8Emulator : IChip8
{
    private readonly IDisplay _display;
    private readonly IAudio _audio;
    
    private byte[] _memory = new byte[4096];
    private byte[] _font =
    [
        0xF0, 0x90, 0x90, 0x90, 0xF0, // 0
        0x20, 0x60, 0x20, 0x20, 0x70, // 1
        0xF0, 0x10, 0xF0, 0x80, 0xF0, // 2
        0xF0, 0x10, 0xF0, 0x10, 0xF0, // 3
        0x90, 0x90, 0xF0, 0x10, 0x10, // 4
        0xF0, 0x80, 0xF0, 0x10, 0xF0, // 5
        0xF0, 0x80, 0xF0, 0x90, 0xF0, // 6
        0xF0, 0x10, 0x20, 0x40, 0x40, // 7
        0xF0, 0x90, 0xF0, 0x90, 0xF0, // 8
        0xF0, 0x90, 0xF0, 0x10, 0xF0, // 9
        0xF0, 0x90, 0xF0, 0x90, 0x90, // A
        0xE0, 0x90, 0xE0, 0x90, 0xE0, // B
        0xF0, 0x80, 0x80, 0x80, 0xF0, // C
        0xE0, 0x90, 0x90, 0x90, 0xE0, // D
        0xF0, 0x80, 0xF0, 0x80, 0xF0, // E
        0xF0, 0x80, 0xF0, 0x80, 0x80  // F
    ];

    private byte _delayTimer;
    private byte _soundTimer;
    private Stack<ushort> _stack = new();
    private const double FrameTimeInSeconds = 1.0 / 60.0;
    
    public Chip8Emulator(IDisplay display, IAudio audio)
    {
        _display = display;
        _audio = audio;
    }

    public void Execute(ReadOnlySpan<byte> program)
    {
        var startTime = Stopwatch.GetTimestamp();
        var totalElapsedSeconds = 0.0;
        while (true)
        {
            var endTime = Stopwatch.GetTimestamp();
            var elapsed = endTime - startTime;
            totalElapsedSeconds += elapsed / (double) Stopwatch.Frequency ;
            startTime = endTime;
            if (totalElapsedSeconds >= FrameTimeInSeconds)
            {
                if (_delayTimer > 0)
                {
                    _delayTimer--;
                }
                
                if (_soundTimer > 0)
                {
                    _audio.Beep();
                    _soundTimer--;
                }
                
                _display.Update();
                totalElapsedSeconds = 0;
            }
        }
    }
}