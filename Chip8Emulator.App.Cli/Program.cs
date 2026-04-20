using Chip8Emulator.App;
using Chip8Emulator.App.Cli;
using Chip8Emulator.Core;

try
{
    if (args.Length < 1)
    {
        Console.Error.WriteLine("Usage: Chip8Emulator.App.Cli <rom-path>");
        return 1;
    }

    var romPath = args[0];

    if (!File.Exists(romPath))
    {
        Console.Error.WriteLine($"ROM file not found: {romPath}");
        Console.Error.WriteLine("Usage: Chip8Emulator.App.Cli <rom-path>");
        return 1;
    }

    var cancelled = false;
    Console.CancelKeyPress += (_, e) =>
    {
        e.Cancel = true;
        cancelled = true;
    };

    using var display = new AnsiConsoleRenderer();
    using var input = new ConsoleInput();
    var audio = new ConsoleBeepAudio();

    var clock = new StopwatchClock();
    var machine = Chip8.Builder()
        .WithRenderer(display)
        .WithAudio(audio)
        .WithClock(clock)
        .WithInput(input)
        .Build();

    display.Attach(machine);

    Console.WriteLine($"Loading ROM: {romPath}");
    var romData = File.ReadAllBytes(romPath);

    machine.LoadProgram(romData);

    clock.Start();
    while (!cancelled && !input.IsCancelRequested)
    {
        machine.Update();

        if (input.ConsumeRestartRequest())
        {
            machine.LoadProgram(romData);
        }
    }

    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.GetType().Name}: {ex.Message}");
    Console.Error.WriteLine(ex.StackTrace);
    return 1;
}
