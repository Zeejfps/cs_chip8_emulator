<script lang="ts">
  import SkipForward from 'phosphor-svelte/lib/SkipForward';
  import XIcon from 'phosphor-svelte/lib/X';
  import { Sheet, SheetContent, SheetHeader, SheetTitle } from '$lib/components/ui/sheet/index.js';
  import { Collapsible, CollapsibleContent } from '$lib/components/ui/collapsible/index.js';
  import { Button } from '$lib/components/ui/button/index.js';
  import { getEmuContext } from '$lib/context.js';
  import { emulator } from '$lib/stores/emulator.svelte.js';
  import { settings } from '$lib/stores/settings.svelte.js';
  import { viewport } from '$lib/stores/viewport.svelte.js';
  import { stepEmulator } from '$lib/emulator-actions.js';
  import MachineState from './MachineState.svelte';

  const { api } = getEmuContext();
  const isMobile = $derived(viewport.width < 768);

  const STEP_REPEAT_DELAY_MS = 300;
  const STEP_REPEAT_INTERVAL_MS = 60;

  let holdTimer: ReturnType<typeof setTimeout> | null = null;
  let repeatTimer: ReturnType<typeof setInterval> | null = null;

  function step(): void {
    stepEmulator(api);
  }

  function startStepHold(): void {
    if (!emulator.running) return;
    step();
    holdTimer = setTimeout(() => {
      repeatTimer = setInterval(step, STEP_REPEAT_INTERVAL_MS);
    }, STEP_REPEAT_DELAY_MS);
  }

  function stopStepHold(): void {
    if (holdTimer !== null) {
      clearTimeout(holdTimer);
      holdTimer = null;
    }
    if (repeatTimer !== null) {
      clearInterval(repeatTimer);
      repeatTimer = null;
    }
  }

  $effect(() => () => stopStepHold());
</script>

{#snippet actionBar()}
  <div class="flex justify-end gap-2">
    <Button
      variant="outline"
      onpointerdown={startStepHold}
      onpointerup={stopStepHold}
      onpointerleave={stopStepHold}
      onpointercancel={stopStepHold}
      disabled={!emulator.running}
      title="Step (hold to repeat)"
      aria-label="Step"
    >
      <SkipForward />
      <span class="font-pixel tracking-wider">STEP</span>
    </Button>
  </div>
{/snippet}

{#if isMobile}
  <Sheet bind:open={settings.debugOpen}>
    <SheetContent side="bottom" class="h-[70vh] gap-0" overlayClass="bg-transparent supports-backdrop-filter:backdrop-blur-none">
      <SheetHeader>
        <SheetTitle class="font-pixel phosphor-text tracking-wider">Debug</SheetTitle>
      </SheetHeader>
      <div class="flex min-h-0 flex-1 flex-col gap-3 overflow-y-auto px-4">
        <MachineState />
      </div>
      <div class="px-4 py-3">
        {@render actionBar()}
      </div>
    </SheetContent>
  </Sheet>
{:else}
  <Collapsible bind:open={settings.debugOpen}>
    <CollapsibleContent>
      <div class="flex flex-col gap-3 rounded-md border border-border/60 bg-card/40 p-3">
        <div class="flex items-center gap-2">
          <h2 class="font-pixel phosphor-text text-xs tracking-wider">Debug</h2>
          <Button
            variant="ghost"
            size="icon-sm"
            class="ml-auto"
            onclick={() => { settings.debugOpen = false; }}
            title="Hide debug"
            aria-label="Hide debug"
          >
            <XIcon />
          </Button>
        </div>
        <MachineState />
        {@render actionBar()}
      </div>
    </CollapsibleContent>
  </Collapsible>
{/if}
