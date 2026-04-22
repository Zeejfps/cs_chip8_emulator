<script lang="ts">
  import { emulator } from '$lib/stores/emulator.svelte.js';
  import { settings } from '$lib/stores/settings.svelte.js';
  import { PRESET_LABELS } from '$lib/quirks.js';
  import { hex4 } from '$lib/format.js';

  const presetLabel = $derived(
    settings.quirksPreset === 'custom' ? 'CUSTOM' : PRESET_LABELS[settings.quirksPreset],
  );
</script>

<div class="font-pixel flex flex-wrap items-center justify-between gap-x-4 gap-y-1 text-[11px] tracking-wider text-muted-foreground">
  <span class="phosphor-text truncate">{emulator.status}</span>
  <span class="flex items-center gap-3">
    <span>{presetLabel}</span>
    <span>{settings.ips} IPS</span>
    <span>PC {hex4(emulator.pc)}</span>
  </span>
</div>
