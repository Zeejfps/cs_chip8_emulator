<script lang="ts">
  import { Slider } from '$lib/components/ui/slider/index.js';
  import { Label } from '$lib/components/ui/label/index.js';
  import { getEmuContext } from '$lib/context.js';
  import {
    settings,
    IPS_MIN,
    IPS_MAX,
    SPEED_PRESET_IPS,
    type SpeedPreset,
  } from '$lib/stores/settings.svelte.js';

  const { api } = getEmuContext();

  const PRESETS: SpeedPreset[] = ['0.5x', '1x', '2x', 'max'];

  $effect(() => {
    api.SetInstructionsPerSecond(settings.ips);
  });

  function applyPreset(preset: SpeedPreset): void {
    settings.speedPreset = preset;
    settings.ips = SPEED_PRESET_IPS[preset];
  }

  function onSliderChange(v: number): void {
    settings.ips = v;
    const match = PRESETS.find((p) => SPEED_PRESET_IPS[p] === v);
    settings.speedPreset = match ?? settings.speedPreset;
  }
</script>

<div class="flex flex-col gap-2">
  <div class="flex items-center justify-between">
    <Label class="font-pixel text-xs tracking-wider">Speed</Label>
    <span class="font-pixel text-muted-foreground text-[11px]">{settings.ips} IPS</span>
  </div>
  <div class="grid grid-cols-4 gap-1">
    {#each PRESETS as preset (preset)}
      <button
        type="button"
        class="rounded border px-2 py-1 text-xs {settings.speedPreset === preset
          ? 'bg-primary/20 border-primary/60 phosphor-text'
          : 'border-border/60'}"
        onclick={() => applyPreset(preset)}
      >
        {preset}
      </button>
    {/each}
  </div>
  <Slider
    type="single"
    min={IPS_MIN}
    max={IPS_MAX}
    step={100}
    value={settings.ips}
    onValueChange={onSliderChange}
  />
</div>
