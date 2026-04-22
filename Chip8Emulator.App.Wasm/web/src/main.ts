import { mount } from 'svelte';
import type { DotnetBuilder } from './lib/types/dotnet.js';
import type { InteropExports } from './lib/interop.js';
import { Audio } from './lib/audio.js';
import App from './components/App.svelte';
import './app.css';

const boot = document.getElementById('boot');
if (boot) boot.textContent = 'Loading runtime…';

const { dotnet } = (await import(
  /* @vite-ignore */ new URL('_framework/dotnet.js', document.baseURI).href
)) as { dotnet: DotnetBuilder };

const runtime = await dotnet.withApplicationArgumentsFromQuery().create();
const audio = new Audio();

runtime.setModuleImports('main.js', {
  audio: {
    beepTick: () => audio.beepTick(),
  },
});

const config = runtime.getConfig();
const exports = await runtime.getAssemblyExports(config.mainAssemblyName);
const api = exports.Interop as InteropExports;
api.Init();

boot?.remove();

const target = document.getElementById('app');
if (!target) throw new Error('Missing #app mount target');
mount(App, { target, props: { api, runtime, audio } });
