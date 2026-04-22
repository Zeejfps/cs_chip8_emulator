<script lang="ts">
  import { onMount } from 'svelte';
  import Upload from 'phosphor-svelte/lib/Upload';
  import File from 'phosphor-svelte/lib/File';
  import { Sheet, SheetContent, SheetHeader, SheetTitle, SheetDescription } from '$lib/components/ui/sheet/index.js';
  import { Button } from '$lib/components/ui/button/index.js';
  import { getEmuContext } from '$lib/context.js';
  import { emulator } from '$lib/stores/emulator.svelte.js';
  import { settings } from '$lib/stores/settings.svelte.js';
  import { loadManifest, loadRomBytes, type RomEntry } from '$lib/roms.js';
  import { QUIRK_PRESETS, writeQuirksToApi } from '$lib/quirks.js';

  interface Props {
    open: boolean;
  }

  let { open = $bindable() }: Props = $props();

  const { api, audio } = getEmuContext();

  let roms = $state<RomEntry[]>([]);
  let manifestError = $state<string | null>(null);
  let loadError = $state<string | null>(null);
  let fileInput = $state<HTMLInputElement | null>(null);

  onMount(() => {
    loadManifest()
      .then((entries) => { roms = entries; })
      .catch((err) => {
        console.warn('ROM manifest unavailable', err);
        manifestError = 'Bundled ROMs unavailable.';
      });
  });

  async function loadBundled(entry: RomEntry): Promise<void> {
    loadError = null;
    try {
      const bytes = await loadRomBytes(entry);
      api.Stop();
      api.Init();
      api.LoadProgram(bytes);

      const quirks = QUIRK_PRESETS[entry.preferredQuirks];
      settings.quirks = { ...quirks };
      settings.quirksPreset = entry.preferredQuirks;
      writeQuirksToApi(api, quirks);

      audio.ensureStarted();
      api.Start();
      emulator.running = true;
      emulator.paused = false;
      emulator.pc = api.GetProgramCounter();
      emulator.lastRomName = entry.title;
      emulator.lastRomBytes = bytes;
      settings.lastRomId = entry.id;
      emulator.status = `Running ${entry.title}`;
      open = false;
    } catch (err) {
      loadError = err instanceof Error ? err.message : String(err);
    }
  }

  async function loadFile(ev: Event): Promise<void> {
    const input = ev.currentTarget as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;
    loadError = null;
    try {
      const bytes = new Uint8Array(await file.arrayBuffer());
      api.Stop();
      api.Init();
      api.LoadProgram(bytes);
      writeQuirksToApi(api, settings.quirks);
      audio.ensureStarted();
      api.Start();
      emulator.running = true;
      emulator.paused = false;
      emulator.pc = api.GetProgramCounter();
      emulator.lastRomName = file.name;
      emulator.lastRomBytes = bytes;
      settings.lastRomId = null;
      emulator.status = `Running ${file.name}`;
      open = false;
    } catch (err) {
      loadError = err instanceof Error ? err.message : String(err);
    } finally {
      input.value = '';
    }
  }
</script>

<Sheet bind:open>
  <SheetContent side="left" class="gap-0">
    <SheetHeader>
      <SheetTitle class="font-pixel phosphor-text tracking-wider">Load ROM</SheetTitle>
      <SheetDescription>Pick a bundled title or upload your own <code>.ch8</code> file.</SheetDescription>
    </SheetHeader>

    <div class="flex flex-col gap-4 overflow-y-auto px-4 pb-6">
      <div class="flex items-center gap-2">
        <input
          bind:this={fileInput}
          type="file"
          accept=".ch8,.rom,.bin,application/octet-stream"
          class="sr-only"
          onchange={loadFile}
        />
        <Button variant="outline" size="sm" onclick={() => fileInput?.click()}>
          <Upload />
          Upload .ch8
        </Button>
      </div>

      {#if loadError}
        <p class="text-destructive text-xs">{loadError}</p>
      {/if}

      <div class="flex flex-col gap-1">
        <h3 class="font-pixel text-xs tracking-wider text-muted-foreground">Bundled</h3>
        {#if manifestError}
          <p class="text-xs text-muted-foreground">{manifestError}</p>
        {:else if roms.length === 0}
          <p class="text-xs text-muted-foreground">Loading…</p>
        {:else}
          <ul class="flex flex-col divide-y divide-border/40">
            {#each roms as rom (rom.id)}
              <li>
                <button
                  type="button"
                  class="flex w-full items-start gap-2 py-2 text-left hover:bg-muted/30"
                  onclick={() => loadBundled(rom)}
                >
                  <File class="mt-0.5 shrink-0 text-muted-foreground" />
                  <span class="flex min-w-0 flex-col">
                    <span class="font-pixel phosphor-text text-sm tracking-wide">{rom.title}</span>
                    <span class="text-xs text-muted-foreground truncate">
                      {rom.author} — {rom.license}
                    </span>
                    {#if rom.description}
                      <span class="text-[11px] text-muted-foreground/80">{rom.description}</span>
                    {/if}
                  </span>
                </button>
              </li>
            {/each}
          </ul>
        {/if}
      </div>
    </div>
  </SheetContent>
</Sheet>
