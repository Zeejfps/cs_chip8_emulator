import type { QuirkFlags } from './quirks.js';

export interface RomEntry {
  id: string;
  title: string;
  author: string;
  license: string;
  file: string;
  preferredQuirks: QuirkFlags;
  preferredIps?: number;
  description?: string;
}

function romsUrl(path: string): string {
  return new URL(`roms/${path}`, document.baseURI).href;
}

export async function loadManifest(): Promise<RomEntry[]> {
  const res = await fetch(romsUrl('manifest.json'));
  if (!res.ok) throw new Error(`Failed to load ROM manifest (${res.status})`);
  return res.json();
}

export async function loadRomBytes(entry: RomEntry): Promise<Uint8Array> {
  const res = await fetch(romsUrl(entry.file));
  if (!res.ok) throw new Error(`Failed to load ROM ${entry.id} (${res.status})`);
  return new Uint8Array(await res.arrayBuffer());
}
