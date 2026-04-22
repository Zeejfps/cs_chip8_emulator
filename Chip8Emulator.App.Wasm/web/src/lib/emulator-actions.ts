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
  emulator.paused = settings.debugOpen;
  emulator.pc = api.GetProgramCounter();
  emulator.lastRomName = name;
  emulator.lastRomBytes = bytes;
  emulator.prevInsLine = null;
  settings.lastRomId = romId;
  emulator.status = settings.debugOpen ? 'Paused (debug)' : `Running ${name}`;
}

export function resetEmulator(api: InteropExports): void {
  api.Stop();
  api.Init();
  writeQuirksToApi(api, settings.quirks);
  api.SetInstructionsPerSecond(settings.ips);
  if (emulator.lastRomBytes) api.LoadProgram(emulator.lastRomBytes);
  api.Start();
  emulator.running = emulator.lastRomBytes !== null;
  emulator.paused = settings.debugOpen;
  emulator.pc = api.GetProgramCounter();
  emulator.prevInsLine = null;
  emulator.status = emulator.lastRomName
    ? `Running ${emulator.lastRomName}`
    : 'Reset. Load a ROM to begin.';
}

export function stepEmulator(api: InteropExports): void {
  if (!emulator.running) return;
  api.Step();
  emulator.pc = api.GetProgramCounter();
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
