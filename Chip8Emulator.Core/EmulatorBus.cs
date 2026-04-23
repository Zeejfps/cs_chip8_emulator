namespace Chip8Emulator.Core;

public sealed class EmulatorBus : IBus
{
    private readonly Dictionary<Type, object> _channels = new();
    
    public IDisposable Subscribe<T>(Action<T> handler) where T : struct
    {
        var messageType = typeof(T);
        MessageChannel<T> typedChannel;
        if (!_channels.TryGetValue(messageType, out var channel))
        {
            typedChannel = new MessageChannel<T>();
            _channels[messageType] = typedChannel;
        }
        else
        {
            typedChannel = (MessageChannel<T>)channel;
        }
        return typedChannel.AddListener(handler);
    }

    public void Publish<T>(T message = default) where T : struct
    {
        var messageType = typeof(T);
        if (!_channels.TryGetValue(messageType, out var channel))
            return;
        ((MessageChannel<T>)channel).Send(message);
    }
}

internal sealed class MessageChannel<T>
{
    private readonly List<Action<T>> _handlers = new();

    public void Send(T message)
    {
        // Indexed loop over a snapshot count so a handler disposing itself mid-invoke
        // (which mutates _handlers) doesn't throw during iteration.
        var count = _handlers.Count;
        for (var i = 0; i < count; i++)
        {
            _handlers[i].Invoke(message);
        }
    }

    public IDisposable AddListener(Action<T> listener)
    {
        _handlers.Add(listener);
        return new Subscription(this, listener);
    }

    private sealed class Subscription : IDisposable
    {
        private readonly MessageChannel<T> _channel;
        private readonly Action<T> _handler;
        private bool _disposed;

        public Subscription(MessageChannel<T> channel, Action<T> handler)
        {
            _channel = channel;
            _handler = handler;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _channel._handlers.Remove(_handler);
            _disposed = true;
        }
    }
}