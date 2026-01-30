using HoverTanks.Entities;
using HoverTanks.Events;
using System.Collections.Generic;
using UnityEngine;

public partial class ScoredEventManager
{
    private class DamageHistory
    {
        private readonly struct Record
        {
            public bool HasExpired => Time.time >= _expireTime;

            public readonly IdentityInfo Identity;

            private readonly float _expireTime;

            public Record(IdentityInfo identity)
            {
                Identity = identity;
                _expireTime = Time.time + DATA_EXPIRY_TIME;
            }
        }

        private const float DATA_EXPIRY_TIME = 3.5f;
        private const int MIN_RECORDS_BEFORE_PURGE = 5;

        /// <summary> Records of attackers where EntityId is the victim. </summary>
        private readonly Dictionary<EntityId, List<Record>> _victimRecords;

        /// <summary> Records of victims where EntityId is the attacker. </summary>
        private readonly Dictionary<EntityId, List<Record>> _attackerRecords;

        public DamageHistory()
        {
            _attackerRecords = new Dictionary<EntityId, List<Record>>();
            _victimRecords = new Dictionary<EntityId, List<Record>>();

		    LocalEvents.Subscribe<EntityDamagedData>(OnEntityDamaged);
            LocalEvents.Subscribe<EntityGarbageCollectedData>(OnEntityGarbageCollected);
            LocalEvents.Subscribe<ArcadeLevelClearedData>(OnArcadeMapCleared);
        }

        public void Cleanup()
        {
		    LocalEvents.Unsubscribe<EntityDamagedData>(OnEntityDamaged);
            LocalEvents.Unsubscribe<EntityGarbageCollectedData>(OnEntityGarbageCollected);
            LocalEvents.Unsubscribe<ArcadeLevelClearedData>(OnArcadeMapCleared);
        }

        public bool HasRecentlyDamagedAliveAttackerAlly(EntityId entityId, IdentityInfo attacker)
        {
            if (!_attackerRecords.TryGetValue(entityId, out var records))
            {
                return false;
            }

            for (int i = records.Count - 1; i >= 0; --i)
            {
                var record = records[i];

                if (record.HasExpired)
                {
                    records.RemoveAt(i);
                    continue;
                }

                if (record.Identity.PlayerId == attacker.PlayerId
                    || !JHelper.SameTeam(record.Identity.TeamId, attacker.TeamId))
                {
                    continue;
                }

                if (!EntityManager.IsPawnAlive(record.Identity.EntityId))
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        public bool TryGetAttackerAlliesOfVictim(EntityId entityId, IdentityInfo attacker, out PlayerId[] alliedPlayers)
        {
            alliedPlayers = null;
            var playerIds = new List<PlayerId>();

            if (!_victimRecords.TryGetValue(entityId, out var records))
            {
                return false;
            }

            for (int i = records.Count - 1; i >= 0; --i)
            {
                var record = records[i];

                if (record.HasExpired)
                {
                    records.RemoveAt(i);
                    continue;
                }

                if (playerIds.Contains(record.Identity.PlayerId))
                {
                    continue;
                }

                if (record.Identity.PlayerId == attacker.PlayerId
                    || !JHelper.SameTeam(record.Identity.TeamId, attacker.TeamId))
                {
                    continue;
                }

                if (!EntityManager.IsPawnAlive(record.Identity.EntityId))
                {
                    continue;
                }

                playerIds.Add(record.Identity.PlayerId);
            }

            alliedPlayers = playerIds.ToArray();
            return alliedPlayers.Length > 0;
        }

        private void OnEntityDamaged(EntityDamagedData data)
        {
            // both entities must be pawns
            if (!EntityManager.IsEntityOfType(data.Attacker.EntityId, EntityType.Pawn)
			    || !EntityManager.IsEntityOfType(data.Victim.EntityId, EntityType.Pawn))
		    {
			    return;
		    }

            // ignore friendly fire
            if (JHelper.SameTeam(data.Attacker.TeamId, data.Victim.TeamId))
            {
                return;
            }

            SafeAddRecord(_victimRecords, data.Victim.EntityId, data.Attacker);
            SafeAddRecord(_attackerRecords, data.Attacker.EntityId, data.Victim);
        }

        private void SafeAddRecord(Dictionary<EntityId, List<Record>> dict, EntityId key, IdentityInfo identity)
        {
            if (!dict.TryGetValue(key, out var records))
            {
                dict.Add(key, records = new List<Record>());
            }

            records.Add(new Record(identity));

            // not enough records to consider purge
            if (records.Count < MIN_RECORDS_BEFORE_PURGE)
            {
                return;
            }

            PurgeExpiredRecords(records);

            // still records after purge
            if (records.Count > 0)
            {
                return;
            }
            
            dict.Remove(key);
        }

        private void PurgeExpiredRecords(List<Record> records)
        {
            for (int i = records.Count - 1; i >= 0; --i)
            {
                var record = records[i];

                if (!record.HasExpired)
                {
                    continue;
                }

                records.RemoveAt(i);
            }
        }

        private void OnEntityGarbageCollected(EntityGarbageCollectedData data)
        {
            if (data.EntityType != EntityType.Pawn)
            {
                return;
            }

            _attackerRecords.Remove(data.Identity.entityId);
            _victimRecords.Remove(data.Identity.entityId);
        }

        private void OnArcadeMapCleared(ArcadeLevelClearedData data)
        {
            _attackerRecords.Clear();
            _victimRecords.Clear();
        }
    }
}
