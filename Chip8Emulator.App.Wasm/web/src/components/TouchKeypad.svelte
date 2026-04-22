<script lang="ts">
  import { getEmuContext } from '$lib/context.js';
  import { KEYPAD_ROWS, hexLabel, keyboardLabel } from '$lib/keymap.js';
  import { settings } from '$lib/stores/settings.svelte.js';

  const { api } = getEmuContext();

  const active = new Map<number, number>();

  function press(hex: number, pointerId: number, target: HTMLElement): void {
    if (active.get(pointerId) === hex) return;
    const prev = active.get(pointerId);
    if (prev !== undefined && prev !== hex) {
      api.SetKey(prev, false);
    }
    active.set(pointerId, hex);
    target.setPointerCapture?.(pointerId);
    api.SetKey(hex, true);
  }

  function release(pointerId: number): void {
    const hex = active.get(pointerId);
    if (hex === undefined) return;
    active.delete(pointerId);
    api.SetKey(hex, false);
  }

  function onPointerDown(ev: PointerEvent, hex: number): void {
    ev.preventDefault();
    press(hex, ev.pointerId, ev.currentTarget as HTMLElement);
  }

  function onPointerUp(ev: PointerEvent): void {
    ev.preventDefault();
    release(ev.pointerId);
  }
</script>

<div class="touch-keypad mx-auto grid w-full max-w-md grid-cols-4 gap-2 select-none">
  {#each KEYPAD_ROWS as row, rowIdx (rowIdx)}
    {#each row as hex (hex)}
      <button
        type="button"
        class="font-pixel phosphor-text flex aspect-square items-center justify-center rounded border border-border/60 bg-muted/30 text-xl tracking-wider active:bg-primary/30 active:translate-y-px"
        onpointerdown={(e) => onPointerDown(e, hex)}
        onpointerup={onPointerUp}
        onpointercancel={onPointerUp}
        onpointerleave={onPointerUp}
        oncontextmenu={(e) => e.preventDefault()}
        aria-label={`CHIP-8 key ${settings.keypadLabelMode === 'keyboard' ? keyboardLabel(hex) : hexLabel(hex)}`}
      >
        {settings.keypadLabelMode === 'keyboard' ? keyboardLabel(hex) : hexLabel(hex)}
      </button>
    {/each}
  {/each}
</div>

<style>
  .touch-keypad button {
    touch-action: none;
  }
</style>
