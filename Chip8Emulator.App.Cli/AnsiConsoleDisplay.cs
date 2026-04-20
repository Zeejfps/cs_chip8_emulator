using System.Runtime.InteropServices;
using System.Text;
using Chip8Emulator.Core;

namespace Chip8Emulator.App.Cli;

public sealed class AnsiConsoleDisplay : IDisplay, IDisposable
{
    private const int PixelWidth = 64;
    private const int PixelHeight = 32;
    private const int CellHeight = PixelHeight / 2;

    private const string CursorHome = "\x1b[H";
    private const string HideCursor = "\x1b[?25l";
    private const string ShowCursor = "\x1b[?25h";
    private const string ResetAttrs = "\x1b[0m";
    private const string EnterAltScreen = "\x1b[?1049h";
    private const string ExitAltScreen = "\x1b[?1049l";
    private const string ClearScreen = "\x1b[2J";

    private readonly byte[] _previousPixels = new byte[PixelWidth * PixelHeight];
    private readonly StringBuilder _frame = new(8 + (PixelWidth + 8) * CellHeight);
    private bool _hasRendered;

    public AnsiConsoleDisplay()
    {
        Console.OutputEncoding = Encoding.UTF8;
        EnableWindowsAnsi();
        Console.Write(EnterAltScreen + ClearScreen + HideCursor + CursorHome);
        Console.Out.Flush();
    }

    public void Draw(ReadOnlySpan<byte> pixels)
    {
        if (_hasRendered && pixels.SequenceEqual(_previousPixels))
        {
            return;
        }

        pixels.CopyTo(_previousPixels);
        _hasRendered = true;

        _frame.Clear();

        for (var row = 0; row < CellHeight; row++)
        {
            _frame.Append("\x1b[").Append(row + 1).Append(";1H");
            var topRowOffset = row * 2 * PixelWidth;
            var bottomRowOffset = (row * 2 + 1) * PixelWidth;
            for (var col = 0; col < PixelWidth; col++)
            {
                var top = pixels[topRowOffset + col] != 0;
                var bottom = pixels[bottomRowOffset + col] != 0;
                _frame.Append((top, bottom) switch
                {
                    (false, false) => ' ',
                    (true, false) => '\u2580',
                    (false, true) => '\u2584',
                    (true, true) => '\u2588',
                });
            }
        }

        Console.Out.Write(_frame);
        Console.Out.Flush();
    }

    public void Dispose()
    {
        Console.Write(ResetAttrs + ShowCursor + ExitAltScreen);
        Console.Out.Flush();
    }

    private static void EnableWindowsAnsi()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        const int stdOutputHandle = -11;
        const uint enableVirtualTerminalProcessing = 0x0004;

        var handle = GetStdHandle(stdOutputHandle);
        if (handle == IntPtr.Zero || handle == new IntPtr(-1))
        {
            return;
        }

        if (!GetConsoleMode(handle, out var mode))
        {
            return;
        }

        SetConsoleMode(handle, mode | enableVirtualTerminalProcessing);
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);
}
