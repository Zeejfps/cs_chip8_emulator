export class Audio {
  private ctx: AudioContext | null = null;
  private oscillator: OscillatorNode | null = null;
  private toneGain: GainNode | null = null;
  private masterGain: GainNode | null = null;
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
  }

  playSound(): void {
    this.playing = true;
    if (!this.toneGain || !this.ctx) return;
    this.toneGain.gain.setTargetAtTime(0.15, this.ctx.currentTime, 0.005);
  }

  stopSound(): void {
    this.playing = false;
    if (!this.toneGain || !this.ctx) return;
    this.toneGain.gain.setTargetAtTime(0, this.ctx.currentTime, 0.005);
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
