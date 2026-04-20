# CHIP-8 Emulator

A CHIP-8 emulator written in C# / .NET 10, with three front-ends sharing a common core.

## Projects

- **`Chip8Emulator.Core`** — the interpreter (memory, registers, fetch/decode/execute, 60Hz timing).
- **`Chip8Emulator.Core.Tests`** — xUnit tests for the core.
- **`Chip8Emulator.App`** — shared host helpers (`StopwatchClock`).
- **`Chip8Emulator.App.Cli`** — terminal front-end, renders to the console with ANSI half-block characters.
- **`Chip8Emulator.App.Wasm`** — browser front-end, runs in WebAssembly and draws to a `<canvas>`.
- **`Chip8Emulator.App.OpenGL`** — desktop front-end (WIP).

## Running

### CLI

```sh
dotnet run --project Chip8Emulator.App.Cli -- path/to/rom.ch8
```

### Browser (WASM)

Requires the `wasm-tools` workload:

```sh
dotnet workload install wasm-tools
dotnet run --project Chip8Emulator.App.Wasm
```

Open the printed URL in a Chromium-based browser, then load a ROM.

## ROMs

A large collection of public-domain CHIP-8 ROMs is available at [kripod/chip8-roms](https://github.com/kripod/chip8-roms/tree/master).

## Keyboard

```
CHIP-8 keypad    Your keyboard
1 2 3 C          1 2 3 4
4 5 6 D          Q W E R
7 8 9 E          A S D F
A 0 B F          Z X C V
```

## Tests

```sh
dotnet test
```
