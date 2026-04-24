using Chip8Emulator.Cli;
using Chip8Emulator.Core;

try
{
    if (args.Length < 1)
    {
        Console.Error.WriteLine("Usage: Chip8Emulator.Cli <rom-path>");
        return 1;
    }

    var romPath = args[0];

    if (!File.Exists(romPath))
    {
        Console.Error.WriteLine($"ROM file not found: {romPath}");
        Console.Error.WriteLine("Usage: Chip8Emulator.Cli <rom-path>");
        return 1;
    }

    var cancelled = false;
    Console.CancelKeyPress += (_, e) =>
    {
        e.Cancel = true;
        cancelled = true;
    };

    using var input = new ConsoleInput();
    var audio = new ConsoleBeepAudio();

    var clock = new StopwatchClock();
    var pixelBuffer = new byte[Chip8Display.HighResWidth * Chip8Display.HighResHeight];
    var emulatedDisplay = new Chip8Display(size => pixelBuffer.AsMemory(0, size));
    using var consoleDisplay = new AnsiConsoleDisplay(emulatedDisplay, pixelBuffer);
    var machine = Chip8.Builder()
        .WithDisplay(consoleDisplay)
        .WithAudio(audio)
        .WithClock(clock)
        .WithInput(input)
        .WithStack(new Chip8Stack(size => new int[size]))
        .WithMemory(new Chip8Memory(size => new byte[size]))
        .WithRegisters(new Chip8Registers(size => new byte[size]))
        .WithPersistentFlags(new FilePersistentFlags())
        .Build();

    Console.WriteLine($"Loading ROM: {romPath}");
    var romData = File.ReadAllBytes(romPath);

    machine.LoadProgram(romData);
    machine.Start();

    while (!cancelled && !input.IsCancelRequested)
    {
        clock.Tick();

        if (input.ConsumeRestartRequest())
        {
            machine.LoadProgram(romData);
        }
    }

    machine.Stop();
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.GetType().Name}: {ex.Message}");
    Console.Error.WriteLine(ex.StackTrace);
    return 1;
}
