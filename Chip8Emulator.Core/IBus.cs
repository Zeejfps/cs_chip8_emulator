namespace Chip8Emulator.Core;

public interface IBus
{
    IDisposable Subscribe<T>(Action<T> handler) where T : struct;
    void Publish<T>(T message = default) where T : struct;
}