using System.Buffers;
using System.Runtime.InteropServices.JavaScript;
using Chip8Emulator.Core;

await Task.CompletedTask;

namespace Chip8Emulator.Web
{
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
                .WithPersistentFlags(new LocalStoragePersistentFlags())
                .Build();
            _pixelsHandle = _machine.Display.Pixels.Pin();
        }

        [JSExport]
        public static void LoadProgram(byte[] rom)
        {
            _machine!.LoadProgram(rom);
        }

        [JSExport]
        public static void Tick() => _clock!.Tick();

        [JSExport]
        public static void Start() => _machine!.Start();

        [JSExport]
        public static void Stop() => _machine!.Stop();

        [JSExport]
        public static void Step() => _machine!.Debugger.StepInstruction();

        [JSExport]
        public static int GetProgramCounter() => _machine!.Debugger.ProgramCounter;

        [JSExport]
        public static int GetMemoryByte(int address) => _machine!.Debugger.Memory[address];

        [JSExport]
        public static byte[] GetVRegisters() => _machine!.Debugger.Registers.ToArray();

        [JSExport]
        public static int GetIndexRegister() => _machine!.Debugger.IndexRegister;

        [JSExport]
        public static int GetDelayTimer() => _machine!.Debugger.DelayTimer;

        [JSExport]
        public static int GetSoundTimer() => _machine!.Debugger.SoundTimer;

        [JSExport]
        public static int GetStackPointer() => _machine!.Debugger.StackPointer;

        [JSExport]
        public static int[] GetStack() => _machine!.Debugger.Stack.ToArray();

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
        public static void SetInstructionsPerSecond(int ips) => _machine!.InstructionsPerSecond = ips;

        [JSExport]
        public static bool GetShiftUsesVy() => _machine!.ShiftUsesVy;

        [JSExport]
        public static void SetShiftUsesVy(bool value) => _machine!.ShiftUsesVy = value;

        [JSExport]
        public static bool GetJumpUsesVx() => _machine!.JumpUsesVx;

        [JSExport]
        public static void SetJumpUsesVx(bool value) => _machine!.JumpUsesVx = value;

        [JSExport]
        public static bool GetLoadStoreIncrementsI() => _machine!.LoadStoreIncrementsI;

        [JSExport]
        public static void SetLoadStoreIncrementsI(bool value) => _machine!.LoadStoreIncrementsI = value;

        [JSExport]
        public static bool GetLogicResetsVf() => _machine!.LogicResetsVf;

        [JSExport]
        public static void SetLogicResetsVf(bool value) => _machine!.LogicResetsVf = value;

        [JSExport]
        public static bool GetSpritesWrap() => _machine!.SpritesWrap;

        [JSExport]
        public static void SetSpritesWrap(bool value) => _machine!.SpritesWrap = value;

        [JSExport]
        public static bool GetDisplayWait() => _machine!.DisplayWait;

        [JSExport]
        public static void SetDisplayWait(bool value) => _machine!.DisplayWait = value;

        [JSExport]
        public static bool GetVfResultWrittenLast() => _machine!.VfResultWrittenLast;

        [JSExport]
        public static void SetVfResultWrittenLast(bool value) => _machine!.VfResultWrittenLast = value;
    }
}
