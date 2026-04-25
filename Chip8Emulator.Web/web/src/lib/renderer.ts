let paintFn: (() => void) | null = null;

export const renderer = {
  register(fn: () => void): () => void {
    paintFn = fn;
    return () => {
      if (paintFn === fn) paintFn = null;
    };
  },
  paint(): void {
    paintFn?.();
  },
};
