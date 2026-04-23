namespace Chip8Emulator.Core;

internal readonly record struct SetPitchEvent(byte Pitch);

internal readonly record struct LoadAudioPatternEvent;

internal readonly record struct KeyIsPressedSkipEvent(byte Key);

internal readonly record struct KeyIsReleasedSkipEvent(byte Key);

internal readonly record struct BeginWaitForKeyEvent(int RegisterIndex);

internal readonly record struct BeginWaitForVBlankEvent;
