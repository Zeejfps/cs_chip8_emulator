export const KEYBOARD_TO_HEX: Record<string, number> = {
  Digit1: 0x1,
  Digit2: 0x2,
  Digit3: 0x3,
  Digit4: 0xc,
  KeyQ: 0x4,
  KeyW: 0x5,
  KeyE: 0x6,
  KeyR: 0xd,
  KeyA: 0x7,
  KeyS: 0x8,
  KeyD: 0x9,
  KeyF: 0xe,
  KeyZ: 0xa,
  KeyX: 0x0,
  KeyC: 0xb,
  KeyV: 0xf,
};

// Touch keypad layout (rows × cols) mapping to CHIP-8 hex values.
// Classic CHIP-8 keypad layout:
//   1 2 3 C
//   4 5 6 D
//   7 8 9 E
//   A 0 B F
export const KEYPAD_ROWS: number[][] = [
  [0x1, 0x2, 0x3, 0xc],
  [0x4, 0x5, 0x6, 0xd],
  [0x7, 0x8, 0x9, 0xe],
  [0xa, 0x0, 0xb, 0xf],
];

export function hexLabel(hex: number): string {
  return hex.toString(16).toUpperCase();
}

const HEX_TO_KEYBOARD: Record<number, string> = Object.fromEntries(
  Object.entries(KEYBOARD_TO_HEX).map(([code, hex]) => [
    hex,
    code.startsWith('Digit') ? code.slice(5) : code.slice(3),
  ]),
);

export function keyboardLabel(hex: number): string {
  return HEX_TO_KEYBOARD[hex] ?? hexLabel(hex);
}
