using HoverTanks.Events;
using HoverTanks.Networking;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace HoverTanks.Entities
{
    [RequireComponent(typeof(NetworkEntity))]
	public partial class LifeForce : MonoBehaviour
	{
		public NetworkIdentity identity => _entity.identity;
		public int MaxHealth => _maxHealth;
		public bool IsAlive => _health > 0;
		public bool IsOverheating => _currentHeat * 100 >= (int)HeatLevel.Overheating;
		public StampedDamageInfo LastExternalDamageInfo { get; private set; }
		public StampedHeatDamageInfo LastExternalHeatDamageInfo { get; private set; }

		public Action<ElementData> OnDamage;
		public Action<ElementData> OnHeal;
		public Action<DeathEventData> OnDeathServer;
		public Action<HealthChangedData> OnHealthChanged;
		public Action<HeatLevelChangedData> OnHeatLevelChanged;

		public Action OnDamageBasic;
		public Action OnHealBasic;
		public Action OnDeathBasic;
		public Action OnOverheatTick;

		[SerializeField] int _maxHealth;
		[SerializeField] bool _godMode;
		[SerializeField] List<ElementFactorData> _elementFactors;
		[SerializeField] GameObject _deathEffectPrefab;

		private const float MAX_HEAT_VALUE = 1f;
		private const float DEFAULT_COOL_RATE = 1f;
		private const float DEFAULT_TIME_BEFORE_COOL = 0.5f;
		private const float OVERHEAT_DAMAGE_INTERVAL = 1f;
		private const int OVERHEAT_DAMAGE_TICK = 3;

		private NetworkEntity _entity;

		private int _health;
		private float _currentHeat;
		private float _heatReduceCooldown;
		private float _heatDamageCooldown;
		private FactoredFloat _heatDamageFactor;

		public Configuration Configure()
		{
			return new Configuration(this);
		}

		public bool HasExplosionDamageOnDeath
		{
			get
			{
				if (_deathEffectPrefab == null)
				{
					return false;
				}

				var explosion = _deathEffectPrefab.GetComponent<Explosion>();

				if (explosion == null)
				{
					return false;
				}

				return explosion.HasDamageThresholds;
			}
		}

		public float GetHealthPercent()
		{
			return (float)_health / _maxHealth;
		}

		public float GetDamagePercent()
		{
			return 1 - GetHealthPercent();
		}

		public float GetHeatPercent()
		{
			return _currentHeat / 1;
		}

		public DamageLevel GetDamageLevel()
		{
			return JHelper.PercentToDamageLevel(GetDamagePercent());
		}

		public HeatLevel GetHeatLevel()
		{
			return JHelper.PercentToHeatLevel(GetHeatPercent());
		}

		public void Damage(DamageInfo info)
		{
			if (!Server.IsActive)
			{
				return;
			}

			if (_godMode)
			{
				return;
			}

			if (info.Damage.Amount <= 0)
			{
				return;
			}

			ProcessElementFactor(info.Damage.Amount, info.Damage.Element, info.Damage.Flags, out int actualDamage);

			// check if element was negated
			if (actualDamage <= 0)
			{
				return;
			}

			// update last external damage info
			if (info.Inflictor.PlayerId != identity.playerId)
			{
				LastExternalDamageInfo.Refresh(info.Inflictor);
			}

			// avoid overkill
			actualDamage = Math.Min(_health, actualDamage);

			int prevHealth = _health;
			_health -= actualDamage;

			using (var data = new EntityDamagedData()
			{
				Attacker = info.Inflictor,
				Victim = identity,
				Element = info.Damage.Element,
				Amount = actualDamage,
				DamageLevel = GetDamageLevel(),
				Position = transform.position,
			})
			{
				LocalEvents.Invoke(data);
			}

			EntityDamageEvents(new ElementData(info.Damage.Element, actualDamage, info.Damage.Flags));

			// handle death
			if (prevHealth > 0 && !IsAlive)
			{
				OnDeathServer?.Invoke(new DeathEventData()
				{
					Inflictor = info.Inflictor,
					ProjectileStats = info.ProjectileStats,
					Element = info.Damage.Element,
				});

				EntityDeathEvents(out var deathEffect);
				InitDeathEffectExplosion(deathEffect, info.Inflictor, info.ProjectileStats);
			}
		}

		public void Heal(HealInfo info)
		{
			if (!Server.IsActive)
			{
				return;
			}

			if (info.Amount <= 0)
			{
				return;
			}

			var elementFlags = info.IsAoe ? ElementFlags.IsAoe : ElementFlags.None;
			ProcessElementFactor(info.Amount, info.Element, elementFlags, out int actualHeal);

			// check if element was negated
			if (actualHeal <= 0)
			{
				return;
			}

			int missingHealth = _maxHealth - _health;

			// handle overhealing
			if (info.Amount > missingHealth)
			{
				actualHeal = missingHealth;
			}

			_health = Mathf.Min(_health + actualHeal, _maxHealth);

			using (var data = new EntityHealedData()
			{
				PlayerId = identity.playerId,
				Element = info.Element,
				Amount = actualHeal,
				NewDamageLevel = GetDamageLevel(),
			})
			{
				LocalEvents.Invoke(data);
			}

			EntityHealEvents(new ElementData(info.Element, actualHeal, elementFlags));
		}

		/// <summary>
		/// Deal heat damage to a target. Changes in heat level will be sent over the network.
		/// </summary>
		public void HeatDamage(HeatDamageInfo data)
		{
			if (!Server.IsActive)
			{
				return;
			}

			float actualAmount = data.Amount * _heatDamageFactor.Value;

			if (actualAmount == 0)
			{
				return;
			}

			// update last external damage info
			if (data.Inflictor.PlayerId != identity.playerId)
			{
				LastExternalHeatDamageInfo.Refresh(data.Inflictor, data.TimeBeforeCool);
			}

			HeatLevel prevHeatLevel = JHelper.PercentToHeatLevel(_currentHeat);

			_currentHeat = Mathf.Min(_currentHeat + actualAmount, MAX_HEAT_VALUE);
			_heatReduceCooldown = Math.Max(_heatReduceCooldown, data.TimeBeforeCool);

			HeatLevel newHeatLevel = JHelper.PercentToHeatLevel(_currentHeat);

			if (newHeatLevel != prevHeatLevel)
			{
				bool justIncreasedToCritical = prevHeatLevel < HeatLevel.Critical
					&& newHeatLevel >= HeatLevel.Critical;

				EntityHeatLevelChangedEvents(new HeatLevelChangedData(newHeatLevel, justIncreasedToCritical));
			}
		}

		public void Kill(IdentityInfo inflictor = default, ElementType element = ElementType.Invalid, ProjectileStats projectileStats = null)
		{
			if (!Server.IsActive)
			{
				return;
			}

			if (!IsAlive)
			{
				return;
			}

			OnDeathServer?.Invoke(new DeathEventData()
			{
				Inflictor = inflictor,
				Element = element,
			});

			EntityDeathEvents(out var deathEffect);
			InitDeathEffectExplosion(deathEffect, inflictor, projectileStats);
		}

		/// <summary>
		/// Subscribe on Awake or we will miss insta-death messages.
		/// </summary>
		private void Awake()
		{
			_entity = GetComponent<NetworkEntity>();
			_health = _maxHealth;
			_heatDamageFactor.Base = 1;

			LastExternalDamageInfo = new StampedDamageInfo();
			LastExternalHeatDamageInfo = new StampedHeatDamageInfo();

			NetworkEvents.Subscribe<EntityDamageMsg>(OnEntityDamageMsg);
			NetworkEvents.Subscribe<EntityHealMsg>(OnEntityHealMsg);
			NetworkEvents.Subscribe<EntityHeatLevelChangedMsg>(OnEntityHeatLevelChangedMsg);
			NetworkEvents.Subscribe<KillEntityMsg>(OnKillEntityMsg);
		}

		protected void OnDestroy()
		{
			NetworkEvents.Unsubscribe<EntityDamageMsg>(OnEntityDamageMsg);
			NetworkEvents.Unsubscribe<EntityHealMsg>(OnEntityHealMsg);
			NetworkEvents.Unsubscribe<EntityHeatLevelChangedMsg>(OnEntityHeatLevelChangedMsg);
			NetworkEvents.Unsubscribe<KillEntityMsg>(OnKillEntityMsg);
		}

		private void Update()
		{
			if (Server.IsActive)
			{
				HandleHeatReduction();
			}
		}

		private void HandleHeatReduction()
		{
			// not yet cooling down
			if (_heatReduceCooldown > 0)
			{
				// overheating
				if (IsOverheating
					&& _heatReduceCooldown >= DEFAULT_TIME_BEFORE_COOL)
				{
					_heatDamageCooldown -= Time.deltaTime;

					// suffer overheat damage tick
					if (_heatDamageCooldown <= 0)
					{
						// determine overheat inflictor
						var inflictor = LastExternalHeatDamageInfo.HasExpired
							? identity
							: LastExternalHeatDamageInfo.Inflictor;

						Damage(new DamageInfo(inflictor, new ElementData(ElementType.Overheat, OVERHEAT_DAMAGE_TICK)));

						_heatDamageCooldown = OVERHEAT_DAMAGE_INTERVAL;
					}
				}

				_heatReduceCooldown -= Time.deltaTime;
			}
			// cooling down
			else
			{
				HeatLevel prevHeatLevel = JHelper.PercentToHeatLevel(_currentHeat);

				_currentHeat = Mathf.Max(_currentHeat - (DEFAULT_COOL_RATE * Time.deltaTime), 0);

				if (!IsOverheating)
				{
					_heatDamageCooldown = OVERHEAT_DAMAGE_INTERVAL;
				}

				HeatLevel newHeatLevel = JHelper.PercentToHeatLevel(_currentHeat);

				if (newHeatLevel != prevHeatLevel)
				{
					EntityHeatLevelChangedEvents(new HeatLevelChangedData(newHeatLevel, false));
				}
			}
		}

		private void OnEntityDamageMsg(EntityDamageMsg msg)
		{
			if (identity.entityId != msg.EntityId)
			{
				return;
			}

			_health -= msg.Amount;

			EntityDamageEvents(new ElementData(msg.Element, msg.Amount, msg.Flags));
		}

		private void OnEntityHealMsg(EntityHealMsg msg)
		{
			if (identity.entityId != msg.EntityId)
			{
				return;
			}

			_health += msg.Amount;

			var elementFlags = msg.IsAoe ? ElementFlags.IsAoe : ElementFlags.None;
			EntityHealEvents(new ElementData(msg.Element, msg.Amount, elementFlags));
		}

		private void OnEntityHeatLevelChangedMsg(EntityHeatLevelChangedMsg msg)
		{
			if (identity.entityId != msg.EntityId)
			{
				return;
			}

			EntityHeatLevelChangedEvents(new HeatLevelChangedData(msg.Level, msg.JustIncreasedToCritical));
		}

		private void OnKillEntityMsg(KillEntityMsg msg)
		{
			if (identity.entityId != msg.EntityId)
			{
				return;
			}

			EntityDeathEvents(out _);
		}

		private void EntityDamageEvents(ElementData data)
		{
			OnDamage?.Invoke(data);
			OnDamageBasic?.Invoke();
			OnHealthChanged?.Invoke(new HealthChangedData(GetHealthPercent(), GetDamageLevel(), true));

			if (Server.IsActive)
			{
				using (var sendMsg = new EntityDamageMsg()
				{
					EntityId = identity.entityId,
					Element = data.Element,
					Amount = data.Amount,
					Flags = data.Flags,
				})
				{
					ServerSend.ToAllExceptHost(sendMsg);
				}
			}
		}

		private void EntityHealEvents(ElementData data)
		{
			OnHeal?.Invoke(data);
			OnHealBasic?.Invoke();
			OnHealthChanged?.Invoke(new HealthChangedData(GetHealthPercent(), GetDamageLevel(), false));

			if (Server.IsActive)
			{
				using (var sendMsg = new EntityHealMsg()
				{
					EntityId = identity.entityId,
					Element = data.Element,
					Amount = (short)data.Amount,
				})
				{
					ServerSend.ToAllExceptHost(sendMsg);
				}
			}
		}

		private void EntityHeatLevelChangedEvents(HeatLevelChangedData data)
		{
			OnHeatLevelChanged?.Invoke(data);

			if (Server.IsActive)
			{
				using (var sendMsg = new EntityHeatLevelChangedMsg()
				{
					EntityId = identity.entityId,
					Level = data.Level,
					JustIncreasedToCritical = data.JustIncreasedToCritical,
				})
				{
					ServerSend.ToAllExceptHost(sendMsg);
				}
			}
		}

		private void EntityDeathEvents(out GameObject deathEffect)
		{
			_health = 0;
			deathEffect = null;

			if (_deathEffectPrefab != null)
			{
				deathEffect = Instantiate(_deathEffectPrefab, transform.position, transform.rotation);
				DebrisManager.Register(deathEffect);
			}

			OnDeathBasic?.Invoke();

			if (Server.IsActive)
			{
				using (var sendMsg = new KillEntityMsg()
				{
					EntityId = identity.entityId,
				})
				{
					ServerSend.ToAllExceptHost(sendMsg);
				}
			}
		}

		private void InitDeathEffectExplosion(GameObject deathEffect, IdentityInfo inflictor, ProjectileStats projectileStats)
		{
			if (deathEffect == null)
			{
				return;
			}

			var explosion = deathEffect.GetComponent<Explosion>();

			if (explosion == null)
			{
				return;
			}

			explosion.Init(inflictor, projectileStats);
		}

		private void ProcessElementFactor(int value, ElementType damageType, ElementFlags flags, out int modifiedValue)
		{
			modifiedValue = value;

			if (!GetElementFactorData(damageType, out var data))
			{
				return;
			}

			float workingValue = modifiedValue;
			workingValue *= Bit.IsSet((int)flags, (int)ElementFlags.IsAoe) ? data.AOE : data.Direct;

			// round up
			modifiedValue = (int)Math.Ceiling(workingValue);
		}

		private bool GetElementFactorData(ElementType type, out ElementFactorData data)
		{
			data = null;

			if (_elementFactors == null
				|| _elementFactors.Count == 0)
			{
				return false;
			}

			for (int i = 0; i < _elementFactors.Count; ++i)
			{
				var elem = _elementFactors[i];

				if (elem.Type != type)
				{
					continue;
				}

				data = elem;
				return true;
			}

			return false;
		}

		private void RemoveElementFactorData(ElementType type)
		{
			if (_elementFactors == null
				|| _elementFactors.Count == 0)
			{
				return;
			}

			for (int i = 0; i < _elementFactors.Count; ++i)
			{
				if (_elementFactors[i].Type != type)
				{
					continue;
				}

				_elementFactors.RemoveAt(i);
				return;
			}
		}
	}
}
