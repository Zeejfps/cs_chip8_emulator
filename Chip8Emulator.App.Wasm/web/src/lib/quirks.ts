import type { InteropExports } from './interop.js';

export interface QuirkFlags {
  shiftVy: boolean;
  jumpVx: boolean;
  lsIncI: boolean;
  logicVf: boolean;
  wrap: boolean;
  dispWait: boolean;
  vfResultLast: boolean;
}

export type PresetName = 'cosmac' | 'chip48' | 'schip' | 'xochip';
export type PresetOrCustom = PresetName | 'custom';

export const QUIRK_PRESETS: Record<PresetName, QuirkFlags> = {
  cosmac: {
    shiftVy: true,
    jumpVx: false,
    lsIncI: true,
    logicVf: true,
    wrap: false,
    dispWait: true,
    vfResultLast: false,
  },
  chip48: {
    shiftVy: false,
    jumpVx: true,
    lsIncI: false,
    logicVf: false,
    wrap: false,
    dispWait: false,
    vfResultLast: false,
  },
  schip: {
    shiftVy: false,
    jumpVx: true,
    lsIncI: false,
    logicVf: false,
    wrap: false,
    dispWait: false,
    vfResultLast: false,
  },
  xochip: {
    shiftVy: false,
    jumpVx: false,
    lsIncI: true,
    logicVf: false,
    wrap: true,
    dispWait: false,
    vfResultLast: false,
  },
};

export const PRESET_LABELS: Record<PresetName, string> = {
  cosmac: 'COSMAC VIP',
  chip48: 'CHIP-48',
  schip: 'SUPER-CHIP 1.1',
  xochip: 'XO-CHIP',
};

function flagsEqual(a: QuirkFlags, b: QuirkFlags): boolean {
  return (
    a.shiftVy === b.shiftVy &&
    a.jumpVx === b.jumpVx &&
    a.lsIncI === b.lsIncI &&
    a.logicVf === b.logicVf &&
    a.wrap === b.wrap &&
    a.dispWait === b.dispWait &&
    a.vfResultLast === b.vfResultLast
  );
}

export function matchingPreset(q: QuirkFlags): PresetOrCustom {
  for (const name of Object.keys(QUIRK_PRESETS) as PresetName[]) {
    if (flagsEqual(QUIRK_PRESETS[name], q)) return name;
  }
  return 'custom';
}

// Reconcile a current selection with the live flags: keep the user's explicit
// choice if its flags still match (disambiguates presets with identical tables,
// e.g. chip48 vs schip). Otherwise fall back to the first matching preset, or
// 'custom' when nothing matches.
export function reconcilePreset(current: PresetOrCustom, q: QuirkFlags): PresetOrCustom {
  if (current !== 'custom' && flagsEqual(QUIRK_PRESETS[current], q)) return current;
  return matchingPreset(q);
}

export function readQuirksFromApi(api: InteropExports): QuirkFlags {
  return {
    shiftVy: api.GetShiftUsesVy(),
    jumpVx: api.GetJumpUsesVx(),
    lsIncI: api.GetLoadStoreIncrementsI(),
    logicVf: api.GetLogicResetsVf(),
    wrap: api.GetSpritesWrap(),
    dispWait: api.GetDisplayWait(),
    vfResultLast: api.GetVfResultWrittenLast(),
  };
}

export function writeQuirksToApi(api: InteropExports, q: QuirkFlags): void {
  api.SetShiftUsesVy(q.shiftVy);
  api.SetJumpUsesVx(q.jumpVx);
  api.SetLoadStoreIncrementsI(q.lsIncI);
  api.SetLogicResetsVf(q.logicVf);
  api.SetSpritesWrap(q.wrap);
  api.SetDisplayWait(q.dispWait);
  api.SetVfResultWrittenLast(q.vfResultLast);
}
