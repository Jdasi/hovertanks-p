using HoverTanks.Networking;
using System;
using System.Collections.Generic;

namespace HoverTanks.Events
{
	public static class NetworkEvents
	{
		private static EventManager<NetworkMessage> _eventManager;

		static NetworkEvents()
		{
			_eventManager = new EventManager<NetworkMessage>();
		}

		public static void Subscribe<TData>(EventManager<NetworkMessage>.Callback<TData> callback) where TData : NetworkMessage
		{
			_eventManager.Subscribe(callback);
		}

		public static void Unsubscribe<TData>(EventManager<NetworkMessage>.Callback<TData> callback) where TData : NetworkMessage
		{
			_eventManager.Unsubscribe(callback);
		}

		public static void Invoke<TData>(TData msg) where TData : NetworkMessage
		{
			_eventManager.Invoke(msg);
		}
	}

	public static class LocalEvents
	{
		private static EventManager<EventData> _eventManager;

		static LocalEvents()
		{
			_eventManager = new EventManager<EventData>();
		}

		public static void Subscribe<TData>(EventManager<EventData>.Callback<TData> callback) where TData : EventData
		{
			_eventManager.Subscribe(callback);
		}

		public static void Unsubscribe<TData>(EventManager<EventData>.Callback<TData> callback) where TData : EventData
		{
			_eventManager.Unsubscribe(callback);
		}

		public static void Invoke<TData>(TData msg) where TData : EventData
		{
			_eventManager.Invoke(msg);
		}
	}

	public partial class EventManager<TBaseData>
	{
		public delegate void Callback<TData>(TData data) where TData : TBaseData;

		private Dictionary<Type, ICallbackHandler> _subscriptionDict;

		public EventManager()
		{
			_subscriptionDict = new Dictionary<Type, ICallbackHandler>();
		}

		public void Subscribe<TData>(Callback<TData> callback) where TData : TBaseData
		{
			if (callback == null)
			{
				return;
			}

			if (!GetHandler<TData>(out var handler))
			{
				handler = new CallbackHandler<TData>();
				_subscriptionDict[typeof(TData)] = handler;
			}

			handler.AddCallback(callback);
		}

		public void Unsubscribe<TData>(Callback<TData> callback) where TData : TBaseData
		{
			if (callback == null)
			{
				return;
			}

			if (!GetHandler<TData>(out var handler))
			{
				return;
			}

			handler.RemoveCallback(callback);

			if (handler.Count == 0)
			{
				_subscriptionDict.Remove(typeof(TData));
			}
		}

		public void Invoke<TData>(TData data) where TData : TBaseData
		{
			if (data == null)
			{
				return;
			}

			if (!GetHandler<TData>(out var handler))
			{
				return;
			}

			handler.Invoke(data);
		}

		private bool GetHandler<TData>(out CallbackHandler<TData> handler) where TData : TBaseData
		{
			handler = null;

			if (!_subscriptionDict.TryGetValue(typeof(TData), out var baseHandler))
			{
				return false;
			}

			handler = baseHandler as CallbackHandler<TData>;
			return true;
		}
	}
}
