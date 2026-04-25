namespace Chip8Emulator.Core;

public sealed class NullRenderer : IRenderer
{
    public void Render(IReadOnlyDisplay display) { }
}
