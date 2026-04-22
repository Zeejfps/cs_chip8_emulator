<script lang="ts">
  import { getEmuContext } from '$lib/context.js';
  import { emulator } from '$lib/stores/emulator.svelte.js';
  import { hex2, hex3, hex4 } from '$lib/format.js';
  import Disasm from './Disasm.svelte';

  const { api } = getEmuContext();

  const REG_LABELS = [
    'V0',
    'V1',
    'V2',
    'V3',
    'V4',
    'V5',
    'V6',
    'V7',
    'V8',
    'V9',
    'VA',
    'VB',
    'VC',
    'VD',
    'VE',
    'VF',
  ];

  interface Snapshot {
    registers: Uint8Array;
    stack: Int32Array | number[];
    stackPointer: number;
    indexRegister: number;
    delayTimer: number;
    soundTimer: number;
    pc: number;
  }

  const snap = $derived.by<Snapshot | null>(() => {
    const pc = emulator.pc;
    if (!emulator.lastRomName) return null;
    return {
      registers: api.GetVRegisters(),
      stack: api.GetStack(),
      stackPointer: api.GetStackPointer(),
      indexRegister: api.GetIndexRegister(),
      delayTimer: api.GetDelayTimer(),
      soundTimer: api.GetSoundTimer(),
      pc,
    };
  });
</script>

<div class="font-pixel border-border/60 overflow-hidden rounded-md border text-[12px] tabular-nums">
  {#if snap === null}
    <div class="text-muted-foreground p-3">Load a ROM to view machine state.</div>
  {:else}
    <div class="flex">
      <!-- Stack panel -->
      <div class="border-border/60 flex flex-col border-r p-3">
        <span class="text-muted-foreground">STACK</span>
        {#each { length: 14 } as _, i}
          <span class={i === snap.stackPointer ? 'phosphor-text' : 'text-muted-foreground'}>
            {hex3(snap.stack[i] ?? 0)}
          </span>
        {/each}
      </div>

      <!-- Right panel -->
      <div class="flex min-w-0 flex-1 flex-col p-3">
        <div class="text-muted-foreground grid grid-cols-16 gap-x-1">
          {#each REG_LABELS as label}
            <span>{label}</span>
          {/each}
        </div>
        <div class="phosphor-text grid grid-cols-16 gap-x-1">
          {#each snap.registers as v}
            <span>{hex2(v)}</span>
          {/each}
        </div>

        <span>&nbsp;</span>

        <span>
          <span class="text-muted-foreground">PC = </span>
          <span class="phosphor-text">{hex4(snap.pc)}</span>
        </span>
        <span>
          <span class="text-muted-foreground">DT = </span>
          <span class="phosphor-text">{snap.delayTimer}</span>
        </span>
        <span>
          <span class="text-muted-foreground">ST = </span>
          <span class="phosphor-text">{snap.soundTimer}</span>
        </span>
        <span>
          <span class="text-muted-foreground">SP = </span>
          <span class="phosphor-text">{snap.stackPointer}</span>
        </span>
        <span>
          <span class="text-muted-foreground">I = </span>
          <span class="phosphor-text">{hex4(snap.indexRegister)}</span>
        </span>

        <span>&nbsp;</span>

        <Disasm />
      </div>
    </div>
  {/if}
</div>

<style>
  .grid-cols-16 {
    grid-template-columns: repeat(16, minmax(0, 1fr));
  }
</style>
