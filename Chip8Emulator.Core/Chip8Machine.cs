namespace Chip8Emulator.Core;

internal sealed partial class Chip8Machine : IChip8Machine
{
    public const int LowResFontBaseAddress = 0x050;
    public const int HighResFontBaseAddress = 0x0A0;
    public const int LowRestFontCharWidth = 5;
    public const int HighRestFontCharWidth = 10;

    private readonly IClock _clock;
    private readonly IDisplay _display;
    private readonly IMemory _memory;
    private readonly EmulatedCpu _cpu;

    private readonly long _ticksPerFrame;
    private long _ticksPerInstruction;
    private int _instructionsPerSecond = 1000;
    private long _lastTimestamp;
    private long _instructionAcc;
    private long _frameAcc;
    private bool _running;

    public Chip8Machine(IClock clock, IDisplay display, IMemory memory, EmulatedCpu cpu)
    {
        _clock = clock;
        _display = display;
        _memory = memory;
        _cpu = cpu;

        _ticksPerFrame = clock.Frequency / 60;
        _ticksPerInstruction = clock.Frequency / _instructionsPerSecond;
        _lastTimestamp = clock.Timestamp;

        ResetMemory();
    }

    public IDisplay Display => _display;
    public IMemory Memory => _memory;
    public EmulatedCpu Cpu => _cpu;
    ICpu IChip8Machine.Cpu => _cpu;

    public int InstructionsPerSecond
    {
        get => _instructionsPerSecond;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
            _instructionsPerSecond = value;
            _ticksPerInstruction = _clock.Frequency / value;
            _instructionAcc = 0;
        }
    }

    public void LoadProgram(ReadOnlySpan<byte> program)
    {
        ResetMemory();
        _cpu.Reset(programCounter: 0x200);
        _instructionAcc = 0;
        _frameAcc = 0;
        _lastTimestamp = _clock.Timestamp;
        _memory.Write(0x200, program);

        // Classic CHIP-8 HIRES signature: programs starting with `1260` (JP 0x260)
        // switch the display to a 64x64 canvas. See Hans Christian Egeberg / David Winter.
        if (program.Length >= 2 && program[0] == 0x12 && program[1] == 0x60)
        {
            _display.EnableClassicHiresMode();
        }
    }

    private void ResetMemory()
    {
        _memory.Clear();
        _memory.Write(LowResFontBaseAddress, LowResFont);
        _memory.Write(HighResFontBaseAddress, HighResFont);
    }

    public void Start()
    {
        if (_running) throw new InvalidOperationException("Machine is already started.");
        _lastTimestamp = _clock.Timestamp;
        _clock.Ticked += OnTicked;
        _running = true;
        if (_cpu.Registers.ReadSt() > 0) _cpu.Audio.PlaySound();
    }

    public void Stop()
    {
        if (!_running) return;
        _clock.Ticked -= OnTicked;
        _running = false;
        if (_cpu.Registers.ReadSt() > 0) _cpu.Audio.StopSound();
    }

    private void OnTicked(object? sender, EventArgs e)
    {
        var now = _clock.Timestamp;
        var delta = now - _lastTimestamp;
        _lastTimestamp = now;
        if (delta == 0) return;

        var maxDelta = _ticksPerFrame * 2;
        if (delta > maxDelta) delta = maxDelta;

        _cpu.TryResumeFromKeyPress();

        _frameAcc += delta;

        if (!_cpu.CanExecute)
        {
            _instructionAcc = 0;
        }
        else
        {
            _instructionAcc += delta;
            while (_instructionAcc >= _ticksPerInstruction)
            {
                _cpu.FetchDecodeExecute();
                _instructionAcc -= _ticksPerInstruction;
                if (!_cpu.CanExecute)
                {
                    _instructionAcc = 0;
                    break;
                }
            }
        }

        while (_frameAcc >= _ticksPerFrame)
        {
            StepFrame();
        }
    }

    private void StepFrame()
    {
        var registers = _cpu.Registers;

        var dt = registers.ReadDt();
        if (dt > 0) registers.WriteDt((byte)(dt - 1));

        var st = registers.ReadSt();
        if (st > 0)
        {
            st--;
            registers.WriteSt(st);
            if (st == 0) _cpu.Audio.StopSound();
        }

        _display.Render();
        _frameAcc -= _ticksPerFrame;
        _cpu.ClearVBlankWait();
    }
}
