using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Chip8Emulator.Core;

namespace Chip8Emulator.Cli;

public sealed class AnsiConsoleDisplay : IDisposable
{
    private const string CursorHome = "\x1b[H";
    private const string HideCursor = "\x1b[?25l";
    private const string ShowCursor = "\x1b[?25h";
    private const string ResetAttrs = "\x1b[0m";
    private const string EnterAltScreen = "\x1b[?1049h";
    private const string ExitAltScreen = "\x1b[?1049l";
    private const string ClearScreen = "\x1b[2J";
    private const string DisableAltScroll = "\x1b[?1007l";
    private const string RestoreAltScroll = "\x1b[?1007h";

    private readonly IReadOnlyDisplay _display;
    private readonly byte[] _previousPixels;
    private readonly StringBuilder _frame = new(8192);
    private bool _hasRendered;
    private int _lastWindowWidth = -1;
    private int _lastWindowHeight = -1;
    private int _lastPixelWidth = -1;
    private int _lastPixelHeight = -1;
    private readonly string? _savedSttyState;

    public AnsiConsoleDisplay(IReadOnlyDisplay display)
    {
        _display = display;
        _previousPixels = new byte[Chip8Display.HighResWidth * Chip8Display.HighResHeight];

        Console.OutputEncoding = Encoding.UTF8;
        EnableWindowsAnsi();
        if (!OperatingSystem.IsWindows())
        {
            _savedSttyState = CaptureSttyState();
            RunStty("-echo -icanon");
        }
        Console.Write(EnterAltScreen + DisableAltScroll + ClearScreen + HideCursor + CursorHome);
        Console.Out.Flush();
    }

    public void Render()
    {
        var pixels = _display.VMem.Span;
        var pixelWidth = _display.Width;
        var pixelHeight = _display.Height;
        var cellHeight = (pixelHeight + 1) / 2;
        var activeLength = pixelWidth * pixelHeight;
        var activePixels = pixels[..activeLength];
        var previousActive = _previousPixels.AsSpan(0, activeLength);

        var (windowWidth, windowHeight) = ReadWindowSize();
        var windowResized = windowWidth != _lastWindowWidth || windowHeight != _lastWindowHeight;
        var modeChanged = pixelWidth != _lastPixelWidth || pixelHeight != _lastPixelHeight;
        if (windowResized || modeChanged)
        {
            _lastWindowWidth = windowWidth;
            _lastWindowHeight = windowHeight;
            _lastPixelWidth = pixelWidth;
            _lastPixelHeight = pixelHeight;
        }

        if (_hasRendered && !windowResized && !modeChanged && activePixels.SequenceEqual(previousActive))
        {
            return;
        }

        activePixels.CopyTo(previousActive);
        _hasRendered = true;

        _frame.Clear();
        if (windowResized || modeChanged)
        {
            _frame.Append(ClearScreen);
        }

        if (windowWidth < pixelWidth || windowHeight < cellHeight)
        {
            var message = $"Terminal too small — needs {pixelWidth}×{cellHeight}";
            var msgRow = Math.Max(1, windowHeight / 2);
            var msgCol = Math.Max(1, (windowWidth - message.Length) / 2 + 1);
            _frame.Append("\x1b[").Append(msgRow).Append(';').Append(msgCol).Append('H');
            _frame.Append(message);
        }
        else
        {
            var offsetCol = (windowWidth - pixelWidth) / 2 + 1;
            var offsetRow = (windowHeight - cellHeight) / 2 + 1;

            for (var row = 0; row < cellHeight; row++)
            {
                _frame.Append("\x1b[").Append(offsetRow + row).Append(';').Append(offsetCol).Append('H');
                var topRowOffset = row * 2 * pixelWidth;
                var bottomRowIndex = row * 2 + 1;
                var bottomRowOffset = bottomRowIndex * pixelWidth;
                var hasBottom = bottomRowIndex < pixelHeight;
                for (var col = 0; col < pixelWidth; col++)
                {
                    var top = pixels[topRowOffset + col] != 0;
                    var bottom = hasBottom && pixels[bottomRowOffset + col] != 0;
                    _frame.Append((top, bottom) switch
                    {
                        (false, false) => ' ',
                        (true, false) => '▀',
                        (false, true) => '▄',
                        (true, true) => '█',
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
            return (0, 0);
        }
    }

    public void Dispose()
    {
        Console.Write(ResetAttrs + ShowCursor + RestoreAltScroll + ExitAltScreen);
        Console.Out.Flush();
        if (_savedSttyState is not null)
        {
            RunStty(_savedSttyState);
        }
    }

    private static string? CaptureSttyState()
    {
        try
        {
            var psi = new ProcessStartInfo("stty", "-g")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
            };
            using var p = Process.Start(psi);
            if (p is null) return null;
            var output = p.StandardOutput.ReadToEnd().Trim();
            p.WaitForExit();
            return p.ExitCode == 0 && output.Length > 0 ? output : null;
        }
        catch
        {
            return null;
        }
    }

    private static void RunStty(string args)
    {
        try
        {
            var psi = new ProcessStartInfo("stty", args) { UseShellExecute = false };
            using var p = Process.Start(psi);
            p?.WaitForExit();
        }
        catch
        {
            // best-effort; if stty isn't available we just live with echo
        }
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
