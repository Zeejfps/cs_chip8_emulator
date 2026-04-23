<script lang="ts">
  import SpeakerHigh from 'phosphor-svelte/lib/SpeakerHigh';
  import SpeakerNone from 'phosphor-svelte/lib/SpeakerNone';
  import { Slider } from '$lib/components/ui/slider/index.js';
  import { Label } from '$lib/components/ui/label/index.js';
  import { Button } from '$lib/components/ui/button/index.js';
  import { getEmuContext } from '$lib/context.js';
  import { settings } from '$lib/stores/settings.svelte.js';

  const { audio } = getEmuContext();

  $effect(() => {
    audio.setVolume(settings.volume);
  });

  $effect(() => {
    audio.setMuted(settings.muted);
  });

  function onChange(v: number): void {
    settings.volume = v / 100;
    if (settings.muted && settings.volume > 0) settings.muted = false;
  }
</script>

<div class="flex flex-col gap-2">
  <Label class="font-pixel text-xs tracking-wider">Volume</Label>
  <div class="flex items-center gap-2">
    <Button
      variant="ghost"
      size="icon-sm"
      onclick={() => {
        settings.muted = !settings.muted;
      }}
      aria-pressed={settings.muted}
      aria-label={settings.muted ? 'Unmute' : 'Mute'}
    >
      {#if settings.muted}
        <SpeakerNone />
      {:else}
        <SpeakerHigh />
      {/if}
    </Button>
    <Slider
      type="single"
      min={0}
      max={100}
      step={1}
      value={Math.round(settings.volume * 100)}
      onValueChange={onChange}
      class="flex-1"
    />
    <span class="font-pixel text-muted-foreground w-10 text-right text-[11px]">
      {Math.round(settings.volume * 100)}%
    </span>
  </div>
</div>
