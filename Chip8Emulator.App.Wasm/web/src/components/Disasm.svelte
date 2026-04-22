<script lang="ts">
  import { getEmuContext } from '$lib/context.js';
  import { emulator } from '$lib/stores/emulator.svelte.js';
  import { hex4 } from '$lib/format.js';

  const { api } = getEmuContext();

  const WINDOW = 6;

  interface Line {
    pc: number;
    bytes: string;
    text: string;
    current: boolean;
  }

  const lines = $derived.by<Line[]>(() => {
    const pc = emulator.pc;
    if (pc === 0 && !emulator.lastRomName) return [];
    const out: Line[] = [];
    for (let i = 0; i < WINDOW; i++) {
      const addr = (pc + i * 2) & 0xffff;
      const hi = api.GetMemoryByte(addr);
      const lo = api.GetMemoryByte((addr + 1) & 0xffff);
      const ins = ((hi << 8) | lo) & 0xffff;
      out.push({
        pc: addr,
        bytes: hex4(ins),
        text: api.DisassembleInstruction(ins),
        current: i === 0,
      });
    }
    return out;
  });
</script>

<div class="font-pixel flex flex-col text-[12px]">
  {#if lines.length === 0}
    <span class="text-muted-foreground">Load a ROM to view disassembly.</span>
  {:else}
    {#each lines as line (line.pc)}
      <div
        class="grid grid-cols-[4rem_3.5rem_1fr] items-baseline gap-2 {line.current
          ? 'phosphor-text'
          : 'text-muted-foreground'}"
      >
        <span class="tabular-nums">{hex4(line.pc)}</span>
        <span class="tabular-nums">{line.bytes}</span>
        <span class="truncate">{line.text}</span>
      </div>
    {/each}
  {/if}
</div>
