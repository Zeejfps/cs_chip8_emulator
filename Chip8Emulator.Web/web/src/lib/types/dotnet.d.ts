import type { InteropExports } from '../interop.js';

export interface DotnetBuilder {
  withApplicationArgumentsFromQuery(): DotnetBuilder;
  create(): Promise<DotnetRuntime>;
}

export interface DotnetRuntime {
  setModuleImports(name: string, imports: Record<string, unknown>): void;
  getAssemblyExports(assemblyName: string): Promise<{
    Chip8Emulator: {
      Web: {
        Interop: InteropExports;
      };
    };
  }>;
  getConfig(): { mainAssemblyName: string };
  localHeapViewU8(): Uint8Array;
}
