using System.Buffers;
using System.Runtime.InteropServices.JavaScript;
using Chip8Emulator.App;
using Chip8Emulator.App.Wasm;
using Chip8Emulator.Core;

await Task.CompletedTask;

public static partial class Interop
{
    private static IChip8Machine? _machine;
    private static BrowserInput? _input;
    private static StopwatchClock? _clock;
    private static MemoryHandle _pixelsHandle;

    [JSExport]
    public static void Init()
    {
        _input = new BrowserInput();
        _clock = new StopwatchClock();
        _machine = Chip8.Builder()
            .WithRenderer(new BrowserRenderer())
            .WithAudio(new BrowserAudio())
            .WithClock(_clock)
            .WithInput(_input)
            .Build();
        _pixelsHandle = _machine.Display.Pixels.Pin();
    }

    [JSExport]
    public static void LoadProgram(byte[] rom)
    {
        _machine!.LoadProgram(rom);
    }

    [JSExport]
    public static void Update()
    {
        _machine!.Update();
    }

    [JSExport]
    public static unsafe int GetPixelDataPtr() => (int)_pixelsHandle.Pointer;

    [JSExport]
    public static int GetPixelDataLength() => _machine!.Display.Pixels.Length;

    [JSExport]
    public static int GetWidth() => _machine!.Display.Width;

    [JSExport]
    public static int GetHeight() => _machine!.Display.Height;

    [JSExport]
    public static void SetKey(int key, bool pressed) => _input!.SetKey((byte)key, pressed);
}
