import { defineConfig } from 'vite';
import { svelte } from '@sveltejs/vite-plugin-svelte';
import tailwindcss from '@tailwindcss/vite';
import path from 'node:path';

export default defineConfig({
  base: './',
  plugins: [tailwindcss(), svelte()],
  resolve: {
    alias: { $lib: path.resolve(__dirname, './src/lib') },
  },
  build: {
    outDir: '../wwwroot',
    emptyOutDir: false,
    assetsDir: 'assets',
    sourcemap: true,
    target: 'es2022',
  },
});
