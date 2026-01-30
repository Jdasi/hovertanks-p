using System;
using System.Collections.Generic;
using UnityEngine;

namespace HoverTanks.Networking
{
    public class PacketHandler<T>
    {
        private class DelayedItem
        {
            private readonly float _invokeTime;
            private readonly Action<T> _action;
            private readonly T _data;

            public DelayedItem(Action<T> action, T data)
            {
                _invokeTime = Time.time + ((float)GameClient.FakeLatency / 1000);
                _action = action;
                _data = data;
            }

            public bool Invoke()
            {
                if (Time.time < _invokeTime)
                {
                    return false;
                }

                _action.Invoke(_data);
                return true;
            }
        }

        private Dictionary<int, Action<T>> _handlers;
        private List<DelayedItem> _delayedItems;

        public PacketHandler()
        {
            _handlers = new Dictionary<int, Action<T>>();
            _delayedItems = new List<DelayedItem>();
        }

        public PacketHandler<T> AddHandler(MessageId id, Action<T> handler)
        {
            _handlers.Add((int)id, handler);
            return this;
        }

        public void Handle(MessageId id, T data)
        {
            if (GameClient.FakeLatency > 0)
            {
                // simulate artificial latency
                _delayedItems.Add(new DelayedItem(_handlers[(int)id], data));
            }
            else
            {
                // standard processing
                _handlers[(int)id].Invoke(data);
            }
        }

        public void Update()
        {
            for (int i = 0; i < _delayedItems.Count; ++i)
            {
                if (!_delayedItems[i].Invoke())
                {
                    continue;
                }

                _delayedItems.RemoveAt(i--);
            }
        }
    }
}
