export interface EmulatorState {
  running: boolean;
  paused: boolean;
  status: string;
  pc: number;
  prevInsLine: string | null;
  lastRomName: string | null;
  lastRomBytes: Uint8Array | null;
  canvasEl: HTMLCanvasElement | null;
}

export const emulator = $state<EmulatorState>({
  running: false,
  paused: true,
  status: 'Load a ROM to begin.',
  pc: 0,
  prevInsLine: null,
  lastRomName: null,
  lastRomBytes: null,
  canvasEl: null,
});
