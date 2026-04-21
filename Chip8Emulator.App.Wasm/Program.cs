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
    private static PausableStopwatchClock? _clock;
    private static MemoryHandle _pixelsHandle;
    private static long _ticksPerInstruction;

    [JSExport]
    public static void Init()
    {
        _input = new BrowserInput();
        _clock = new PausableStopwatchClock();
        _machine = Chip8.Builder()
            .WithRenderer(new BrowserRenderer())
            .WithAudio(new BrowserAudio())
            .WithClock(_clock)
            .WithInput(_input)
            .Build();
        _pixelsHandle = _machine.Display.Pixels.Pin();
        _ticksPerInstruction = _clock.Frequency / _machine.InstructionsPerSecond;
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
    public static void Pause() => _clock!.Pause();

    [JSExport]
    public static void Resume() => _clock!.Resume();

    [JSExport]
    public static void Step()
    {
        _clock!.Advance(_ticksPerInstruction);
        _machine!.Update();
    }

    [JSExport]
    public static int GetProgramCounter() => _machine!.ProgramCounter;

    [JSExport]
    public static int GetMemoryByte(int address) => _machine!.Memory[address];

    [JSExport]
    public static string DisassembleInstruction(int ins) => Chip8Disassembler.Disassemble(ins);

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

    [JSExport]
    public static int GetInstructionsPerSecond() => _machine!.InstructionsPerSecond;

    [JSExport]
    public static void SetInstructionsPerSecond(int ips)
    {
        _machine!.InstructionsPerSecond = ips;
        _ticksPerInstruction = _clock!.Frequency / ips;
    }
}
