import { defineConfig, type Plugin } from 'vite';
import { svelte } from '@sveltejs/vite-plugin-svelte';
import tailwindcss from '@tailwindcss/vite';
import path from 'node:path';
import fs from 'node:fs';

const WASM_PREFIXES = ['/_framework/', '/_content/'];

function resolveWasmBinDir(): string | null {
  const envDir = process.env.WASM_OUTPUT_DIR;
  if (envDir) return path.resolve(__dirname, envDir);

  const binRoot = path.resolve(__dirname, '../bin');
  if (!fs.existsSync(binRoot)) return null;

  const candidates: { dir: string; mtime: number }[] = [];
  for (const config of fs.readdirSync(binRoot)) {
    const configDir = path.join(binRoot, config);
    if (!fs.statSync(configDir).isDirectory()) continue;
    for (const tfm of fs.readdirSync(configDir)) {
      if (!/^net\d/.test(tfm)) continue;
      const wwwroot = path.join(configDir, tfm, 'wwwroot');
      if (fs.existsSync(wwwroot) && fs.statSync(wwwroot).isDirectory()) {
        candidates.push({ dir: wwwroot, mtime: fs.statSync(wwwroot).mtimeMs });
      }
    }
  }
  if (candidates.length === 0) return null;
  candidates.sort((a, b) => b.mtime - a.mtime);
  return candidates[0].dir;
}

const WASM_BIN_DIR = resolveWasmBinDir();

const MIME_TYPES: Record<string, string> = {
  '.js': 'application/javascript',
  '.mjs': 'application/javascript',
  '.wasm': 'application/wasm',
  '.json': 'application/json',
  '.map': 'application/json',
  '.html': 'text/html',
  '.css': 'text/css',
  '.svg': 'image/svg+xml',
  '.png': 'image/png',
  '.ico': 'image/x-icon',
  '.woff': 'font/woff',
  '.woff2': 'font/woff2',
};

function serveWasmAssets(): Plugin {
  return {
    name: 'serve-wasm-assets',
    configureServer(server) {
      if (!WASM_BIN_DIR) {
        server.config.logger.warn(
          `[serve-wasm-assets] No wasm output directory found under ../bin/*/net*/wwwroot. Build the C# project first (dotnet build Chip8Emulator.Web.csproj -p:SKIP_FRONTEND_BUILD=true) or set WASM_OUTPUT_DIR.`,
        );
      } else {
        server.config.logger.info(`[serve-wasm-assets] Serving wasm assets from ${WASM_BIN_DIR}`);
      }
      server.middlewares.use((req, res, next) => {
        if (!WASM_BIN_DIR) return next();
        const url = req.url;
        if (!url) return next();
        const prefix = WASM_PREFIXES.find((p) => url.startsWith(p));
        if (!prefix) return next();

        const relative = url.split('?')[0];
        const filePath = path.join(WASM_BIN_DIR, relative);
        if (!filePath.startsWith(WASM_BIN_DIR)) return next();

        fs.stat(filePath, (err, stat) => {
          if (err || !stat.isFile()) {
            res.statusCode = 404;
            res.end();
            return;
          }
          const ext = path.extname(filePath).toLowerCase();
          const mime = MIME_TYPES[ext] ?? 'application/octet-stream';
          res.setHeader('Content-Type', mime);
          res.setHeader('Content-Length', stat.size);
          fs.createReadStream(filePath).pipe(res);
        });
      });
    },
  };
}

export default defineConfig({
  base: './',
  define: {
    __BUILD_ID__: JSON.stringify(Date.now().toString(36)),
    __APP_VERSION__: JSON.stringify(process.env.APP_VERSION || 'dev'),
  },
  plugins: [tailwindcss(), svelte(), serveWasmAssets()],
  resolve: {
    alias: { $lib: path.resolve(__dirname, './src/lib') },
  },
  server: {
    open: true,
  },
  build: {
    outDir: '../wwwroot',
    emptyOutDir: false,
    assetsDir: 'assets',
    sourcemap: true,
    target: 'es2022',
  },
});
