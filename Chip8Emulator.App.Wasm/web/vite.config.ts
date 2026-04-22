import { defineConfig } from 'vite';

export default defineConfig({
  base: './',
  build: {
    outDir: '../wwwroot',
    emptyOutDir: false,
    assetsDir: 'assets',
    sourcemap: true,
    target: 'es2022',
    rollupOptions: {
      external: [/^\.\/_framework\//],
    },
  },
});
