import type { InteropExports } from './interop.js';
import type { Audio } from './audio.js';
import { settings } from './stores/settings.svelte.js';
import { emulator } from './stores/emulator.svelte.js';
import { writeQuirksToApi } from './quirks.js';

export function runRom(
  api: InteropExports,
  audio: Audio,
  bytes: Uint8Array,
  name: string,
  romId: string | null,
): void {
  api.Stop();
  api.Init();
  api.LoadProgram(bytes);
  writeQuirksToApi(api, settings.quirks);
  api.SetInstructionsPerSecond(settings.ips);
  audio.ensureStarted();
  api.Start();
  emulator.running = true;
  emulator.paused = false;
  emulator.pc = api.GetProgramCounter();
  emulator.lastRomName = name;
  emulator.lastRomBytes = bytes;
  emulator.prevInsLine = null;
  settings.lastRomId = romId;
  emulator.status = `Running ${name}`;
}

export function resetEmulator(api: InteropExports): void {
  api.Stop();
  api.Init();
  writeQuirksToApi(api, settings.quirks);
  api.SetInstructionsPerSecond(settings.ips);
  if (emulator.lastRomBytes) api.LoadProgram(emulator.lastRomBytes);
  emulator.running = false;
  emulator.paused = true;
  emulator.pc = api.GetProgramCounter();
  emulator.prevInsLine = null;
  emulator.status = emulator.lastRomName
    ? `Reset. Press Start to run ${emulator.lastRomName}.`
    : 'Reset. Load a ROM to begin.';
}

let fullscreenFn: (() => void) | null = null;

export function registerFullscreen(fn: () => void): () => void {
  fullscreenFn = fn;
  return () => {
    if (fullscreenFn === fn) fullscreenFn = null;
  };
}

export function toggleFullscreen(): void {
  fullscreenFn?.();
}
