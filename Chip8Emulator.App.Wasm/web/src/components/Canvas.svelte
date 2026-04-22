<script lang="ts">
  import { onMount } from 'svelte';
  import { getEmuContext } from '$lib/context.js';
  import { emulator } from '$lib/stores/emulator.svelte.js';
  import { settings } from '$lib/stores/settings.svelte.js';
  import { registerFullscreen } from '$lib/emulator-actions.js';

  const { api, runtime } = getEmuContext();

  let canvasEl = $state<HTMLCanvasElement | null>(null);
  let wrapperEl = $state<HTMLDivElement | null>(null);

  const WIDTH = api.GetWidth();
  const HEIGHT = api.GetHeight();

  const PHOSPHOR_COLORS = {
    green: { on: [0x6d, 0xff, 0x9c], off: [0x0a, 0x12, 0x0d] },
    amber: { on: [0xff, 0xb8, 0x47], off: [0x10, 0x0a, 0x02] },
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
        const rgb = view[i] !== 0 ? palette.on : palette.off;
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
  class="crt-screen relative mx-auto w-full overflow-hidden rounded-md border border-border/60 bg-black"
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
</div>

<style>
  .crt-screen {
    box-shadow:
      inset 0 0 60px rgba(0, 0, 0, 0.8),
      0 0 0 1px color-mix(in oklab, var(--phosphor-on, #6dff9c) 20%, transparent);
  }
</style>
