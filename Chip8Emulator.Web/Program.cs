using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices.JavaScript;
using Chip8Emulator.Core;

await Task.CompletedTask;

namespace Chip8Emulator.Web
{
    public static partial class Interop
    {
        private static IInterpreter? _interpreter;
        private static BrowserInput? _input;
        private static ManualClock? _clock;
        private static MemoryHandle _pixelsHandle;
        private static long _lastRealTimestamp;

        [JSExport]
        public static void Init()
        {
            _input = new BrowserInput();
            _clock = new ManualClock();
            _interpreter = Chip8.Builder()
                .WithAudio(new BrowserAudio())
                .WithClock(_clock)
                .WithInput(_input)
                .WithRenderer(new BrowserRenderer())
                .WithPersistentFlags(new LocalStoragePersistentFlags())
                .Build();
            _pixelsHandle = _interpreter.Display.VMem.Pin();
            _lastRealTimestamp = Stopwatch.GetTimestamp();
        }

        [JSExport]
        public static void LoadProgram(byte[] rom)
        {
            _interpreter!.LoadProgram(rom);
            _lastRealTimestamp = Stopwatch.GetTimestamp();
        }

        [JSExport]
        public static void Tick()
        {
            var now = Stopwatch.GetTimestamp();
            var delta = now - _lastRealTimestamp;
            _lastRealTimestamp = now;
            _clock!.Advance(delta);
        }

        [JSExport]
        public static void Start()
        {
            _interpreter!.Start();
            _lastRealTimestamp = Stopwatch.GetTimestamp();
        }

        [JSExport]
        public static void Stop() => _interpreter!.Stop();

        [JSExport]
        public static void Step()
        {
            // A vblank-wait draw suspends instruction execution until ~1 frame of ticks accumulates,
            // so a single one-instruction advance can land in a no-op window. Loop up to
            // (steps-per-frame + 1) times until the PC actually moves.
            var pcBefore = _interpreter!.Registers.ReadPc();
            var delta = _clock!.Frequency / _interpreter!.InstructionsPerSecond;
            var maxSteps = _interpreter.InstructionsPerSecond / 60 + 1;
            for (var i = 0; i < maxSteps; i++)
            {
                _clock.Advance(delta);
                if (_interpreter.Registers.ReadPc() != pcBefore) return;
            }
        }

        [JSExport]
        public static int GetProgramCounter() => _interpreter!.Registers.ReadPc();

        [JSExport]
        public static int GetMemoryByte(int address) => _interpreter!.Memory.Read(address);

        [JSExport]
        public static byte[] GetVRegisters() => _interpreter!.Registers.VRegisters.ToArray();

        [JSExport]
        public static int GetIndexRegister() => _interpreter!.Registers.ReadI();

        [JSExport]
        public static int GetDelayTimer() => _interpreter!.Registers.ReadDt();

        [JSExport]
        public static int GetSoundTimer() => _interpreter!.Registers.ReadSt();

        [JSExport]
        public static int GetStackPointer() => _interpreter!.Stack.StackPointer;

        [JSExport]
        public static int[] GetStack() => _interpreter!.Stack.Frames.ToArray();

        [JSExport]
        public static string DisassembleInstruction(int ins) => Chip8Disassembler.Disassemble(ins);

        [JSExport]
        public static unsafe int GetPixelDataPtr() => (int)_pixelsHandle.Pointer;

        [JSExport]
        public static int GetPixelDataLength() => _interpreter!.Display.VMem.Length;

        [JSExport]
        public static int GetWidth() => _interpreter!.Display.Width;

        [JSExport]
        public static int GetHeight() => _interpreter!.Display.Height;

        [JSExport]
        public static void SetKey(int key, bool pressed) => _input!.SetKey((byte)key, pressed);

        [JSExport]
        public static int GetInstructionsPerSecond() => _interpreter!.InstructionsPerSecond;

        [JSExport]
        public static void SetInstructionsPerSecond(int ips) => _interpreter!.InstructionsPerSecond = ips;

        [JSExport]
        public static bool GetShiftUsesVy() => _interpreter!.ShiftUsesVy;

        [JSExport]
        public static void SetShiftUsesVy(bool value) => _interpreter!.ShiftUsesVy = value;

        [JSExport]
        public static bool GetJumpUsesVx() => _interpreter!.JumpUsesVx;

        [JSExport]
        public static void SetJumpUsesVx(bool value) => _interpreter!.JumpUsesVx = value;

        [JSExport]
        public static bool GetLoadStoreIncrementsI() => _interpreter!.LoadStoreIncrementsI;

        [JSExport]
        public static void SetLoadStoreIncrementsI(bool value) => _interpreter!.LoadStoreIncrementsI = value;

        [JSExport]
        public static bool GetLogicResetsVf() => _interpreter!.LogicResetsVf;

        [JSExport]
        public static void SetLogicResetsVf(bool value) => _interpreter!.LogicResetsVf = value;

        [JSExport]
        public static bool GetSpritesWrap() => _interpreter!.SpritesWrap;

        [JSExport]
        public static void SetSpritesWrap(bool value) => _interpreter!.SpritesWrap = value;

        [JSExport]
        public static bool GetDisplayWait() => _interpreter!.DisplayWait;

        [JSExport]
        public static void SetDisplayWait(bool value) => _interpreter!.DisplayWait = value;

        [JSExport]
        public static bool GetVfResultWrittenLast() => _interpreter!.VfResultWrittenLast;

        [JSExport]
        public static void SetVfResultWrittenLast(bool value) => _interpreter!.VfResultWrittenLast = value;
    }
}
