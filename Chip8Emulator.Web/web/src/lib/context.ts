import { getContext, setContext } from 'svelte';
import type { InteropExports } from './interop.js';
import type { DotnetRuntime } from './types/dotnet.js';
import type { Audio } from './audio.js';

export interface EmuContext {
  api: InteropExports;
  runtime: DotnetRuntime;
  audio: Audio;
}

const EMU_KEY = Symbol('emu');

export function setEmuContext(ctx: EmuContext): void {
  setContext(EMU_KEY, ctx);
}

export function getEmuContext(): EmuContext {
  const ctx = getContext<EmuContext | undefined>(EMU_KEY);
  if (!ctx)
    throw new Error(
      'EmuContext not provided — ensure App.svelte mounted with api/runtime/audio props',
    );
  return ctx;
}
