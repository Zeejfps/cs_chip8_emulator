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

    private const string TooSmallMessage = "Terminal too small \u2014 needs 64\u00d716";

    private readonly byte[] _previousPixels = new byte[PixelWidth * PixelHeight];
    private readonly StringBuilder _frame = new(8 + (PixelWidth + 8) * CellHeight);
    private bool _hasRendered;
    private int _lastWindowWidth = -1;
    private int _lastWindowHeight = -1;

    public AnsiConsoleDisplay()
    {
        Console.OutputEncoding = Encoding.UTF8;
        EnableWindowsAnsi();
        Console.Write(EnterAltScreen + ClearScreen + HideCursor + CursorHome);
        Console.Out.Flush();
    }

    public void Draw(ReadOnlySpan<byte> pixels)
    {
        var (width, height) = ReadWindowSize();
        var resized = width != _lastWindowWidth || height != _lastWindowHeight;
        if (resized)
        {
            _lastWindowWidth = width;
            _lastWindowHeight = height;
        }

        if (_hasRendered && !resized && pixels.SequenceEqual(_previousPixels))
        {
            return;
        }

        pixels.CopyTo(_previousPixels);
        _hasRendered = true;

        _frame.Clear();
        if (resized)
        {
            _frame.Append(ClearScreen);
        }

        if (width < PixelWidth || height < CellHeight)
        {
            var msgRow = Math.Max(1, height / 2);
            var msgCol = Math.Max(1, (width - TooSmallMessage.Length) / 2 + 1);
            _frame.Append("\x1b[").Append(msgRow).Append(';').Append(msgCol).Append('H');
            _frame.Append(TooSmallMessage);
        }
        else
        {
            var offsetCol = (width - PixelWidth) / 2 + 1;
            var offsetRow = (height - CellHeight) / 2 + 1;

            for (var row = 0; row < CellHeight; row++)
            {
                _frame.Append("\x1b[").Append(offsetRow + row).Append(';').Append(offsetCol).Append('H');
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
        }

        Console.Out.Write(_frame);
        Console.Out.Flush();
    }

    private static (int width, int height) ReadWindowSize()
    {
        try
        {
            return (Console.WindowWidth, Console.WindowHeight);
        }
        catch (IOException)
        {
            return (PixelWidth, CellHeight);
        }
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
