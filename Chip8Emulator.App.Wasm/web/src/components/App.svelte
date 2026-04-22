<script lang="ts">
  import { onMount } from 'svelte';
  import type { InteropExports } from '$lib/interop.js';
  import type { DotnetRuntime } from '$lib/types/dotnet.js';
  import type { Audio } from '$lib/audio.js';
  import { setEmuContext } from '$lib/context.js';
  import { settings, persistSettings } from '$lib/stores/settings.svelte.js';
  import { emulator } from '$lib/stores/emulator.svelte.js';
  import { viewport, initViewport } from '$lib/stores/viewport.svelte.js';
  import { KEYBOARD_TO_HEX } from '$lib/keymap.js';
  import { writeQuirksToApi } from '$lib/quirks.js';
  import Canvas from './Canvas.svelte';
  import Toolbar from './Toolbar.svelte';
  import StatusBar from './StatusBar.svelte';
  import SettingsSheet from './SettingsSheet.svelte';
  import DebugPanel from './DebugPanel.svelte';
  import TouchKeypad from './TouchKeypad.svelte';

  interface Props {
    api: InteropExports;
    runtime: DotnetRuntime;
    audio: Audio;
  }

  const { api, runtime, audio }: Props = $props();

  /* svelte-ignore state_referenced_locally */
  setEmuContext({ api, runtime, audio });

  let settingsOpen = $state(false);
  let romPickerOpen = $state(false);

  const touchKeypadVisible = $derived(
    settings.touchKeypadManual ?? viewport.width < 768,
  );

  onMount(() => {
    writeQuirksToApi(api, settings.quirks);
    api.SetInstructionsPerSecond(settings.ips);
    audio.setVolume(settings.volume);
    audio.setMuted(settings.muted);

    const stopPersist = persistSettings();
    const stopViewport = initViewport();

    const onKeyDown = (e: KeyboardEvent) => {
      const active = document.activeElement;
      if (active instanceof HTMLElement) {
        const tag = active.tagName;
        if (tag === 'INPUT' || tag === 'TEXTAREA' || tag === 'SELECT' || active.isContentEditable) {
          return;
        }
      }
      const hex = KEYBOARD_TO_HEX[e.code];
      if (hex === undefined) return;
      e.preventDefault();
      api.SetKey(hex, true);
    };

    const onKeyUp = (e: KeyboardEvent) => {
      const hex = KEYBOARD_TO_HEX[e.code];
      if (hex === undefined) return;
      e.preventDefault();
      api.SetKey(hex, false);
    };

    window.addEventListener('keydown', onKeyDown);
    window.addEventListener('keyup', onKeyUp);

    emulator.status = 'Ready. Load a ROM to begin.';

    return () => {
      window.removeEventListener('keydown', onKeyDown);
      window.removeEventListener('keyup', onKeyUp);
      stopPersist();
      stopViewport();
    };
  });

  $effect(() => {
    document.documentElement.classList.toggle('phosphor-amber', settings.phosphor === 'amber');
  });
</script>

<div class="flex min-h-svh flex-col">
  <header class="border-b border-border/50 px-3 py-2 sm:px-4">
    <div class="mx-auto flex w-full max-w-5xl items-center justify-between gap-3">
      <h1 class="font-pixel phosphor-text text-base tracking-[0.2em] sm:text-lg">CHIP-8</h1>
      <Toolbar bind:settingsOpen bind:romPickerOpen />
    </div>
  </header>

  <main class="mx-auto flex w-full max-w-5xl flex-1 flex-col gap-3 px-3 py-3 sm:px-4 sm:py-4">
    <Canvas onOpenRomPicker={() => { romPickerOpen = true; }} />
    <StatusBar />
    {#if touchKeypadVisible}
      <TouchKeypad />
    {/if}
    <DebugPanel />
  </main>

  <SettingsSheet bind:open={settingsOpen} />
</div>
