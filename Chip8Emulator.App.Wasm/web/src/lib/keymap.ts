export const KEYBOARD_TO_HEX: Record<string, number> = {
  Digit1: 0x1, Digit2: 0x2, Digit3: 0x3, Digit4: 0xC,
  KeyQ: 0x4,  KeyW: 0x5,  KeyE: 0x6,  KeyR: 0xD,
  KeyA: 0x7,  KeyS: 0x8,  KeyD: 0x9,  KeyF: 0xE,
  KeyZ: 0xA,  KeyX: 0x0,  KeyC: 0xB,  KeyV: 0xF,
};

// Touch keypad layout (rows × cols) mapping to CHIP-8 hex values.
// Classic CHIP-8 keypad layout:
//   1 2 3 C
//   4 5 6 D
//   7 8 9 E
//   A 0 B F
export const KEYPAD_ROWS: number[][] = [
  [0x1, 0x2, 0x3, 0xC],
  [0x4, 0x5, 0x6, 0xD],
  [0x7, 0x8, 0x9, 0xE],
  [0xA, 0x0, 0xB, 0xF],
];

export function hexLabel(hex: number): string {
  return hex.toString(16).toUpperCase();
}
