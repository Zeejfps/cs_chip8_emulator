export class Audio {
  private ctx: AudioContext | null = null;
  private oscillator: OscillatorNode | null = null;
  private toneGain: GainNode | null = null;
  private masterGain: GainNode | null = null;
  private beepRequestedThisFrame = false;
  private volume = 0.5;
  private muted = false;

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
    this.toneGain.gain.value = 0;
    this.toneGain.connect(this.masterGain);
    this.oscillator = this.ctx.createOscillator();
    this.oscillator.type = 'square';
    this.oscillator.frequency.value = 440;
    this.oscillator.connect(this.toneGain);
    this.oscillator.start();
  }

  beepTick(): void {
    this.beepRequestedThisFrame = true;
  }

  reconcile(): void {
    if (!this.toneGain || !this.ctx) return;
    const target = this.beepRequestedThisFrame ? 0.15 : 0;
    this.toneGain.gain.setTargetAtTime(target, this.ctx.currentTime, 0.005);
    this.beepRequestedThisFrame = false;
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
