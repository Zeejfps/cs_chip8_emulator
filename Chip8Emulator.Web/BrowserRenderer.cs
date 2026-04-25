using System.Runtime.InteropServices.JavaScript;
using Chip8Emulator.Core;

namespace Chip8Emulator.Web;

internal sealed partial class BrowserRenderer : IRenderer
{
    public void Render(IReadOnlyDisplay display) => RenderJs();

    [JSImport("display.render", "main.js")]
    private static partial void RenderJs();
}
