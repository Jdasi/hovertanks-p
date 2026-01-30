using HoverTanks.Events;
using HoverTanks.Networking;
using UnityEngine;

namespace HoverTanks.UI
{
    public class AccoladeReadoutManager : MonoBehaviour
	{
		[SerializeField] AccoladeReadoutItem[] _readouts;
		[SerializeField] EffectAudioSettings _accoladeAudio;

		private NetworkIdentity _owner;
		private EffectsSource _source;

		private const float MIN_DELAY_BETWEEN_AUDIO = 0.1f;
		private float _nextAudioTimestamp;

		private void Awake()
		{
			_owner = GetComponentInParent<NetworkEntity>().identity;
			_source = AudioManager.CreateEffectsSource(transform);

			NetworkEvents.Subscribe<AccoladeAwardedMsg>(OnAccoladeAwardedMsg);

			if (_readouts != null)
			{
				foreach (var readout in _readouts)
				{
					readout.SetActive(false);
				}
			}
		}

		private void OnDestroy()
		{
			NetworkEvents.Unsubscribe<AccoladeAwardedMsg>(OnAccoladeAwardedMsg);
		}

		private void OnAccoladeAwardedMsg(AccoladeAwardedMsg msg)
		{
			if (_owner.playerId != msg.PlayerId)
			{
				return;
			}

			if (msg.AccoladeType == AccoladeType.None)
			{
				return;
			}

			if (Time.time >= _nextAudioTimestamp)
			{
				_nextAudioTimestamp = Time.time + MIN_DELAY_BETWEEN_AUDIO;
				_source?.Play(_accoladeAudio);
			}

			AccoladeReadoutItem readout = null;

			for (int i = 0; i < _readouts.Length; ++i)
			{
				var possibleReadout = _readouts[i];

				if (possibleReadout.IsActive)
				{
					continue;
				}

				readout = possibleReadout;
				break;
			}

			if (readout == null)
			{
				readout = _readouts[0];
			}

			if (ScoredEventManager.instance == null)
			{
				Log.Error(LogChannel.AccoladeReadoutManager, "OnAccoladeAwardedMsg - ScoredEventManager.instance == null");
				return;
			}

			if (!ScoredEventManager.instance.TryGetAccoladeInfo(msg.AccoladeType, out var info))
			{
				Log.Error(LogChannel.AccoladeReadoutManager, $"OnAccoladeAwardedMsg - no accolade info found for type: {msg.AccoladeType}");
				return;
			}

			readout.Refresh($"{info.Name} +{info.Score}");
		}

		private void Update()
		{
			if (_owner.playerId == PlayerId.Invalid)
			{
				return;
			}

			for (int i = 0; i < _readouts.Length; ++i)
			{
				_readouts[i].Update();
			}
		}
	}
}
