import type { InteropExports } from './interop.js';

const KEY_MAP: Record<string, number> = {
  Digit1: 0x1, Digit2: 0x2, Digit3: 0x3, Digit4: 0xC,
  KeyQ: 0x4, KeyW: 0x5, KeyE: 0x6, KeyR: 0xD,
  KeyA: 0x7, KeyS: 0x8, KeyD: 0x9, KeyF: 0xE,
  KeyZ: 0xA, KeyX: 0x0, KeyC: 0xB, KeyV: 0xF,
};

export function installKeyListeners(api: InteropExports): void {
  const tracked = new Set<string>();

  window.addEventListener('keydown', (e) => {
    const hex = KEY_MAP[e.code];
    if (hex === undefined) return;
    e.preventDefault();
    if (!tracked.has(e.code)) {
      tracked.add(e.code);
      api.SetKey(hex, true);
    }
  });

  window.addEventListener('keyup', (e) => {
    const hex = KEY_MAP[e.code];
    if (hex === undefined) return;
    e.preventDefault();
    tracked.delete(e.code);
    api.SetKey(hex, false);
  });
}
