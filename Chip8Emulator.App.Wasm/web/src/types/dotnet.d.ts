declare module '*/dotnet.js' {
  export interface DotnetBuilder {
    withApplicationArgumentsFromQuery(): DotnetBuilder;
    create(): Promise<DotnetRuntime>;
  }

  export interface DotnetRuntime {
    setModuleImports(name: string, imports: Record<string, unknown>): void;
    getAssemblyExports(assemblyName: string): Promise<{
      Interop: import('../interop.js').InteropExports;
    }>;
    getConfig(): { mainAssemblyName: string };
    localHeapViewU8(): Uint8Array;
  }

  export const dotnet: DotnetBuilder;
}
