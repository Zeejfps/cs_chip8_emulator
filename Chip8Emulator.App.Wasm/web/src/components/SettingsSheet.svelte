<script lang="ts">
  import { Sheet, SheetContent, SheetHeader, SheetTitle, SheetDescription } from '$lib/components/ui/sheet/index.js';
  import { Tabs, TabsContent, TabsList, TabsTrigger } from '$lib/components/ui/tabs/index.js';
  import { Switch } from '$lib/components/ui/switch/index.js';
  import { Label } from '$lib/components/ui/label/index.js';
  import { settings } from '$lib/stores/settings.svelte.js';
  import QuirksPanel from './QuirksPanel.svelte';
  import SpeedControl from './SpeedControl.svelte';
  import VolumeControl from './VolumeControl.svelte';

  interface Props {
    open: boolean;
  }

  let { open = $bindable() }: Props = $props();
</script>

<Sheet bind:open>
  <SheetContent side="right" class="gap-0 overflow-x-hidden sm:max-w-md">
    <SheetHeader>
      <SheetTitle class="font-pixel phosphor-text tracking-wider">Settings</SheetTitle>
      <SheetDescription>Tune timing, audio, and compatibility.</SheetDescription>
    </SheetHeader>

    <div class="flex min-w-0 min-h-0 flex-1 flex-col px-4 pb-6">
      <Tabs value="quirks" class="flex min-w-0 min-h-0 flex-1 flex-col">
        <TabsList class="grid w-full grid-cols-4">
          <TabsTrigger value="quirks">Quirks</TabsTrigger>
          <TabsTrigger value="display">Display</TabsTrigger>
          <TabsTrigger value="audio">Audio</TabsTrigger>
          <TabsTrigger value="about">About</TabsTrigger>
        </TabsList>

        <div class="min-w-0 min-h-0 flex-1 overflow-x-hidden overflow-y-auto pt-3">
          <TabsContent value="quirks" class="mt-0 flex flex-col gap-4">
            <SpeedControl />
            <QuirksPanel />
          </TabsContent>

          <TabsContent value="display" class="mt-0 flex flex-col gap-4">
            <div class="flex flex-col gap-2">
              <Label class="font-pixel text-xs tracking-wider">Phosphor</Label>
              <div class="flex gap-2">
                <button
                  type="button"
                  class="flex-1 rounded border px-2 py-1 text-xs {settings.phosphor === 'green' ? 'bg-primary/20 border-primary/60' : 'border-border/60'}"
                  onclick={() => { settings.phosphor = 'green'; }}
                >
                  Green
                </button>
                <button
                  type="button"
                  class="flex-1 rounded border px-2 py-1 text-xs {settings.phosphor === 'amber' ? 'bg-primary/20 border-primary/60' : 'border-border/60'}"
                  onclick={() => { settings.phosphor = 'amber'; }}
                >
                  Amber
                </button>
              </div>
            </div>
            <div class="flex items-center justify-between gap-2">
              <div class="flex flex-col gap-0.5">
                <span class="font-pixel text-xs tracking-wider">Scanlines</span>
                <span class="text-[11px] text-muted-foreground">CRT phosphor line overlay</span>
              </div>
              <Switch id="scanlines" bind:checked={settings.scanlines} />
            </div>
            <div class="flex items-center justify-between gap-2">
              <div class="flex flex-col gap-0.5">
                <span class="font-pixel text-xs tracking-wider">Touch keypad</span>
                <span class="text-[11px] text-muted-foreground">
                  {settings.touchKeypadManual === null ? 'Auto (on mobile)' : settings.touchKeypadManual ? 'Always on' : 'Always off'}
                </span>
              </div>
              <div class="flex gap-1">
                <button
                  type="button"
                  class="rounded border px-2 py-0.5 text-xs {settings.touchKeypadManual === null ? 'bg-primary/20 border-primary/60' : 'border-border/60'}"
                  onclick={() => { settings.touchKeypadManual = null; }}
                >Auto</button>
                <button
                  type="button"
                  class="rounded border px-2 py-0.5 text-xs {settings.touchKeypadManual === true ? 'bg-primary/20 border-primary/60' : 'border-border/60'}"
                  onclick={() => { settings.touchKeypadManual = true; }}
                >On</button>
                <button
                  type="button"
                  class="rounded border px-2 py-0.5 text-xs {settings.touchKeypadManual === false ? 'bg-primary/20 border-primary/60' : 'border-border/60'}"
                  onclick={() => { settings.touchKeypadManual = false; }}
                >Off</button>
              </div>
            </div>
          </TabsContent>

          <TabsContent value="audio" class="mt-0 flex flex-col gap-4">
            <VolumeControl />
          </TabsContent>

          <TabsContent value="about" class="mt-0 flex flex-col gap-3 text-xs text-muted-foreground">
            <p>
              CHIP-8 emulator written in C# and compiled to WebAssembly. UI built with Svelte 5,
              Tailwind v4, and shadcn-svelte.
            </p>
            <p>
              Bundled ROMs are from the
              <a class="underline" href="https://github.com/JohnEarnest/chip8Archive" target="_blank" rel="noreferrer">chip8Archive</a>
              by John Earnest and contributors (CC0/CC-BY).
            </p>
            <p>
              Keyboard mapping: <code>1234 / QWER / ASDF / ZXCV</code> →
              <code>123C / 456D / 789E / A0BF</code>.
            </p>
          </TabsContent>
        </div>
      </Tabs>
    </div>
  </SheetContent>
</Sheet>
