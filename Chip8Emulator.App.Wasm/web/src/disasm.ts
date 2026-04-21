import type { InteropExports } from './interop.js';

const MEMORY_SIZE = 4096;

export class Disassembler {
  private prevInsLine: string | null = null;
  private lastRenderedPc = -1;

  constructor(private readonly api: InteropExports, private readonly el: HTMLElement) {}

  readInsWord(addr: number): number | null {
    if (addr < 0 || addr + 1 >= MEMORY_SIZE) return null;
    const hi = this.api.GetMemoryByte(addr);
    const lo = this.api.GetMemoryByte(addr + 1);
    return (hi << 8) | lo;
  }

  formatInsLine(marker: string, addr: number, word: number): string {
    const a = addr.toString(16).toUpperCase().padStart(4, '0');
    const w = word.toString(16).toUpperCase().padStart(4, '0');
    const mnem = this.api.DisassembleInstruction(word);
    return `${marker} 0x${a}  ${w}  ${mnem}`;
  }

  render(): void {
    const pc = this.api.GetProgramCounter();
    const lines: string[] = [];
    lines.push(this.prevInsLine ?? '                              ');
    for (let i = 0; i < 5; i++) {
      const addr = pc + i * 2;
      const word = this.readInsWord(addr);
      if (word === null) break;
      lines.push(this.formatInsLine(i === 0 ? '>' : ' ', addr, word));
    }
    this.el.textContent = lines.join('\n');
    this.lastRenderedPc = pc;
  }

  setPrevInsLine(line: string | null): void {
    this.prevInsLine = line;
  }

  getLastRenderedPc(): number {
    return this.lastRenderedPc;
  }
}
