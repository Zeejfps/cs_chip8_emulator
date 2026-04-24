namespace Chip8Emulator.Core;

internal sealed partial class Chip8Interpreter : IChip8Interpreter
{
    public const int LowResFontBaseAddress = 0x050;
    public const int HighResFontBaseAddress = 0x0A0;
    public const int LowRestFontCharWidth = 5;
    public const int HighRestFontCharWidth = 10;

    public IDisplay Display { get; }
    public IMemory Memory { get; }
    public ICpu Cpu { get; }
    
    public bool IsWaitingForKey => _isWaitingForKey;

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
    
    private readonly IClock _clock;
    private readonly IAudio _audio;
    private readonly IInput _input;
    private readonly IBus _bus;

    private readonly long _ticksPerFrame;
    private long _ticksPerInstruction;
    private int _instructionsPerSecond = 1000;
    private long _lastTimestamp;
    private long _instructionAcc;
    private long _frameAcc;
    private bool _running;

    private bool _isWaitingForKey;
    private int _keyRegisterIndex;
    private bool _waitForVBlank;

    public Chip8Interpreter(IClock clock, IDisplay display, IMemory memory, IAudio audio, IInput input, IBus bus, ICpu cpu)
    {
        _clock = clock;
        Display = display;
        Memory = memory;
        _audio = audio;
        _input = input;
        _bus = bus;
        Cpu = cpu;

        _ticksPerFrame = clock.Frequency / 60;
        _ticksPerInstruction = clock.Frequency / _instructionsPerSecond;
        _lastTimestamp = clock.Timestamp;

        _bus.Subscribe<SetPitchEvent>(OnSetPitch);
        _bus.Subscribe<LoadAudioPatternEvent>(OnLoadAudioPattern);
        _bus.Subscribe<KeyIsPressedSkipEvent>(OnKeyIsPressedSkip);
        _bus.Subscribe<KeyIsReleasedSkipEvent>(OnKeyIsReleasedSkip);
        _bus.Subscribe<BeginWaitForKeyEvent>(OnBeginWaitForKey);
        _bus.Subscribe<BeginWaitForVBlankEvent>(OnBeginWaitForVBlank);

        ResetMemory();
    }

    public void LoadProgram(ReadOnlySpan<byte> program)
    {
        ResetMemory();
        Cpu.Registers.Clear();
        Cpu.Stack.Clear();
        Cpu.WriteProgramCounter(0x200);
        Memory.Write(0x200, program);
        
        _audio.Reset();
        Display.Reset();
        
        _isWaitingForKey = false;
        _keyRegisterIndex = 0;
        _waitForVBlank = false;
        _instructionAcc = 0;
        _frameAcc = 0;
        _lastTimestamp = _clock.Timestamp;
        
        // Classic CHIP-8 HIRES signature: programs starting with `1260` (JP 0x260)
        // switch the display to a 64x64 canvas. See Hans Christian Egeberg / David Winter.
        if (program.Length >= 2 && program[0] == 0x12 && program[1] == 0x60)
        {
            Display.EnableClassicHiresMode();
        }
    }

    private void ResetMemory()
    {
        Memory.Clear();
        Memory.Write(LowResFontBaseAddress, LowResFont);
        Memory.Write(HighResFontBaseAddress, HighResFont);
    }

    public void Start()
    {
        if (_running) throw new InvalidOperationException("Machine is already started.");
        _lastTimestamp = _clock.Timestamp;
        _clock.Ticked += OnTicked;
        _running = true;
    }

    public void Stop()
    {
        if (!_running) return;
        _clock.Ticked -= OnTicked;
        _running = false;
        if (_audio.IsPlaying) _audio.StopSound();
    }

    private void OnTicked(object? sender, EventArgs e)
    {
        var delta = CalculateDeltaTime();
        if (delta == 0) return;

        var maxDelta = _ticksPerFrame * 2;
        if (delta > maxDelta) delta = maxDelta;

        if (_isWaitingForKey && _input.WasAnyKeyPressedAndReleased(out var key))
        {
            Cpu.Registers.WriteV(_keyRegisterIndex, key);
            _isWaitingForKey = false;
        }

        _frameAcc += delta;

        if (_isWaitingForKey || _waitForVBlank)
        {
            _instructionAcc = 0;
        }
        else
        {
            _instructionAcc += delta;
            while (_instructionAcc >= _ticksPerInstruction)
            {
                Cpu.FetchDecodeExecute();
                _instructionAcc -= _ticksPerInstruction;
                if (_isWaitingForKey || _waitForVBlank)
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

        var st = Cpu.Registers.ReadSt();
        if (st > 0 && !_audio.IsPlaying) _audio.PlaySound();
        else if (st == 0 && _audio.IsPlaying) _audio.StopSound();
    }

    private long CalculateDeltaTime()
    {
        var now = _clock.Timestamp;
        var delta = now - _lastTimestamp;
        _lastTimestamp = now;
        return delta;
    }

    private void StepFrame()
    {
        var registers = Cpu.Registers;

        var dt = registers.ReadDt();
        if (dt > 0) registers.WriteDt((byte)(dt - 1));

        var st = registers.ReadSt();
        if (st > 0) registers.WriteSt((byte)(st - 1));

        Display.Render();
        _frameAcc -= _ticksPerFrame;
        _waitForVBlank = false;
    }

    private void OnSetPitch(SetPitchEvent evt) => _audio.Pitch = evt.Pitch;

    private void OnLoadAudioPattern(LoadAudioPatternEvent _)
    {
        _audio.WritePattern(span =>
        {
            var registers = Cpu.Registers;
            for (var i = 0; i < span.Length; i++)
            {
                span[i] = Memory.Read(registers.ReadIWithOffset(i));
            }
        });
    }

    private void OnKeyIsPressedSkip(KeyIsPressedSkipEvent evt)
    {
        if (_input.IsKeyPressed(evt.Key)) Cpu.AdvanceProgramCounter();
    }

    private void OnKeyIsReleasedSkip(KeyIsReleasedSkipEvent evt)
    {
        if (!_input.IsKeyPressed(evt.Key)) Cpu.AdvanceProgramCounter();
    }

    private void OnBeginWaitForKey(BeginWaitForKeyEvent evt)
    {
        _isWaitingForKey = true;
        _keyRegisterIndex = evt.RegisterIndex;
    }

    private void OnBeginWaitForVBlank(BeginWaitForVBlankEvent _)
    {
        _waitForVBlank = true;
    }
}
