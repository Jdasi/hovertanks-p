using HoverTanks.Events;
using HoverTanks.Networking;
using System.Collections.Generic;

public partial class ScoredEventManager
{
    private class AccoladeSpreeTracker
	{
		private readonly PlayerId _playerId;
		private readonly AccoladeType[] _typesToCount;
		private readonly Dictionary<AccoladeType, int> _accoladeCounts;

		public AccoladeSpreeTracker(PlayerId playerId)
		{
			_playerId = playerId;
			_typesToCount = new AccoladeType[]
			{
				AccoladeType.WallBounce,
				AccoladeType.LongShot,
				AccoladeType.Demolition,
				AccoladeType.DodgeThis,
				AccoladeType.CoveringFire,
			};

			_accoladeCounts = new Dictionary<AccoladeType, int>();

			foreach (var type in _typesToCount)
			{
				_accoladeCounts.Add(type, 0);
			}

			NetworkEvents.Subscribe<AccoladeAwardedMsg>(OnAccoladeAwardedMsg);
			LocalEvents.Subscribe<ServerPawnKilledData>(OnPawnKilled);
		}

		public void Cleanup()
		{
			NetworkEvents.Unsubscribe<AccoladeAwardedMsg>(OnAccoladeAwardedMsg);
			LocalEvents.Unsubscribe<ServerPawnKilledData>(OnPawnKilled);
		}

		private void OnPawnKilled(ServerPawnKilledData data)
		{
			// ignore other player deaths
			if (data.Victim.identity.playerId != _playerId)
			{
				return;
			}

			foreach (var type in _typesToCount)
			{
				_accoladeCounts[type] = 0;
			}
		}

		private void OnAccoladeAwardedMsg(AccoladeAwardedMsg msg)
		{
			if (_playerId != msg.PlayerId)
			{
				return;
			}

			if (!_accoladeCounts.ContainsKey(msg.AccoladeType))
			{
				return;
			}

			int prevCount = _accoladeCounts[msg.AccoladeType];
			int newCount = prevCount + 1;
			_accoladeCounts[msg.AccoladeType] = newCount;

			AccoladeType prevAccolade = GetAccoladeForSpreeCount(msg.AccoladeType, prevCount);
			AccoladeType newAccolade = GetAccoladeForSpreeCount(msg.AccoladeType, newCount);

			if (prevAccolade != newAccolade)
			{
				AwardAccolade(_playerId, newAccolade);
			}
		}

		private AccoladeType GetAccoladeForSpreeCount(AccoladeType accolade, int count)
		{
			switch (accolade)
			{
				case AccoladeType.WallBounce:
				{
					if (count >= instance.GetAccoladeThreshold(AccoladeType.Strategist))
					{
						return AccoladeType.Strategist;
					}
					else if (count >= instance.GetAccoladeThreshold(AccoladeType.Tactician))
					{
						return AccoladeType.Tactician;
					}
					else if (count >= instance.GetAccoladeThreshold(AccoladeType.Planner))
					{
						return AccoladeType.Planner;
					}
				} break;

				case AccoladeType.LongShot:
				{
					if (count >= instance.GetAccoladeThreshold(AccoladeType.WizzBang))
					{
						return AccoladeType.WizzBang;
					}
					else if (count >= instance.GetAccoladeThreshold(AccoladeType.EagleEye))
					{
						return AccoladeType.EagleEye;
					}
					else if (count >= instance.GetAccoladeThreshold(AccoladeType.Sniper))
					{
						return AccoladeType.Sniper;
					}
				} break;

				case AccoladeType.Demolition:
				{
					if (count >= instance.GetAccoladeThreshold(AccoladeType.BeTheBullet))
					{
						return AccoladeType.BeTheBullet;
					}
					else if (count >= instance.GetAccoladeThreshold(AccoladeType.WreckingBall))
					{
						return AccoladeType.WreckingBall;
					}
					else if (count >= instance.GetAccoladeThreshold(AccoladeType.RoadRage))
					{
						return AccoladeType.RoadRage;
					}
				} break;

				case AccoladeType.DodgeThis:
				{
					if (count >= instance.GetAccoladeThreshold(AccoladeType.DownTheBarrel))
					{
						return AccoladeType.DownTheBarrel;
					}
					else if (count >= instance.GetAccoladeThreshold(AccoladeType.SwiftJustice))
					{
						return AccoladeType.SwiftJustice;
					}
					else if (count >= instance.GetAccoladeThreshold(AccoladeType.CloseQuarters))
					{
						return AccoladeType.CloseQuarters;
					}
				} break;

				case AccoladeType.CoveringFire:
				{
					if (count >= instance.GetAccoladeThreshold(AccoladeType.GuardianAngel))
					{
						return AccoladeType.GuardianAngel;
					}
					else if (count >= instance.GetAccoladeThreshold(AccoladeType.Protector))
					{
						return AccoladeType.Protector;
					}
					else if (count >= instance.GetAccoladeThreshold(AccoladeType.Saviour))
					{
						return AccoladeType.Saviour;
					}
				} break;
			}

			return AccoladeType.None;
		}
	}
}
