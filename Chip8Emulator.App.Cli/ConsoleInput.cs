using System.Diagnostics;
using Chip8Emulator.Core;

namespace Chip8Emulator.App.Cli;

public sealed class ConsoleInput : IInput, IDisposable
{
    private const long DecayMilliseconds = 150;
    private const int NoPendingKey = -1;

    private readonly long[] _lastSeenTicks = new long[16];
    private volatile bool _stopping;
    private volatile bool _isCancelRequested;
    private int _pendingKey = NoPendingKey;
    private int _restartRequested;

    public ConsoleInput()
    {
        var readerThread = new Thread(ReaderLoop)
        {
            IsBackground = true,
            Name = "ConsoleInput-Reader",
        };
        readerThread.Start();
    }

    public bool IsCancelRequested => _isCancelRequested;

    public bool ConsumeRestartRequest()
        => Interlocked.Exchange(ref _restartRequested, 0) == 1;

    public bool IsKeyPressed(byte key)
    {
        if (key >= 16) return false;
        var elapsedTicks = Stopwatch.GetTimestamp() - _lastSeenTicks[key];
        var elapsedMs = elapsedTicks * 1000 / Stopwatch.Frequency;
        return elapsedMs < DecayMilliseconds;
    }

    public bool WasAnyKeyPressed(out byte key)
    {
        var pending = Interlocked.Exchange(ref _pendingKey, NoPendingKey);
        if (pending >= 0)
        {
            key = (byte)pending;
            return true;
        }
        key = 0;
        return false;
    }

    private void ReaderLoop()
    {
        while (!_stopping)
        {
            ConsoleKeyInfo info;
            try
            {
                info = Console.ReadKey(intercept: true);
            }
            catch (InvalidOperationException)
            {
                return;
            }

            if (info.Key == ConsoleKey.Escape)
            {
                _isCancelRequested = true;
                return;
            }

            if (info.Key == ConsoleKey.F5)
            {
                Interlocked.Exchange(ref _restartRequested, 1);
                continue;
            }

            if (TryMap(info.Key, out var chip8Key))
            {
                _lastSeenTicks[chip8Key] = Stopwatch.GetTimestamp();
                Interlocked.Exchange(ref _pendingKey, chip8Key);
            }
        }
    }

    private static bool TryMap(ConsoleKey key, out byte chip8Key)
    {
        chip8Key = key switch
        {
            ConsoleKey.D1 => 0x1,
            ConsoleKey.D2 => 0x2,
            ConsoleKey.D3 => 0x3,
            ConsoleKey.D4 => 0xC,
            ConsoleKey.Q => 0x4,
            ConsoleKey.W => 0x5,
            ConsoleKey.E => 0x6,
            ConsoleKey.R => 0xD,
            ConsoleKey.A => 0x7,
            ConsoleKey.S => 0x8,
            ConsoleKey.D => 0x9,
            ConsoleKey.F => 0xE,
            ConsoleKey.Z => 0xA,
            ConsoleKey.X => 0x0,
            ConsoleKey.C => 0xB,
            ConsoleKey.V => 0xF,
            _ => 0xFF,
        };
        return chip8Key != 0xFF;
    }

    public void Dispose()
    {
        _stopping = true;
    }
}
