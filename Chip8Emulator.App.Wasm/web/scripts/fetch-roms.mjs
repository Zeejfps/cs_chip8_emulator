#!/usr/bin/env node
// One-shot script: fetch a curated set of CC0 CHIP-8 ROMs from chip8Archive
// (https://github.com/JohnEarnest/chip8Archive) and emit a manifest.
//
// Run: `npm run fetch-roms` from `web/`. Output lands in `web/public/roms/`
// and is committed to the repo — the script is not run during CI or build.

import { mkdir, writeFile } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import path from 'node:path';

const ROMS = [
  { id: 'br8kout',       title: 'BR8KOUT',        author: 'SharpenedSpoon',   license: 'CC0-1.0',   preferredQuirks: 'chip48',
    description: 'Breakout clone.' },
  { id: 'octojam1title', title: 'Octojam 1 Title',author: 'John Earnest',     license: 'CC0-1.0',   preferredQuirks: 'chip48',
    description: 'Octojam intro animation.' },
  { id: 'snake',         title: 'Snake',          author: 'TomR',             license: 'CC0-1.0',   preferredQuirks: 'chip48',
    description: 'Snake.' },
  { id: 'rockto',        title: 'Rockto',         author: 'SupSuper',         license: 'CC0-1.0',   preferredQuirks: 'chip48',
    description: 'Rock, paper, scissors vs. CPU.' },
  { id: 'flightrunner',  title: 'Flight Runner',  author: 'TodPunk',          license: 'CC0-1.0',   preferredQuirks: 'chip48',
    description: 'Side-scrolling avoider.' },
  { id: 'glitchGhost',   title: 'Glitch Ghost',   author: 'LillianV',         license: 'CC0-1.0',   preferredQuirks: 'chip48',
    description: 'Puzzle platformer.' },
  { id: 'danm8ku',       title: 'danm8ku',        author: 'buffis',           license: 'CC0-1.0',   preferredQuirks: 'chip48',
    description: 'Bullet-hell demo.' },
  { id: 'chipquarium',   title: 'Chipquarium',    author: 'Silvervice',       license: 'CC0-1.0',   preferredQuirks: 'chip48',
    description: 'Virtual fish tank.' },
  { id: 'slipperyslope', title: 'Slippery Slope', author: 'John Earnest',     license: 'CC0-1.0',   preferredQuirks: 'chip48',
    description: 'Downhill skier.' },
  { id: 'RPS',           title: 'RPS',            author: 'SystemLogoff',     license: 'CC0-1.0',   preferredQuirks: 'chip48',
    description: 'Rock paper scissors.' },
];

const BASE = 'https://raw.githubusercontent.com/JohnEarnest/chip8Archive/master/roms';
const here = path.dirname(fileURLToPath(import.meta.url));
const outDir = path.resolve(here, '..', 'public', 'roms');
await mkdir(outDir, { recursive: true });

const manifest = [];
for (const rom of ROMS) {
  const url = `${BASE}/${rom.id}.ch8`;
  process.stdout.write(`Fetching ${rom.id}… `);
  const res = await fetch(url);
  if (!res.ok) {
    console.error(`FAILED (${res.status})`);
    continue;
  }
  const bytes = new Uint8Array(await res.arrayBuffer());
  const file = `${rom.id}.ch8`;
  await writeFile(path.join(outDir, file), bytes);
  manifest.push({ ...rom, file });
  console.log(`${bytes.byteLength} bytes`);
}

await writeFile(
  path.join(outDir, 'manifest.json'),
  JSON.stringify(manifest, null, 2) + '\n',
);
console.log(`Wrote manifest with ${manifest.length} entries.`);
