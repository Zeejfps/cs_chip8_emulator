import { dotnet } from './_framework/dotnet.js';

const KEY_MAP = {
  Digit1: 0x1, Digit2: 0x2, Digit3: 0x3, Digit4: 0xC,
  KeyQ: 0x4, KeyW: 0x5, KeyE: 0x6, KeyR: 0xD,
  KeyA: 0x7, KeyS: 0x8, KeyD: 0x9, KeyF: 0xE,
  KeyZ: 0xA, KeyX: 0x0, KeyC: 0xB, KeyV: 0xF,
};

const audio = {
  ctx: null,
  oscillator: null,
  gain: null,
  beepRequestedThisFrame: false,

  ensureStarted() {
    if (this.ctx) return;
    this.ctx = new (window.AudioContext || window.webkitAudioContext)();
    this.gain = this.ctx.createGain();
    this.gain.gain.value = 0;
    this.gain.connect(this.ctx.destination);
    this.oscillator = this.ctx.createOscillator();
    this.oscillator.type = 'square';
    this.oscillator.frequency.value = 440;
    this.oscillator.connect(this.gain);
    this.oscillator.start();
  },

  beepTick() {
    this.beepRequestedThisFrame = true;
  },

  reconcile() {
    if (!this.gain) return;
    const target = this.beepRequestedThisFrame ? 0.15 : 0;
    this.gain.gain.setTargetAtTime(target, this.ctx.currentTime, 0.005);
    this.beepRequestedThisFrame = false;
  },
};

const status = document.getElementById('status');
const restartBtn = document.getElementById('restart');
const romInput = document.getElementById('rom');
const canvas = document.getElementById('screen');
const ctx2d = canvas.getContext('2d');

status.textContent = 'Loading runtime...';

const runtime = await dotnet
  .withApplicationArgumentsFromQuery()
  .create();
const { setModuleImports, getAssemblyExports, getConfig } = runtime;

setModuleImports('main.js', {
  audio: {
    beepTick: () => audio.beepTick(),
  },
});

const config = getConfig();
const exports = await getAssemblyExports(config.mainAssemblyName);
const api = exports.Interop;

api.Init();

const pixelPtr = api.GetPixelDataPtr();
let width = 0;
let height = 0;
let imageData = null;
let rgba = null;

function resize(w, h) {
  width = w;
  height = h;
  canvas.width = w;
  canvas.height = h;
  imageData = ctx2d.createImageData(w, h);
  rgba = imageData.data;
  for (let i = 0; i < w * h; i++) {
    rgba[i * 4 + 3] = 255;
  }
}

resize(api.GetWidth(), api.GetHeight());

let lastRomBytes = null;
let running = false;

romInput.addEventListener('change', async (e) => {
  const file = e.target.files?.[0];
  if (!file) return;
  audio.ensureStarted();
  const buf = await file.arrayBuffer();
  const bytes = new Uint8Array(buf);
  lastRomBytes = bytes;
  api.LoadProgram(bytes);
  restartBtn.disabled = false;
  status.textContent = `Loaded ${file.name} (${bytes.length} bytes)`;
  if (!running) {
    running = true;
    requestAnimationFrame(frame);
  }
});

restartBtn.addEventListener('click', () => {
  if (!lastRomBytes) return;
  audio.ensureStarted();
  api.LoadProgram(lastRomBytes);
});

const tracked = new Set();
window.addEventListener('keydown', (e) => {
  const hex = KEY_MAP[e.code];
  if (hex === undefined) return;
  e.preventDefault();
  if (!tracked.has(e.code)) {
    tracked.add(e.code);
    api.SetKey(hex, true);
  }
});
window.addEventListener('keyup', (e) => {
  const hex = KEY_MAP[e.code];
  if (hex === undefined) return;
  e.preventDefault();
  tracked.delete(e.code);
  api.SetKey(hex, false);
});

function frame() {
  api.Update();

  const curW = api.GetWidth();
  const curH = api.GetHeight();
  if (curW !== width || curH !== height) {
    resize(curW, curH);
  }

  const pixelCount = width * height;
  const view = runtime.localHeapViewU8().subarray(pixelPtr, pixelPtr + pixelCount);
  for (let i = 0; i < pixelCount; i++) {
    const on = view[i] !== 0;
    const j = i * 4;
    rgba[j] = on ? 0xE6 : 0x0D;
    rgba[j + 1] = on ? 0xED : 0x11;
    rgba[j + 2] = on ? 0xF3 : 0x17;
  }
  ctx2d.putImageData(imageData, 0, 0);

  audio.reconcile();
  requestAnimationFrame(frame);
}

status.textContent = 'Ready. Load a ROM to begin.';
