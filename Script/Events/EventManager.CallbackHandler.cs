using System;
using System.Collections.Generic;

namespace HoverTanks.Events
{
	public partial class EventManager<TBaseData>
    {
		private interface ICallbackHandler { }

        private class CallbackHandler<TData> : ICallbackHandler where TData : TBaseData
		{
			private readonly List<Callback<TData>> _callbacks;

			public int Count => _callbacks.Count;

			public CallbackHandler()
			{
				_callbacks = new List<Callback<TData>>();
			}

			public void AddCallback(Callback<TData> callback)
			{
				if (_callbacks.Contains(callback))
				{
					return;
				}

				_callbacks.Add(callback);
			}

			public void RemoveCallback(Callback<TData> callback)
			{
				if (_callbacks.Count == 0)
				{
					return;
				}

				_callbacks.Remove(callback);
			}

			public void Invoke(TData data)
			{
				if (_callbacks.Count == 0)
				{
					return;
				}

				bool isDirty = false;

				// invoke all callbacks
				for (int i = 0; i < _callbacks.Count; ++i)
				{
					var callback = _callbacks[i];

					if (callback == null)
					{
						// list needs cleanup
						isDirty = true;
						continue;
					}

					callback.Invoke(data);
				}

				if (isDirty)
				{
					// cleanup null refs
					_callbacks.RemoveAll(elem => elem == null);
				}
			}
		}
    }
}
