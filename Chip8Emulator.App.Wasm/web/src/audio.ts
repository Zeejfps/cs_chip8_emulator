export class Audio {
  private ctx: AudioContext | null = null;
  private oscillator: OscillatorNode | null = null;
  private gain: GainNode | null = null;
  private beepRequestedThisFrame = false;

  ensureStarted(): void {
    if (this.ctx) return;
    const AudioCtor = window.AudioContext ?? (window as unknown as { webkitAudioContext: typeof AudioContext }).webkitAudioContext;
    this.ctx = new AudioCtor();
    this.gain = this.ctx.createGain();
    this.gain.gain.value = 0;
    this.gain.connect(this.ctx.destination);
    this.oscillator = this.ctx.createOscillator();
    this.oscillator.type = 'square';
    this.oscillator.frequency.value = 440;
    this.oscillator.connect(this.gain);
    this.oscillator.start();
  }

  beepTick(): void {
    this.beepRequestedThisFrame = true;
  }

  reconcile(): void {
    if (!this.gain || !this.ctx) return;
    const target = this.beepRequestedThisFrame ? 0.15 : 0;
    this.gain.gain.setTargetAtTime(target, this.ctx.currentTime, 0.005);
    this.beepRequestedThisFrame = false;
  }
}
