<script lang="ts">
  import { onMount } from 'svelte';
  import { Sheet, SheetContent, SheetHeader, SheetTitle } from '$lib/components/ui/sheet/index.js';
  import { Collapsible, CollapsibleContent } from '$lib/components/ui/collapsible/index.js';
  import { settings } from '$lib/stores/settings.svelte.js';
  import Disasm from './Disasm.svelte';

  let viewportWidth = $state(typeof window === 'undefined' ? 1024 : window.innerWidth);

  onMount(() => {
    const onResize = () => { viewportWidth = window.innerWidth; };
    window.addEventListener('resize', onResize, { passive: true });
    return () => window.removeEventListener('resize', onResize);
  });

  const isMobile = $derived(viewportWidth < 768);
</script>

{#if isMobile}
  <Sheet bind:open={settings.debugOpen}>
    <SheetContent side="bottom" class="h-[70vh] gap-0">
      <SheetHeader>
        <SheetTitle class="font-pixel phosphor-text tracking-wider">Debug</SheetTitle>
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
        <h2 class="font-pixel phosphor-text text-xs tracking-wider">Debug</h2>
        <Disasm />
      </div>
    </CollapsibleContent>
  </Collapsible>
{/if}
