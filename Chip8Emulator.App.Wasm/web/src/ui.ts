import type { InteropExports } from './interop.js';
import type { DotnetRuntime } from './_framework/dotnet.js';
import type { Audio } from './audio.js';
import type { Disassembler } from './disasm.js';

const IPS_MIN = 400;
const IPS_MAX = 1500;

export class AppUi {
  private readonly statusEl: HTMLElement;
  private readonly restartBtn: HTMLButtonElement;
  private readonly pauseBtn: HTMLButtonElement;
  private readonly stepBtn: HTMLButtonElement;
  private readonly romInput: HTMLInputElement;
  private readonly canvas: HTMLCanvasElement;
  private readonly ctx2d: CanvasRenderingContext2D;
  private readonly ipsRange: HTMLInputElement;
  private readonly ipsNumber: HTMLInputElement;

  private readonly pixelPtr: number;
  private width = 0;
  private height = 0;
  private imageData: ImageData | null = null;
  private rgba: Uint8ClampedArray | null = null;

  private lastRomBytes: Uint8Array | null = null;
  private running = false;
  private paused = false;

  constructor(
    private readonly api: InteropExports,
    private readonly runtime: DotnetRuntime,
    private readonly audio: Audio,
    private readonly disasm: Disassembler,
  ) {
    this.statusEl = this.requireElement('status');
    this.restartBtn = this.requireElement<HTMLButtonElement>('restart');
    this.pauseBtn = this.requireElement<HTMLButtonElement>('pause');
    this.stepBtn = this.requireElement<HTMLButtonElement>('step');
    this.romInput = this.requireElement<HTMLInputElement>('rom');
    this.canvas = this.requireElement<HTMLCanvasElement>('screen');
    this.ipsRange = this.requireElement<HTMLInputElement>('ips-range');
    this.ipsNumber = this.requireElement<HTMLInputElement>('ips-number');

    const ctx = this.canvas.getContext('2d');
    if (!ctx) throw new Error('Failed to acquire 2D canvas context');
    this.ctx2d = ctx;

    this.pixelPtr = api.GetPixelDataPtr();
    this.resize(api.GetWidth(), api.GetHeight());

    this.wireRomInput();
    this.wireRestart();
    this.wirePause();
    this.wireStep();
    this.wireIps();
  }

  setStatus(text: string): void {
    this.statusEl.textContent = text;
  }

  isPaused(): boolean {
    return this.paused;
  }

  renderPixels(): void {
    if (!this.rgba || !this.imageData) return;
    const pixelCount = this.width * this.height;
    const view = this.runtime.localHeapViewU8().subarray(this.pixelPtr, this.pixelPtr + pixelCount);
    const rgba = this.rgba;
    for (let i = 0; i < pixelCount; i++) {
      const on = view[i] !== 0;
      const j = i * 4;
      rgba[j] = on ? 0xE6 : 0x0D;
      rgba[j + 1] = on ? 0xED : 0x11;
      rgba[j + 2] = on ? 0xF3 : 0x17;
    }
    this.ctx2d.putImageData(this.imageData, 0, 0);
  }

  syncCanvasSize(): void {
    const curW = this.api.GetWidth();
    const curH = this.api.GetHeight();
    if (curW !== this.width || curH !== this.height) {
      this.resize(curW, curH);
    }
  }

  handleEmulatorError(e: unknown): void {
    if (!this.paused) {
      this.paused = true;
      this.api.Stop();
      this.pauseBtn.textContent = 'Play';
    }
    this.stepBtn.disabled = false;
    const pc = this.api.GetProgramCounter();
    const bad = pc - 2;
    const word = this.disasm.readInsWord(bad);
    this.disasm.setPrevInsLine(word !== null ? this.disasm.formatInsLine('!', bad, word) : null);
    this.disasm.render();
    this.renderPixels();
    this.setStatus(`Error: ${extractErrorMessage(e)}`);
  }

  private requireElement<T extends HTMLElement = HTMLElement>(id: string): T {
    const el = document.getElementById(id);
    if (!el) throw new Error(`Missing required element: #${id}`);
    return el as T;
  }

  private resize(w: number, h: number): void {
    this.width = w;
    this.height = h;
    this.canvas.width = w;
    this.canvas.height = h;
    this.imageData = this.ctx2d.createImageData(w, h);
    this.rgba = this.imageData.data;
    for (let i = 0; i < w * h; i++) {
      this.rgba[i * 4 + 3] = 255;
    }
  }

  private setPaused(next: boolean): void {
    if (this.paused === next) return;
    this.paused = next;
    if (this.paused) {
      this.api.Stop();
      this.pauseBtn.textContent = 'Play';
      this.stepBtn.disabled = false;
      this.disasm.render();
    } else {
      this.api.Start();
      this.pauseBtn.textContent = 'Pause';
      this.stepBtn.disabled = true;
    }
  }

  private wireRomInput(): void {
    this.romInput.addEventListener('change', async (e) => {
      const input = e.target as HTMLInputElement;
      const file = input.files?.[0];
      if (!file) return;
      this.audio.ensureStarted();
      const buf = await file.arrayBuffer();
      const bytes = new Uint8Array(buf);
      this.lastRomBytes = bytes;
      this.api.LoadProgram(bytes);
      this.disasm.setPrevInsLine(null);
      this.restartBtn.disabled = false;
      this.pauseBtn.disabled = false;
      if (this.paused) {
        this.stepBtn.disabled = false;
        this.disasm.render();
      }
      this.setStatus(`Loaded ${file.name} (${bytes.length} bytes)`);
      if (!this.running) {
        this.running = true;
        this.api.Start();
        this.startLoop();
      }
    });
  }

  private wireRestart(): void {
    this.restartBtn.addEventListener('click', () => {
      if (!this.lastRomBytes) return;
      this.audio.ensureStarted();
      this.api.LoadProgram(this.lastRomBytes);
      this.disasm.setPrevInsLine(null);
      if (this.paused) this.disasm.render();
    });
  }

  private wirePause(): void {
    this.pauseBtn.addEventListener('click', () => {
      this.setPaused(!this.paused);
    });
  }

  private wireStep(): void {
    this.stepBtn.addEventListener('click', () => {
      if (!this.paused) return;
      const prePc = this.api.GetProgramCounter();
      const preWord = this.disasm.readInsWord(prePc);
      try {
        this.api.Step();
      } catch (e) {
        this.handleEmulatorError(e);
        return;
      }
      if (preWord !== null) this.disasm.setPrevInsLine(this.disasm.formatInsLine('-', prePc, preWord));
      this.disasm.render();
      this.renderPixels();
    });
  }

  private wireIps(): void {
    this.ipsRange.min = String(IPS_MIN);
    this.ipsRange.max = String(IPS_MAX);
    this.ipsNumber.min = String(IPS_MIN);
    this.ipsNumber.max = String(IPS_MAX);

    const applyIps = (raw: number): void => {
      let ips = Number.isFinite(raw) ? Math.round(raw) : this.api.GetInstructionsPerSecond();
      if (ips < IPS_MIN) ips = IPS_MIN;
      if (ips > IPS_MAX) ips = IPS_MAX;
      this.ipsRange.value = String(ips);
      this.ipsNumber.value = String(ips);
      this.api.SetInstructionsPerSecond(ips);
    };
    this.ipsRange.addEventListener('input', () => applyIps(Number(this.ipsRange.value)));
    this.ipsNumber.addEventListener('change', () => applyIps(Number(this.ipsNumber.value)));
  }

  private startLoop(): void {
    const frame = (): void => {
      if (!this.paused) {
        const prePc = this.api.GetProgramCounter();
        const preWord = this.disasm.readInsWord(prePc);
        try {
          this.api.Tick();
        } catch (e) {
          this.handleEmulatorError(e);
          requestAnimationFrame(frame);
          return;
        }
        if (preWord !== null && this.api.GetProgramCounter() !== prePc) {
          this.disasm.setPrevInsLine(this.disasm.formatInsLine('-', prePc, preWord));
        }
      }

      this.syncCanvasSize();
      this.renderPixels();

      if (!this.paused) {
        const pc = this.api.GetProgramCounter();
        if (pc !== this.disasm.getLastRenderedPc()) this.disasm.render();
        this.audio.reconcile();
      }

      requestAnimationFrame(frame);
    };
    requestAnimationFrame(frame);
  }
}

function extractErrorMessage(e: unknown): string {
  if (!e) return 'Unknown error';
  if (typeof e === 'string') return e;
  const msg = (e as { message?: string }).message ?? String(e);
  return msg.replace(/\s+/g, ' ').trim();
}
