using System.Collections.Concurrent;

namespace FakeServer.WebSockets;

public interface IMessageBus
{
    void Publish<T>(string topic, T message);

    void Subscribe<T>(string topic, Action<T> handler);
}

public class MessageBus : IMessageBus
{
    private readonly ConcurrentDictionary<string, ConcurrentBag<dynamic>> _subscriptions = new();

    public void Publish<T>(string topic, T message)
    {
        if (!_subscriptions.ContainsKey(topic))
            return;

        foreach (var action in _subscriptions[topic])
        {
            // T message should be cloned if it is a reference type, so data can't be changed after new thread is created
            Task.Run(() => { action(message); });
        }
    }

    public void Subscribe<T>(string topic, Action<T> handler)
    {
        if (!_subscriptions.ContainsKey(topic))
            _subscriptions.TryAdd(topic, new ConcurrentBag<dynamic>());

        _subscriptions[topic].Add(handler);
    }
}