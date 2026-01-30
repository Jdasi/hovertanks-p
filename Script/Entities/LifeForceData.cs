using HoverTanks.Networking;
using System;
using UnityEngine;

namespace HoverTanks.Entities
{
	public partial class LifeForce
	{
		[Serializable]
		public class ElementFactorData
		{
			public ElementType Type;
			public FactoredFloat Direct;
			public FactoredFloat AOE;
		}

		public struct DeathEventData
		{
			public IdentityInfo Inflictor;
			public ProjectileStats ProjectileStats;
			public ElementType Element;
		}
	}

    public readonly struct IdentityInfo
	{
		public readonly EntityId EntityId;
		public readonly PlayerId PlayerId;
		public readonly TeamId TeamId;

		public IdentityInfo(EntityId entityId, PlayerId playerId, TeamId teamId)
		{
			EntityId = entityId;
			PlayerId = playerId;
			TeamId = teamId;
		}

		public static implicit operator IdentityInfo(NetworkIdentity identity) => new IdentityInfo(identity.entityId, identity.playerId, identity.teamId);
	}

	public class StampedDamageInfo
	{
		public IdentityInfo Inflictor { get; private set; }
		public bool HasExpired => Time.time >= _expireTimestamp;

		private float _expireTimestamp;

		public StampedDamageInfo()
		{
			Refresh(EntityId.Invalid, PlayerId.Invalid, TeamId.None);
		}

		public void Refresh(EntityId entityId, PlayerId playerId, TeamId teamId)
		{
			Inflictor = new IdentityInfo(entityId, playerId, teamId);
			_expireTimestamp = Time.time + 4;
		}

		public void Refresh(IdentityInfo inflictor)
		{
			Refresh(inflictor.EntityId, inflictor.PlayerId, inflictor.TeamId);
		}
	}

	public class StampedHeatDamageInfo
	{
		public IdentityInfo Inflictor { get; private set; }
		public bool HasExpired => Time.time >= _expireTimestamp;

		private float _expireTimestamp;

		public StampedHeatDamageInfo()
		{
			Refresh(EntityId.Invalid, PlayerId.Invalid, TeamId.None, 0);
		}

		public void Refresh(EntityId entityId, PlayerId playerId, TeamId teamId, float timeBeforeCool)
		{
			Inflictor = new IdentityInfo(entityId, playerId, teamId);
			_expireTimestamp = Time.time + timeBeforeCool;
		}

		public void Refresh(IdentityInfo inflictor, float timeBeforeCool)
		{
			Refresh(inflictor.EntityId, inflictor.PlayerId, inflictor.TeamId, timeBeforeCool);
		}
	}

	public readonly struct HealthChangedData
	{
		public readonly float Percent;
		public readonly DamageLevel Level;
		public readonly bool WasDamage;

		public HealthChangedData(float percent, DamageLevel level, bool wasDamage)
		{
			Percent = percent;
			Level = level;
			WasDamage = wasDamage;
		}
	}

	public readonly struct HeatLevelChangedData
	{
		public readonly HeatLevel Level;
		public readonly bool JustIncreasedToCritical;

		public HeatLevelChangedData(HeatLevel level, bool justIncreasedToCritical)
		{
			Level = level;
			JustIncreasedToCritical = justIncreasedToCritical;
		}
	}

	public readonly struct ElementData
	{
		public readonly ElementType Element;
		public readonly int Amount;
		public readonly ElementFlags Flags;

		public ElementData(ElementType type, int amount, ElementFlags flags = ElementFlags.None)
		{
			Element = type;
			Amount = amount;
			Flags = flags;
		}
	}

	public readonly struct HeatDamageInfo
	{
		public readonly IdentityInfo Inflictor;
		public readonly float Amount;
		public readonly float TimeBeforeCool;

		public HeatDamageInfo(IdentityInfo inflictor, float amount, float timeBeforeCool)
		{
			Inflictor = inflictor;
			Amount = amount;
			TimeBeforeCool = timeBeforeCool;
		}
	}

	public class ProjectileStats
	{
		public readonly int NumWallBounces;
		public readonly float DistanceTravelled;

		public ProjectileStats(int numWallBounces, float distanceTravelled)
		{
			NumWallBounces = numWallBounces;
			DistanceTravelled = distanceTravelled;
		}
	}

	public readonly struct DamageInfo
	{
		public readonly IdentityInfo Inflictor;
		public readonly ElementData Damage;
		public readonly ProjectileStats ProjectileStats;

		public DamageInfo(IdentityInfo inflictor, ElementData damage, ProjectileStats projectileStats = null)
		{
			Inflictor = inflictor;
			Damage = damage;
			ProjectileStats = projectileStats;
		}
	}

	public readonly struct HealInfo
	{
		public readonly int Amount;
		public readonly ElementType Element;
		public readonly bool IsAoe;

		public HealInfo(int amount, ElementType element, bool isAoe = false)
		{
			Amount = amount;
			Element = element;
			IsAoe = isAoe;
		}
	}
}
