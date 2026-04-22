<script lang="ts">
  import { onMount } from 'svelte';
  import FolderOpen from 'phosphor-svelte/lib/FolderOpen';
  import { getEmuContext } from '$lib/context.js';
  import { emulator } from '$lib/stores/emulator.svelte.js';
  import { settings } from '$lib/stores/settings.svelte.js';
  import { registerFullscreen } from '$lib/emulator-actions.js';

  interface Props {
    onOpenRomPicker?: () => void;
  }

  const { onOpenRomPicker }: Props = $props();

  const { api, runtime } = getEmuContext();

  let canvasEl = $state<HTMLCanvasElement | null>(null);
  let wrapperEl = $state<HTMLDivElement | null>(null);

  const WIDTH = api.GetWidth();
  const HEIGHT = api.GetHeight();

  // Four-entry XO-Chip palette: [plane-mask value 0..3] -> [r, g, b].
  // 0 = both planes off, 1 = plane 0 only, 2 = plane 1 only, 3 = both planes on.
  const PHOSPHOR_COLORS = {
    green: [
      [0x0a, 0x12, 0x0d],
      [0x6d, 0xff, 0x9c],
      [0x30, 0x8a, 0x4e],
      [0xc0, 0xff, 0xd6],
    ],
    amber: [
      [0x10, 0x0a, 0x02],
      [0xff, 0xb8, 0x47],
      [0x8a, 0x5e, 0x22],
      [0xff, 0xe0, 0xa8],
    ],
  } as const;

  function requestFullscreen(): void {
    const el = wrapperEl;
    if (!el) return;
    if (document.fullscreenElement === el) {
      void document.exitFullscreen();
    } else {
      void el.requestFullscreen().catch(() => {});
    }
  }

  $effect(() => registerFullscreen(requestFullscreen));

  onMount(() => {
    if (!canvasEl) return;
    const ctx = canvasEl.getContext('2d');
    if (!ctx) throw new Error('Canvas 2D context unavailable');
    ctx.imageSmoothingEnabled = false;

    const imageData = ctx.createImageData(WIDTH, HEIGHT);
    const pixels = imageData.data;
    for (let i = 3; i < pixels.length; i += 4) pixels[i] = 255;

    const pixelLen = api.GetPixelDataLength();

    let rafId = 0;
    let lastPc = -1;
    let running = true;

    const render = () => {
      if (!running) return;
      rafId = requestAnimationFrame(render);

      if (emulator.running && !emulator.paused) {
        try {
          api.Tick();
        } catch (err) {
          console.error('Tick error', err);
          emulator.running = false;
          emulator.paused = true;
          emulator.status = 'Emulator halted due to error.';
          return;
        }

        const pc = api.GetProgramCounter();
        if (pc !== lastPc) {
          lastPc = pc;
          emulator.pc = pc;
        }
      }

      const palette = PHOSPHOR_COLORS[settings.phosphor];
      const ptr = api.GetPixelDataPtr();
      const view = runtime.localHeapViewU8().subarray(ptr, ptr + pixelLen);
      for (let i = 0; i < pixelLen; i++) {
        const rgb = palette[view[i] & 0x03];
        const o = i * 4;
        pixels[o] = rgb[0];
        pixels[o + 1] = rgb[1];
        pixels[o + 2] = rgb[2];
      }
      ctx.putImageData(imageData, 0, 0);
    };

    rafId = requestAnimationFrame(render);

    return () => {
      running = false;
      cancelAnimationFrame(rafId);
    };
  });
</script>

<div
  bind:this={wrapperEl}
  class="crt-screen border-border/60 relative mx-auto w-full overflow-hidden rounded-md border bg-black"
  style="aspect-ratio: {WIDTH} / {HEIGHT};"
>
  <canvas
    bind:this={canvasEl}
    width={WIDTH}
    height={HEIGHT}
    class="pixel-canvas block h-full w-full"
  ></canvas>
  {#if settings.scanlines}
    <div class="scanlines flicker"></div>
  {/if}
  {#if !emulator.lastRomName}
    <button
      class="absolute inset-0 flex cursor-pointer flex-col items-center justify-center gap-3 bg-black/70 transition-colors hover:bg-black/60"
      onclick={onOpenRomPicker}
      aria-label="Load ROM"
    >
      <FolderOpen size={48} class="phosphor-text opacity-80" />
      <span class="phosphor-text font-pixel text-sm tracking-widest opacity-80">
        Click to load a ROM
      </span>
    </button>
  {/if}
</div>

<style>
  .crt-screen {
    box-shadow:
      inset 0 0 60px rgba(0, 0, 0, 0.8),
      0 0 0 1px color-mix(in oklab, var(--phosphor-on, #6dff9c) 20%, transparent);
  }
</style>
