import { mount } from 'svelte';
import type { DotnetBuilder } from './lib/types/dotnet.js';
import type { InteropExports } from './lib/interop.js';
import { Audio } from './lib/audio.js';
import App from './components/App.svelte';
import './app.css';

const boot = document.getElementById('boot');

declare const __BUILD_ID__: string;

const { dotnet } = (await import(
  /* @vite-ignore */ new URL(`_framework/dotnet.js?v=${__BUILD_ID__}`, document.baseURI).href
)) as { dotnet: DotnetBuilder };

const runtime = await dotnet.withApplicationArgumentsFromQuery().create();
const audio = new Audio();

runtime.setModuleImports('main.js', {
  audio: {
    playSound: () => audio.playSound(),
    stopSound: () => audio.stopSound(),
    setPattern: (pattern: Uint8Array, frequencyHz: number) =>
      audio.setPattern(pattern, frequencyHz),
  },
  persistentFlags: {
    read: () => {
      try {
        return localStorage.getItem('chip8.flags') ?? '';
      } catch {
        return '';
      }
    },
    write: (base64: string) => {
      try {
        localStorage.setItem('chip8.flags', base64);
      } catch {
        // localStorage unavailable or full — swallow silently.
      }
    },
  },
});

const config = runtime.getConfig();
const exports = await runtime.getAssemblyExports(config.mainAssemblyName);
const api = exports.Chip8Emulator.Web.Interop as InteropExports;
api.Init();

if (boot) {
  const reduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
  if (reduced) {
    boot.remove();
  } else {
    boot.addEventListener('transitionend', () => boot.remove(), { once: true });
    boot.classList.add('fade-out');
  }
}

const target = document.getElementById('app');
if (!target) throw new Error('Missing #app mount target');
mount(App, { target, props: { api, runtime, audio } });
