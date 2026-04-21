export interface InteropExports {
  Init(): void;
  LoadProgram(rom: Uint8Array): void;
  Tick(): void;
  Start(): void;
  Stop(): void;
  Step(): void;

  GetProgramCounter(): number;
  GetMemoryByte(address: number): number;
  DisassembleInstruction(ins: number): string;

  GetPixelDataPtr(): number;
  GetPixelDataLength(): number;
  GetWidth(): number;
  GetHeight(): number;

  SetKey(key: number, pressed: boolean): void;

  GetInstructionsPerSecond(): number;
  SetInstructionsPerSecond(ips: number): void;

  GetShiftUsesVy(): boolean;
  SetShiftUsesVy(value: boolean): void;
  GetJumpUsesVx(): boolean;
  SetJumpUsesVx(value: boolean): void;
  GetLoadStoreIncrementsI(): boolean;
  SetLoadStoreIncrementsI(value: boolean): void;
  GetLogicResetsVf(): boolean;
  SetLogicResetsVf(value: boolean): void;
  GetSpritesWrap(): boolean;
  SetSpritesWrap(value: boolean): void;
  GetDisplayWait(): boolean;
  SetDisplayWait(value: boolean): void;
}
