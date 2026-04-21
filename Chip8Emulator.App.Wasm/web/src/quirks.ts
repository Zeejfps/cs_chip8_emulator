import type { InteropExports } from './interop.js';

interface QuirkFlags {
  shiftVy: boolean;
  jumpVx: boolean;
  lsIncI: boolean;
  logicVf: boolean;
}

type PresetName = 'cosmac' | 'chip48' | 'schip' | 'xochip';

const QUIRK_PRESETS: Record<PresetName, QuirkFlags> = {
  cosmac: { shiftVy: true,  jumpVx: false, lsIncI: true,  logicVf: true  },
  chip48: { shiftVy: false, jumpVx: true,  lsIncI: false, logicVf: false },
  schip:  { shiftVy: false, jumpVx: true,  lsIncI: false, logicVf: false },
  xochip: { shiftVy: false, jumpVx: false, lsIncI: true,  logicVf: false },
};

export function initQuirks(api: InteropExports): void {
  const presetRadios = document.querySelectorAll<HTMLInputElement>('input[name="quirk-preset"]');
  const shiftVy = document.getElementById('q-shift-vy') as HTMLInputElement;
  const jumpVx = document.getElementById('q-jump-vx') as HTMLInputElement;
  const lsIncI = document.getElementById('q-ls-inc-i') as HTMLInputElement;
  const logicVf = document.getElementById('q-logic-vf') as HTMLInputElement;

  function currentQuirks(): QuirkFlags {
    return {
      shiftVy: shiftVy.checked,
      jumpVx: jumpVx.checked,
      lsIncI: lsIncI.checked,
      logicVf: logicVf.checked,
    };
  }

  function matchingPresetName(q: QuirkFlags): PresetName | 'custom' {
    for (const name of Object.keys(QUIRK_PRESETS) as PresetName[]) {
      const p = QUIRK_PRESETS[name];
      if (p.shiftVy === q.shiftVy && p.jumpVx === q.jumpVx && p.lsIncI === q.lsIncI && p.logicVf === q.logicVf) {
        return name;
      }
    }
    return 'custom';
  }

  function selectPresetRadio(name: PresetName | 'custom'): void {
    for (const r of presetRadios) r.checked = (r.value === name);
  }

  function pushQuirksToCore(): void {
    const q = currentQuirks();
    api.SetShiftUsesVy(q.shiftVy);
    api.SetJumpUsesVx(q.jumpVx);
    api.SetLoadStoreIncrementsI(q.lsIncI);
    api.SetLogicResetsVf(q.logicVf);
  }

  function applyPreset(name: PresetName): void {
    const p = QUIRK_PRESETS[name];
    shiftVy.checked = p.shiftVy;
    jumpVx.checked = p.jumpVx;
    lsIncI.checked = p.lsIncI;
    logicVf.checked = p.logicVf;
    selectPresetRadio(name);
    pushQuirksToCore();
  }

  function onQuirkCheckboxChanged(): void {
    selectPresetRadio(matchingPresetName(currentQuirks()));
    pushQuirksToCore();
  }

  for (const r of presetRadios) {
    r.addEventListener('change', () => {
      if (r.checked && r.value !== 'custom') applyPreset(r.value as PresetName);
    });
  }
  for (const cb of [shiftVy, jumpVx, lsIncI, logicVf]) {
    cb.addEventListener('change', onQuirkCheckboxChanged);
  }

  shiftVy.checked = api.GetShiftUsesVy();
  jumpVx.checked = api.GetJumpUsesVx();
  lsIncI.checked = api.GetLoadStoreIncrementsI();
  logicVf.checked = api.GetLogicResetsVf();
  selectPresetRadio(matchingPresetName(currentQuirks()));
}
