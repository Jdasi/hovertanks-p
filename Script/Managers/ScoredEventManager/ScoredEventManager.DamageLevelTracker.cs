using HoverTanks.Events;
using UnityEngine;

public partial class ScoredEventManager
{
	private class DamageLevelTracker
	{
		private readonly PlayerId _playerId;

		private int _trackedHeal;
		private DamageLevel _highestDamageLevel;
		private bool _hasTakenDamage;
		private bool _isCriticalFromEnemyDamage;
		private float _lastEnemyDamageTime;

		public DamageLevelTracker(PlayerId playerId)
		{
			_playerId = playerId;

			LocalEvents.Subscribe<EntityGarbageCollectedData>(OnEntityGarbageCollected);
			LocalEvents.Subscribe<ServerPawnKilledData>(OnPawnKilled);
			LocalEvents.Subscribe<EntityDamagedData>(OnEntityDamaged);
			LocalEvents.Subscribe<EntityHealedData>(OnEntityHealed);
			LocalEvents.Subscribe<ArcadeLevelClearedData>(OnArcadeMapCleared);
		}

		public void Cleanup()
		{
			LocalEvents.Unsubscribe<EntityGarbageCollectedData>(OnEntityGarbageCollected);
			LocalEvents.Unsubscribe<ServerPawnKilledData>(OnPawnKilled);
			LocalEvents.Unsubscribe<EntityDamagedData>(OnEntityDamaged);
			LocalEvents.Unsubscribe<EntityHealedData>(OnEntityHealed);
			LocalEvents.Unsubscribe<ArcadeLevelClearedData>(OnArcadeMapCleared);
		}

        private void OnEntityGarbageCollected(EntityGarbageCollectedData data)
		{
			// ignore other players
			if (data.Identity.playerId != _playerId)
			{
				return;
			}

			_highestDamageLevel = DamageLevel.None;
			_hasTakenDamage = false;
			_isCriticalFromEnemyDamage = false;
			_lastEnemyDamageTime = 0;
		}

		private void OnPawnKilled(ServerPawnKilledData data)
		{
			// ignore other player deaths
			if (data.Victim.identity.playerId != _playerId)
			{
				return;
			}

			_trackedHeal = 0;
		}

		private void OnEntityDamaged(EntityDamagedData data)
		{
			// ignore other player damage
			if (data.Victim.PlayerId != _playerId)
			{
				return;
			}

			_hasTakenDamage = true;

			// short term grace period for friendly fire
			if (!JHelper.SameTeam(data.Attacker.TeamId, data.Victim.TeamId))
			{
				_lastEnemyDamageTime = Time.time;
			}

			// update highest damage level
			if (data.DamageLevel >= _highestDamageLevel)
			{
				_highestDamageLevel = data.DamageLevel;
			}

			// determine if critical from enemy attacks
			if (data.DamageLevel >= DamageLevel.Critical)
			{
				_isCriticalFromEnemyDamage = data.Element == ElementType.Ram
					|| Time.time - _lastEnemyDamageTime < 4.5f;
			}
		}

		private void OnEntityHealed(EntityHealedData data)
		{
			// ignore other player healing
			if (data.PlayerId != _playerId)
			{
				return;
			}

			// only track orb healing
			if (data.Element != ElementType.HealthOrb)
			{
				return;
			}

			ProcessHealAmount(data.Amount);

			// check if fully healed from having been critical
			if (data.NewDamageLevel <= DamageLevel.None
				&& _highestDamageLevel >= DamageLevel.Critical)
			{
				_highestDamageLevel = DamageLevel.None;
				AwardAccolade(_playerId, AccoladeType.FromTheBrink);
			}

			// update critical from enemy damage status
			if (data.NewDamageLevel < DamageLevel.Critical)
			{
				_isCriticalFromEnemyDamage = false;
			}
		}

		private void ProcessHealAmount(int amount)
		{
			int prevTrackedHeal = _trackedHeal;
			_trackedHeal += amount;

			if (TestAccoladeThreshold(prevTrackedHeal, AccoladeType.QuickRepair))
			{
				return;
			}

			if (TestAccoladeThreshold(prevTrackedHeal, AccoladeType.Scavenger))
			{
				return;
			}

			if (TestAccoladeThreshold(prevTrackedHeal, AccoladeType.ComebackKid))
			{
				return;
			}
		}

		private bool TestAccoladeThreshold(int prevTrackedHeal, AccoladeType type)
		{
			int threshold = instance.GetAccoladeThreshold(type);

			if (prevTrackedHeal < threshold
				&& _trackedHeal >= threshold)
			{
				AwardAccolade(_playerId, type);
				return true;
			}

			return false;
		}

		private void OnArcadeMapCleared(ArcadeLevelClearedData data)
        {
			if (data.AlivePlayers == null)
			{
				return;
			}

			foreach (var player in data.AlivePlayers)
			{
				if (player.identity.playerId != _playerId)
				{
					continue;
				}

				if (!_hasTakenDamage)
				{
					AwardAccolade(_playerId, AccoladeType.Perfectionist);
				}
				else if (_isCriticalFromEnemyDamage)
				{
					AwardAccolade(_playerId, AccoladeType.NotEvenClose);
				}
			}
        }
	}
}
