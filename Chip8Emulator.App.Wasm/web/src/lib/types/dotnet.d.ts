import type { InteropExports } from '../interop.js';

export interface DotnetBuilder {
  withApplicationArgumentsFromQuery(): DotnetBuilder;
  create(): Promise<DotnetRuntime>;
}

export interface DotnetRuntime {
  setModuleImports(name: string, imports: Record<string, unknown>): void;
  getAssemblyExports(assemblyName: string): Promise<{
    Interop: InteropExports;
  }>;
  getConfig(): { mainAssemblyName: string };
  localHeapViewU8(): Uint8Array;
}

declare global {
  // eslint-disable-next-line @typescript-eslint/no-unused-vars
  interface Window {
    __chip8_fullscreen?: () => void;
  }
}
