import type { PresetName } from '../quirks.js';
import type { QuirkFlags } from '../quirks.js';

export type Phosphor = 'green' | 'amber';
export type SpeedPreset = '0.5x' | '1x' | '2x' | 'max';

export interface Settings {
  ips: number;
  speedPreset: SpeedPreset;
  volume: number;
  muted: boolean;
  phosphor: Phosphor;
  scanlines: boolean;
  quirks: QuirkFlags;
  quirksPreset: PresetName | 'custom';
  touchKeypadManual: boolean | null;
  debugOpen: boolean;
  lastRomId: string | null;
}

const STORAGE_KEY = 'chip8-settings/v1';

export const IPS_MIN = 200;
export const IPS_MAX = 5000;
export const SPEED_PRESET_IPS: Record<SpeedPreset, number> = {
  '0.5x': 500,
  '1x': 1000,
  '2x': 2000,
  'max': IPS_MAX,
};

const defaults: Settings = {
  ips: SPEED_PRESET_IPS['1x'],
  speedPreset: '1x',
  volume: 0.5,
  muted: false,
  phosphor: 'green',
  scanlines: true,
  quirks: { shiftVy: false, jumpVx: true, lsIncI: false, logicVf: false, wrap: false, dispWait: false, vfResultLast: false },
  quirksPreset: 'chip48',
  touchKeypadManual: null,
  debugOpen: false,
  lastRomId: null,
};

function load(): Settings {
  if (typeof localStorage === 'undefined') return structuredClone(defaults);
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return structuredClone(defaults);
    const parsed = JSON.parse(raw) as Partial<Settings>;
    return {
      ...defaults,
      ...parsed,
      quirks: { ...defaults.quirks, ...(parsed.quirks ?? {}) },
    };
  } catch {
    return structuredClone(defaults);
  }
}

export const settings = $state<Settings>(load());

export function persistSettings(): () => void {
  return $effect.root(() => {
    $effect(() => {
      try {
        localStorage.setItem(STORAGE_KEY, JSON.stringify(settings));
      } catch {
        // Private mode / storage full — settings become session-only, no-op.
      }
    });
  });
}
