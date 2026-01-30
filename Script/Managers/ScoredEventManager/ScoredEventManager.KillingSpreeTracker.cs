using HoverTanks.Events;
using UnityEngine;

public partial class ScoredEventManager
{
    private class KillingSpreeTracker
	{
		private const float MICRO_SPREE_DURATION = 4.5f;

		private readonly PlayerId _playerId;

		private int _numMacroKills;
		private int _numMicroKills;
		private float _microSpreeEndTimestamp;

		private AccoladeType _microType;
		private AccoladeType _macroType;

		public KillingSpreeTracker(PlayerId playerId)
		{
			_playerId = playerId;

			LocalEvents.Subscribe<ServerPawnKilledData>(OnPawnKilled);
		}

		public void Cleanup()
		{
			LocalEvents.Unsubscribe<ServerPawnKilledData>(OnPawnKilled);
		}

		private void OnPawnKilled(ServerPawnKilledData data)
		{
			// reset if this player killed
			if (data.Victim.identity.playerId == _playerId)
			{
				Reset();
				return;
			}

			// ignore kills of other players
			if (data.Killer.PlayerId != _playerId)
			{
				return;
			}

			// ignore team kills
			if (JHelper.SameTeam(data.Victim.identity.teamId, data.Killer.TeamId))
			{
				return;
			}

			HandleKill();
		}

		private void HandleKill()
		{
			// handle ending micro spree
			if (Time.time >= _microSpreeEndTimestamp)
			{
				EndMicroSpree();
			}

			// increase micro kills
			var prevMicroType = GetMicroSpreeForCount(_numMicroKills);
			_microType = GetMicroSpreeForCount(++_numMicroKills);

			// handle change in mciro spree
			if (_microType != prevMicroType)
			{
				AwardAccolade(_playerId, _microType);
			}

			// keep micro spree going
			_microSpreeEndTimestamp = Time.time + MICRO_SPREE_DURATION;

			// increase macro kills
			var prevMacroType = GetMacroSpreeForCount(_numMacroKills);
			_macroType = GetMacroSpreeForCount(++_numMacroKills);

			// handle change in macro spree
			if (_macroType != prevMacroType)
			{
				AwardAccolade(_playerId, _macroType);
			}
		}

		private void Reset()
		{
			_numMacroKills = 0;
			_macroType = AccoladeType.None;

			EndMicroSpree();
		}

		private void EndMicroSpree()
		{
			_numMicroKills = 0;
			_microSpreeEndTimestamp = 0;
			_microType = AccoladeType.None;
		}

		private AccoladeType GetMicroSpreeForCount(int count)
		{
			if (count >= instance.GetAccoladeThreshold(AccoladeType.KillCrazy))
			{
				return AccoladeType.KillCrazy;
			}
			if (count >= instance.GetAccoladeThreshold(AccoladeType.Overkill))
			{
				return AccoladeType.Overkill;
			}
			else if (count >= instance.GetAccoladeThreshold(AccoladeType.UltraKill))
			{
				return AccoladeType.UltraKill;
			}
			else if (count >= instance.GetAccoladeThreshold(AccoladeType.MegaKill))
			{
				return AccoladeType.MegaKill;
			}
			else if (count >= instance.GetAccoladeThreshold(AccoladeType.TripleKill))
			{
				return AccoladeType.TripleKill;
			}
			else if (count >= instance.GetAccoladeThreshold(AccoladeType.DoubleKill))
			{
				return AccoladeType.DoubleKill;
			}

			return AccoladeType.None;
		}

		private AccoladeType GetMacroSpreeForCount(int count)
		{
			if (count >= instance.GetAccoladeThreshold(AccoladeType.TheOne))
			{
				return AccoladeType.TheOne;
			}
			else if (count >= instance.GetAccoladeThreshold(AccoladeType.Omnipotent))
			{
				return AccoladeType.Omnipotent;
			}
			else if (count >= instance.GetAccoladeThreshold(AccoladeType.Godlike))
			{
				return AccoladeType.Godlike;
			}
			else if (count >= instance.GetAccoladeThreshold(AccoladeType.Invincible))
			{
				return AccoladeType.Invincible;
			}
			else if (count >= instance.GetAccoladeThreshold(AccoladeType.Unstoppable))
			{
				return AccoladeType.Unstoppable;
			}
			else if (count >= instance.GetAccoladeThreshold(AccoladeType.Dominating))
			{
				return AccoladeType.Dominating;
			}
			else if (count >= instance.GetAccoladeThreshold(AccoladeType.Formidable))
			{
				return AccoladeType.Formidable;
			}
			else if (count >= instance.GetAccoladeThreshold(AccoladeType.KillingSpree))
			{
				return AccoladeType.KillingSpree;
			}

			return AccoladeType.None;
		}
	}
}
