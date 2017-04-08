using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace FakeServer.WebSockets
{
    public interface IMessageBus
    {
        void Publish<T>(string topic, T message);

        void Subscribe<T>(string topic, Action<T> handler);
    }

    public class MessageBus : IMessageBus
    {
        public ConcurrentDictionary<string, List<dynamic>> _subscriptions = new ConcurrentDictionary<string, List<dynamic>>();

        public void Publish<T>(string topic, T message)
        {
            if (_subscriptions.ContainsKey(topic))
                _subscriptions[topic].ForEach(a => a(message));
        }

        public void Subscribe<T>(string topic, Action<T> handler)
        {
            if (!_subscriptions.ContainsKey(topic))
                _subscriptions.TryAdd(topic, new List<dynamic>());

            _subscriptions[topic].Add(handler);
        }
    }
}