export class Audio {
  private ctx: AudioContext | null = null;
  private oscillator: OscillatorNode | null = null;
  private toneGain: GainNode | null = null;
  private masterGain: GainNode | null = null;
  private patternSource: AudioBufferSourceNode | null = null;
  private patternBuffer: AudioBuffer | null = null;
  private patternBits: Uint8Array = new Uint8Array(16);
  private patternFrequencyHz = 4000;
  private hasPattern = false;
  private volume = 0.5;
  private muted = false;
  private playing = false;

  ensureStarted(): void {
    if (this.ctx) return;
    const AudioCtor =
      window.AudioContext ??
      (window as unknown as { webkitAudioContext: typeof AudioContext }).webkitAudioContext;
    this.ctx = new AudioCtor();
    this.masterGain = this.ctx.createGain();
    this.masterGain.gain.value = this.muted ? 0 : this.volume;
    this.masterGain.connect(this.ctx.destination);
    this.toneGain = this.ctx.createGain();
    this.toneGain.gain.value = this.playing ? 0.15 : 0;
    this.toneGain.connect(this.masterGain);
    this.oscillator = this.ctx.createOscillator();
    this.oscillator.type = 'square';
    this.oscillator.frequency.value = 440;
    this.oscillator.connect(this.toneGain);
    this.oscillator.start();
    if (this.hasPattern) {
      this.startPatternSource();
    }
  }

  playSound(): void {
    this.playing = true;
    if (!this.toneGain || !this.ctx) return;
    if (this.hasPattern) {
      this.startPatternSource();
      this.toneGain.gain.setTargetAtTime(0, this.ctx.currentTime, 0.005);
    } else {
      this.toneGain.gain.setTargetAtTime(0.15, this.ctx.currentTime, 0.005);
    }
  }

  stopSound(): void {
    this.playing = false;
    if (!this.toneGain || !this.ctx) return;
    this.toneGain.gain.setTargetAtTime(0, this.ctx.currentTime, 0.005);
    this.stopPatternSource();
  }

  setPattern(pattern: Uint8Array, frequencyHz: number): void {
    this.patternBits = new Uint8Array(16);
    for (let i = 0; i < 16 && i < pattern.length; i++) this.patternBits[i] = pattern[i];
    this.patternFrequencyHz = Math.max(1, frequencyHz);

    let anyBitSet = false;
    for (let i = 0; i < 16; i++) {
      if (this.patternBits[i] !== 0) {
        anyBitSet = true;
        break;
      }
    }
    this.hasPattern = anyBitSet;

    if (!this.ctx) return;
    this.rebuildPatternBuffer();
    if (this.playing && this.hasPattern) {
      this.startPatternSource();
    } else if (!this.hasPattern) {
      this.stopPatternSource();
    }
  }

  private rebuildPatternBuffer(): void {
    if (!this.ctx) return;
    // 128 samples (16 bytes * 8 bits), each bit becomes one sample at ±1.
    const sampleRate = Math.max(3000, Math.min(this.patternFrequencyHz, this.ctx.sampleRate));
    const buffer = this.ctx.createBuffer(1, 128, sampleRate);
    const data = buffer.getChannelData(0);
    for (let byteIdx = 0; byteIdx < 16; byteIdx++) {
      const b = this.patternBits[byteIdx];
      for (let bit = 0; bit < 8; bit++) {
        const on = ((b >> (7 - bit)) & 1) === 1;
        data[byteIdx * 8 + bit] = on ? 0.5 : -0.5;
      }
    }
    this.patternBuffer = buffer;
  }

  private startPatternSource(): void {
    if (!this.ctx || !this.toneGain) return;
    if (!this.patternBuffer) this.rebuildPatternBuffer();
    if (!this.patternBuffer) return;
    this.stopPatternSource();
    const src = this.ctx.createBufferSource();
    src.buffer = this.patternBuffer;
    src.loop = true;
    src.connect(this.toneGain);
    this.toneGain.gain.setTargetAtTime(0.15, this.ctx.currentTime, 0.005);
    src.start();
    this.patternSource = src;
  }

  private stopPatternSource(): void {
    if (!this.patternSource) return;
    try {
      this.patternSource.stop();
    } catch {
      /* already stopped */
    }
    try {
      this.patternSource.disconnect();
    } catch {
      /* ignore */
    }
    this.patternSource = null;
  }

  setVolume(v: number): void {
    this.volume = Math.max(0, Math.min(1, v));
    if (this.masterGain && this.ctx) {
      this.masterGain.gain.setTargetAtTime(
        this.muted ? 0 : this.volume,
        this.ctx.currentTime,
        0.01,
      );
    }
  }

  setMuted(m: boolean): void {
    this.muted = m;
    if (this.masterGain && this.ctx) {
      this.masterGain.gain.setTargetAtTime(
        this.muted ? 0 : this.volume,
        this.ctx.currentTime,
        0.01,
      );
    }
  }
}
