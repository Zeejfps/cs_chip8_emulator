<script lang="ts">
  import { Sheet, SheetContent, SheetHeader, SheetTitle, SheetDescription } from '$lib/components/ui/sheet/index.js';
  import { Tabs, TabsContent, TabsList, TabsTrigger } from '$lib/components/ui/tabs/index.js';
  import { Switch } from '$lib/components/ui/switch/index.js';
  import { Label } from '$lib/components/ui/label/index.js';
  import { settings, type Phosphor } from '$lib/stores/settings.svelte.js';
  import QuirksPanel from './QuirksPanel.svelte';
  import SpeedControl from './SpeedControl.svelte';
  import VolumeControl from './VolumeControl.svelte';
  import GithubLogo from 'phosphor-svelte/lib/GithubLogo';

  interface Props {
    open: boolean;
  }

  let { open = $bindable() }: Props = $props();

  const PHOSPHOR_OPTIONS: { value: Phosphor; label: string }[] = [
    { value: 'green', label: 'Green' },
    { value: 'amber', label: 'Amber' },
  ];

  const TOUCH_KEYPAD_OPTIONS: { value: boolean | null; label: string }[] = [
    { value: null,  label: 'Auto' },
    { value: true,  label: 'On' },
    { value: false, label: 'Off' },
  ];
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
                {#each PHOSPHOR_OPTIONS as opt (opt.value)}
                  <button
                    type="button"
                    class="flex-1 rounded border px-2 py-1 text-xs {settings.phosphor === opt.value ? 'bg-primary/20 border-primary/60' : 'border-border/60'}"
                    onclick={() => { settings.phosphor = opt.value; }}
                  >
                    {opt.label}
                  </button>
                {/each}
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
                {#each TOUCH_KEYPAD_OPTIONS as opt (opt.label)}
                  <button
                    type="button"
                    class="rounded border px-2 py-0.5 text-xs {settings.touchKeypadManual === opt.value ? 'bg-primary/20 border-primary/60' : 'border-border/60'}"
                    onclick={() => { settings.touchKeypadManual = opt.value; }}
                  >{opt.label}</button>
                {/each}
              </div>
            </div>
          </TabsContent>

          <TabsContent value="audio" class="mt-0 flex flex-col gap-4">
            <VolumeControl />
          </TabsContent>

          <TabsContent value="about" class="mt-0 flex flex-col gap-3 text-xs text-muted-foreground">
            <a
              href="https://github.com/Zeejfps/cs_chip8_emulator"
              target="_blank"
              rel="noreferrer"
              class="flex w-fit items-center gap-1.5 underline"
            >
              <GithubLogo size={13} />
              Zeejfps/cs_chip8_emulator
            </a>
            <p>
              CHIP-8 emulator written in C# and compiled to WebAssembly. UI built with Svelte 5,
              Tailwind v4, and shadcn-svelte.
            </p>
            <p>
              Bundled ROMs are from the
              <a class="underline" href="https://github.com/JohnEarnest/chip8Archive" target="_blank" rel="noreferrer">chip8Archive</a>
              by John Earnest and contributors (CC0/CC-BY).
            </p>
            <div class="flex flex-col gap-1.5">
              <span class="font-pixel text-[10px] tracking-wider text-muted-foreground/70">Keyboard mapping</span>
              <div class="flex items-center gap-3">
                <div class="flex flex-col gap-0.5 font-mono text-[11px]">
                  {#each [['1','2','3','C'],['4','5','6','D'],['7','8','9','E'],['A','0','B','F']] as row}
                    <div class="flex gap-0.5">
                      {#each row as key}
                        <span class="flex h-6 w-6 items-center justify-center rounded border border-border/50 bg-muted/30">{key}</span>
                      {/each}
                    </div>
                  {/each}
                </div>
                <span class="text-base text-muted-foreground/60">→</span>
                <div class="flex flex-col gap-0.5 font-mono text-[11px]">
                  {#each [['1','2','3','4'],['Q','W','E','R'],['A','S','D','F'],['Z','X','C','V']] as row}
                    <div class="flex gap-0.5">
                      {#each row as key}
                        <span class="flex h-6 w-6 items-center justify-center rounded border border-border/50 bg-muted/30">{key}</span>
                      {/each}
                    </div>
                  {/each}
                </div>
              </div>
            </div>
          </TabsContent>
        </div>
      </Tabs>
    </div>
  </SheetContent>
</Sheet>
