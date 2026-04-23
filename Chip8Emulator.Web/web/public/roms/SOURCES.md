# ROM Sources

Provenance for every `.ch8` file bundled with the WASM build. Manifest entries
in `manifest.json` carry author and license; this file records *where each
binary was sourced from* so future updates can be re-pulled from the same
upstream.

## chip8Archive (CC0-1.0)

The following ROMs come from the [Chip8 Community Archive](https://github.com/JohnEarnest/chip8Archive),
maintained by John Earnest. The entire archive is released under CC0-1.0
("No Rights Reserved"). Files were pulled from
`https://raw.githubusercontent.com/JohnEarnest/chip8Archive/master/roms/<name>.ch8`.

- `br8kout.ch8` — *BR8KOUT* by SharpenedSpoon
- `octojam1title.ch8` — *Octojam 1 Title* by John Earnest
- `snake.ch8` — *Snake* by TomR
- `rockto.ch8` — *Rockto* by SupSuper
- `flightrunner.ch8` — *Flight Runner* by TodPunk
- `glitchGhost.ch8` — *Glitch Ghost* by LillianV
- `danm8ku.ch8` — *danm8ku* by buffi
- `chipquarium.ch8` — *Chipquarium* by mattmik
- `slipperyslope.ch8` — *Slippery Slope* by John Earnest
- `RPS.ch8` — *RPS* by SystemLogoff
- `superpong.ch8` — *Super Pong* by offstatic
- `caveexplorer.ch8` — *Cave Explorer* by John Earnest
- `outlaw.ch8` — *Outlaw* by John Earnest
- `pumpkindressup.ch8` — *Pumpkin Dress Up* by SystemLogoff

`preferredIps` values for these ROMs are derived from each entry's `tickrate`
field in chip8Archive's `programs.json` (Octo runs at 60fps, so
`IPS = tickrate * 60`), rounded to the slider's 100-IPS step.

## Freeware classics (license: "Freeware")

These two are not CC0. They were released by their authors without an explicit
license and have been redistributed in every CHIP-8 emulator since the 1990s
under a community convention of "freely distributable freeware". They are
labeled `Freeware` in the manifest to distinguish them from the strictly CC0
chip8Archive entries above.

- `tetris.ch8` — *Tetris* by Fran Dachille (1991). Sourced via
  [kripod/chip8-roms](https://github.com/kripod/chip8-roms),
  file `games/Tetris [Fran Dachille, 1991].ch8`.
- `invaders.ch8` — *Space Invaders* by David Winter. Sourced via
  [kripod/chip8-roms](https://github.com/kripod/chip8-roms),
  file `games/Space Invaders [David Winter].ch8`. Original distribution:
  http://pong-story.com/chip8/

Both expect COSMAC-VIP-style behavior (`shiftVy`, `lsIncI`, `logicVf`,
`dispWait`), which is why their `preferredQuirks` matches the `cosmac` preset
rather than the chip8Archive default profile used above.
