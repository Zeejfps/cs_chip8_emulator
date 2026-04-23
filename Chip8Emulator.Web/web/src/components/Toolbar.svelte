<script lang="ts">
  import ArrowClockwise from 'phosphor-svelte/lib/ArrowClockwise';
  import ArrowsOut from 'phosphor-svelte/lib/ArrowsOut';
  import Bug from 'phosphor-svelte/lib/Bug';
  import Gear from 'phosphor-svelte/lib/Gear';
  import FolderOpen from 'phosphor-svelte/lib/FolderOpen';
  import { Button } from '$lib/components/ui/button/index.js';
  import { getEmuContext } from '$lib/context.js';
  import { emulator } from '$lib/stores/emulator.svelte.js';
  import { settings } from '$lib/stores/settings.svelte.js';
  import { viewport } from '$lib/stores/viewport.svelte.js';
  import { resetEmulator, toggleFullscreen } from '$lib/emulator-actions.js';
  import RomPicker from './RomPicker.svelte';

  interface Props {
    settingsOpen: boolean;
    romPickerOpen: boolean;
  }

  let { settingsOpen = $bindable(), romPickerOpen = $bindable() }: Props = $props();

  const { api } = getEmuContext();
</script>

<div class="flex items-center gap-1">
  <Button
    variant="outline"
    size="icon-sm"
    onclick={() => {
      romPickerOpen = !romPickerOpen;
    }}
    title="Load ROM"
    aria-label="Load ROM"
  >
    <FolderOpen />
  </Button>

  <Button
    variant="outline"
    size="icon-sm"
    onclick={() => resetEmulator(api)}
    disabled={!emulator.lastRomName}
    title="Reset"
    aria-label="Reset"
  >
    <ArrowClockwise />
  </Button>

  {#if viewport.width >= 768}
    <Button
      variant="ghost"
      size="icon-sm"
      onclick={toggleFullscreen}
      title="Fullscreen"
      aria-label="Fullscreen"
    >
      <ArrowsOut />
    </Button>
  {/if}

  <Button
    variant="ghost"
    size="icon-sm"
    onclick={() => {
      settings.debugOpen = !settings.debugOpen;
    }}
    title={settings.debugOpen ? 'Hide debug' : 'Show debug'}
    aria-label="Toggle debug panel"
    aria-pressed={settings.debugOpen}
  >
    <Bug />
  </Button>

  <Button
    variant="ghost"
    size="icon-sm"
    onclick={() => {
      settingsOpen = true;
    }}
    title="Settings"
    aria-label="Open settings"
  >
    <Gear />
  </Button>
</div>

<RomPicker bind:open={romPickerOpen} />
