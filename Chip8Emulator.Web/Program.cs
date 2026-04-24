using System.Buffers;
using System.Runtime.InteropServices.JavaScript;
using Chip8Emulator.Core;

await Task.CompletedTask;

namespace Chip8Emulator.Web
{
    public static partial class Interop
    {
        private static IChip8Interpreter? _interpreter;
        private static BrowserInput? _input;
        private static StopwatchClock? _clock;
        private static MemoryHandle _pixelsHandle;
        private static int[]? _stackBuffer;
        private static byte[]? _memoryBuffer;
        private static byte[]? _vRegistersBuffer;
        private static byte[]? _pixelBuffer;
        private static IMemory? _memory;
        private static IDisplay? _display;

        [JSExport]
        public static void Init()
        {
            _input = new BrowserInput();
            _clock = new StopwatchClock();
            _stackBuffer = new int[16];
            _memoryBuffer = new byte[4096];
            _vRegistersBuffer = new byte[16];
            _pixelBuffer = new byte[Chip8Display.HighResWidth * Chip8Display.HighResHeight];
            var stack = new Chip8Stack(size => _stackBuffer.AsMemory(0, size));
            _memory = new Chip8Memory(size => _memoryBuffer.AsMemory(0, size));
            var registers = new Chip8Registers(size => _vRegistersBuffer.AsMemory(0, size));
            _display = new Chip8Display(size => _pixelBuffer.AsMemory(0, size));
            _interpreter = Chip8.Builder()
                .WithDisplay(_display)
                .WithAudio(new BrowserAudio())
                .WithClock(_clock)
                .WithInput(_input)
                .WithStack(stack)
                .WithMemory(_memory)
                .WithRegisters(registers)
                .WithPersistentFlags(new LocalStoragePersistentFlags())
                .Build();
            _pixelsHandle = _pixelBuffer.AsMemory().Pin();
        }

        [JSExport]
        public static void LoadProgram(byte[] rom)
        {
            _interpreter!.LoadProgram(rom);
        }

        [JSExport]
        public static void Tick() => _clock!.Tick();

        [JSExport]
        public static void Start() => _interpreter!.Start();

        [JSExport]
        public static void Stop() => _interpreter!.Stop();

        [JSExport]
        public static void Step() => _interpreter!.Cpu.FetchDecodeExecute();

        [JSExport]
        public static int GetProgramCounter() => _interpreter!.Cpu.ReadProgramCounter();

        [JSExport]
        public static int GetMemoryByte(int address) => _memory!.Read(address);

        [JSExport]
        public static byte[] GetVRegisters() => _vRegistersBuffer!;

        [JSExport]
        public static int GetIndexRegister() => _interpreter!.Cpu.Registers.ReadI();

        [JSExport]
        public static int GetDelayTimer() => _interpreter!.Cpu.Registers.ReadDt();

        [JSExport]
        public static int GetSoundTimer() => _interpreter!.Cpu.Registers.ReadSt();

        [JSExport]
        public static int GetStackPointer() => _interpreter!.Cpu.Stack.StackPointer;

        [JSExport]
        public static int[] GetStack() => _stackBuffer!;

        [JSExport]
        public static string DisassembleInstruction(int ins) => Chip8Disassembler.Disassemble(ins);

        [JSExport]
        public static unsafe int GetPixelDataPtr() => (int)_pixelsHandle.Pointer;

        [JSExport]
        public static int GetPixelDataLength() => _pixelBuffer!.Length;

        [JSExport]
        public static int GetWidth() => _display!.Width;

        [JSExport]
        public static int GetHeight() => _display!.Height;

        [JSExport]
        public static void SetKey(int key, bool pressed) => _input!.SetKey((byte)key, pressed);

        [JSExport]
        public static int GetInstructionsPerSecond() => _interpreter!.InstructionsPerSecond;

        [JSExport]
        public static void SetInstructionsPerSecond(int ips) => _interpreter!.InstructionsPerSecond = ips;

        [JSExport]
        public static bool GetShiftUsesVy() => _interpreter!.Cpu.ShiftUsesVy;

        [JSExport]
        public static void SetShiftUsesVy(bool value) => _interpreter!.Cpu.ShiftUsesVy = value;

        [JSExport]
        public static bool GetJumpUsesVx() => _interpreter!.Cpu.JumpUsesVx;

        [JSExport]
        public static void SetJumpUsesVx(bool value) => _interpreter!.Cpu.JumpUsesVx = value;

        [JSExport]
        public static bool GetLoadStoreIncrementsI() => _interpreter!.Cpu.LoadStoreIncrementsI;

        [JSExport]
        public static void SetLoadStoreIncrementsI(bool value) => _interpreter!.Cpu.LoadStoreIncrementsI = value;

        [JSExport]
        public static bool GetLogicResetsVf() => _interpreter!.Cpu.LogicResetsVf;

        [JSExport]
        public static void SetLogicResetsVf(bool value) => _interpreter!.Cpu.LogicResetsVf = value;

        [JSExport]
        public static bool GetSpritesWrap() => _interpreter!.Cpu.SpritesWrap;

        [JSExport]
        public static void SetSpritesWrap(bool value) => _interpreter!.Cpu.SpritesWrap = value;

        [JSExport]
        public static bool GetDisplayWait() => _interpreter!.Cpu.DisplayWait;

        [JSExport]
        public static void SetDisplayWait(bool value) => _interpreter!.Cpu.DisplayWait = value;

        [JSExport]
        public static bool GetVfResultWrittenLast() => _interpreter!.Cpu.VfResultWrittenLast;

        [JSExport]
        public static void SetVfResultWrittenLast(bool value) => _interpreter!.Cpu.VfResultWrittenLast = value;
    }
}
