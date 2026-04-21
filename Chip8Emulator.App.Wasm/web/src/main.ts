import { dotnet } from './_framework/dotnet.js';
import { Audio } from './audio.js';
import { Disassembler } from './disasm.js';
import { installKeyListeners } from './input.js';
import { initQuirks } from './quirks.js';
import { AppUi } from './ui.js';

const statusEl = document.getElementById('status');
if (statusEl) statusEl.textContent = 'Loading runtime...';

const runtime = await dotnet.withApplicationArgumentsFromQuery().create();

const audio = new Audio();

runtime.setModuleImports('main.js', {
  audio: {
    beepTick: () => audio.beepTick(),
  },
});

const config = runtime.getConfig();
const exports = await runtime.getAssemblyExports(config.mainAssemblyName);
const api = exports.Interop;

api.Init();

const disasmEl = document.getElementById('disasm');
if (!disasmEl) throw new Error('Missing required element: #disasm');
const disassembler = new Disassembler(api, disasmEl);

const ui = new AppUi(api, runtime, audio, disassembler);

installKeyListeners(api);
initQuirks(api);

ui.setStatus('Ready. Load a ROM to begin.');
