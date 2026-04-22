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
  import Disasm from './Disasm.svelte';

  const { api } = getEmuContext();
  const isMobile = $derived(viewport.width < 768);

  function step(): void {
    stepEmulator(api);
  }
</script>

{#if isMobile}
  <Sheet bind:open={settings.debugOpen}>
    <SheetContent side="bottom" class="h-[70vh] gap-0" overlayClass="bg-transparent supports-backdrop-filter:backdrop-blur-none">
      <SheetHeader>
        <div class="flex items-center gap-2">
          <SheetTitle class="font-pixel phosphor-text tracking-wider">Debug</SheetTitle>
          <Button
            variant="outline"
            size="icon-sm"
            onclick={step}
            disabled={!emulator.running}
            title="Step (one instruction)"
            aria-label="Step"
          >
            <SkipForward />
          </Button>
        </div>
      </SheetHeader>
      <div class="flex min-h-0 flex-1 flex-col gap-3 overflow-y-auto px-4 pb-6">
        <Disasm />
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
            variant="outline"
            size="icon-sm"
            onclick={step}
            disabled={!emulator.running}
            title="Step (one instruction)"
            aria-label="Step"
          >
            <SkipForward />
          </Button>
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
        <Disasm />
      </div>
    </CollapsibleContent>
  </Collapsible>
{/if}
