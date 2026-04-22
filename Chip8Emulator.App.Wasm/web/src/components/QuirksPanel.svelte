<script lang="ts">
  import { Switch } from '$lib/components/ui/switch/index.js';
  import { Label } from '$lib/components/ui/label/index.js';
  import { getEmuContext } from '$lib/context.js';
  import { settings } from '$lib/stores/settings.svelte.js';
  import {
    QUIRK_PRESETS,
    PRESET_LABELS,
    reconcilePreset,
    writeQuirksToApi,
    type PresetName,
  } from '$lib/quirks.js';

  const { api } = getEmuContext();

  const QUIRK_FIELDS: Array<{ key: keyof typeof settings.quirks; label: string; hint: string }> = [
    { key: 'shiftVy', label: 'Shift uses VY', hint: '8XY6 / 8XYE shifts VY into VX' },
    { key: 'jumpVx', label: 'Jump BNNN uses VX', hint: 'BXNN jumps to NNN + VX' },
    { key: 'lsIncI', label: 'Load/store incs I', hint: 'FX55 / FX65 increments I' },
    { key: 'logicVf', label: 'Logic resets VF', hint: '8XY1/2/3 clear VF' },
    { key: 'wrap', label: 'Sprites wrap', hint: 'Sprites wrap at screen edges' },
    { key: 'dispWait', label: 'Display wait', hint: 'DXYN waits for vblank' },
    {
      key: 'vfResultLast',
      label: 'VF result kept',
      hint: 'Math ops preserve result when VX is VF',
    },
  ];

  $effect(() => {
    writeQuirksToApi(api, settings.quirks);
    settings.quirksPreset = reconcilePreset(settings.quirksPreset, settings.quirks);
  });

  function applyPreset(name: PresetName): void {
    settings.quirks = { ...QUIRK_PRESETS[name] };
    settings.quirksPreset = name;
  }
</script>

<div class="flex flex-col gap-3">
  <div class="flex flex-col gap-1">
    <Label class="font-pixel text-xs tracking-wider">Preset</Label>
    <div class="grid grid-cols-2 gap-1">
      {#each Object.keys(PRESET_LABELS) as name (name)}
        <button
          type="button"
          class="relative z-10 rounded border px-2 py-1 text-xs {settings.quirksPreset === name
            ? 'bg-primary/20 border-primary/60 phosphor-text'
            : 'border-border/60'}"
          onclick={() => applyPreset(name as PresetName)}
        >
          {PRESET_LABELS[name as PresetName]}
        </button>
      {/each}
    </div>
    {#if settings.quirksPreset === 'custom'}
      <span class="text-muted-foreground text-[11px]">Custom combination</span>
    {/if}
  </div>

  <div class="flex flex-col gap-2">
    <Label class="font-pixel text-xs tracking-wider">Flags</Label>
    {#each QUIRK_FIELDS as field (field.key)}
      <div class="flex items-center justify-between gap-2">
        <div class="flex flex-col gap-0.5">
          <span class="text-xs">{field.label}</span>
          <span class="text-muted-foreground text-[11px]">{field.hint}</span>
        </div>
        <Switch bind:checked={settings.quirks[field.key]} />
      </div>
    {/each}
  </div>
</div>
