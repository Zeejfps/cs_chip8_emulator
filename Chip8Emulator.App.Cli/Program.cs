using Chip8Emulator.App;
using Chip8Emulator.App.Cli;
using Chip8Emulator.Core;


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

using var display = new AnsiConsoleDisplay();

var clock = new StopwatchClock();
var machine = Chip8.Builder()
    .WithDisplay(display)
    .WithAudio(new ConsoleBeepAudio())
    .WithClock(clock)
    .Build();

Console.WriteLine($"Loading ROM: {romPath}");
var romData = File.ReadAllBytes(romPath);

Console.WriteLine($"Rom size: {romData.Length}");
machine.LoadProgram(romData);

clock.Start();
while (!cancelled)
{
    machine.Update();
}

return 0;
