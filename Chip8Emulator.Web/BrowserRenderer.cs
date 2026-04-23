using Chip8Emulator.Core;

namespace Chip8Emulator.Web;

internal sealed class BrowserRenderer : IRenderer
{
    public void Render()
    {
        // No-op: JS reads the pinned pixel buffer from WASM linear memory each rAF tick.
    }
}
