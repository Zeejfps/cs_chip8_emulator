<script lang="ts">
  import Play from 'phosphor-svelte/lib/Play';
  import Pause from 'phosphor-svelte/lib/Pause';
  import ArrowClockwise from 'phosphor-svelte/lib/ArrowClockwise';
  import SkipForward from 'phosphor-svelte/lib/SkipForward';
  import ArrowsOut from 'phosphor-svelte/lib/ArrowsOut';
  import Bug from 'phosphor-svelte/lib/Bug';
  import Gear from 'phosphor-svelte/lib/Gear';
  import FolderOpen from 'phosphor-svelte/lib/FolderOpen';
  import { Button } from '$lib/components/ui/button/index.js';
  import { getEmuContext } from '$lib/context.js';
  import { emulator } from '$lib/stores/emulator.svelte.js';
  import { settings } from '$lib/stores/settings.svelte.js';
  import { writeQuirksToApi } from '$lib/quirks.js';
  import RomPicker from './RomPicker.svelte';

  interface Props {
    settingsOpen: boolean;
  }

  let { settingsOpen = $bindable() }: Props = $props();
  let romPickerOpen = $state(false);

  const { api, audio } = getEmuContext();

  function toggleRun(): void {
    if (!emulator.running) {
      audio.ensureStarted();
      api.Start();
      emulator.running = true;
      emulator.paused = false;
      emulator.status = `Running ${emulator.lastRomName ?? 'program'}`;
    } else {
      emulator.paused = !emulator.paused;
      emulator.status = emulator.paused ? 'Paused' : `Running ${emulator.lastRomName ?? 'program'}`;
    }
  }

  function reset(): void {
    api.Stop();
    api.Init();
    writeQuirksToApi(api, settings.quirks);
    api.SetInstructionsPerSecond(settings.ips);
    if (emulator.lastRomBytes) {
      api.LoadProgram(emulator.lastRomBytes);
    }
    emulator.running = false;
    emulator.paused = true;
    emulator.pc = api.GetProgramCounter();
    emulator.prevInsLine = null;
    emulator.status = emulator.lastRomName
      ? `Reset. Press Start to run ${emulator.lastRomName}.`
      : 'Reset. Load a ROM to begin.';
  }

  function step(): void {
    if (!emulator.running) return;
    api.Step();
    emulator.pc = api.GetProgramCounter();
  }

  function fullscreen(): void {
    window.__chip8_fullscreen?.();
  }
</script>

<div class="flex items-center gap-1">
  <Button
    variant="outline"
    size="icon-sm"
    onclick={() => { romPickerOpen = !romPickerOpen; }}
    title="Load ROM"
    aria-label="Load ROM"
  >
    <FolderOpen />
  </Button>

  <Button
    variant="outline"
    size="icon-sm"
    onclick={toggleRun}
    disabled={!emulator.lastRomName}
    title={emulator.running && !emulator.paused ? 'Pause' : 'Start'}
    aria-label={emulator.running && !emulator.paused ? 'Pause' : 'Start'}
  >
    {#if emulator.running && !emulator.paused}
      <Pause />
    {:else}
      <Play />
    {/if}
  </Button>

  <Button
    variant="outline"
    size="icon-sm"
    onclick={step}
    disabled={!emulator.running || !emulator.paused}
    title="Step (one instruction)"
    aria-label="Step"
  >
    <SkipForward />
  </Button>

  <Button
    variant="outline"
    size="icon-sm"
    onclick={reset}
    disabled={!emulator.lastRomName}
    title="Reset"
    aria-label="Reset"
  >
    <ArrowClockwise />
  </Button>

  <span class="mx-1 h-4 w-px bg-border/60" aria-hidden="true"></span>

  <Button
    variant="ghost"
    size="icon-sm"
    onclick={fullscreen}
    title="Fullscreen"
    aria-label="Fullscreen"
  >
    <ArrowsOut />
  </Button>

  <Button
    variant="ghost"
    size="icon-sm"
    onclick={() => { settings.debugOpen = !settings.debugOpen; }}
    title={settings.debugOpen ? 'Hide debug' : 'Show debug'}
    aria-label="Toggle debug panel"
    aria-pressed={settings.debugOpen}
  >
    <Bug />
  </Button>

  <Button
    variant="ghost"
    size="icon-sm"
    onclick={() => { settingsOpen = true; }}
    title="Settings"
    aria-label="Open settings"
  >
    <Gear />
  </Button>
</div>

<RomPicker bind:open={romPickerOpen} />
