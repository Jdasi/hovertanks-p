using HoverTanks.Events;
using HoverTanks.Networking;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HoverTanks.Entities
{
	[RequireComponent(typeof(BoxCollider))]
	public partial class ProjectileDirect : Projectile
	{
		private enum BounceResult
		{
			BounceFail,
			BounceSuccess,
			MultiBounce,
			HitNothing,
		}

		public bool IsDetonated { get; private set; }
		public ElementType Element => _element;
		public LifeForce Life => _life;
		public bool CanBounce => _maxBounces > 0 && _numBounces < _maxBounces;
		public short ActualDamage
		{
			get
			{
				if (!_useLifeDamageFactor)
				{
					return _damage;
				}

				return (short)Mathf.Max(1, _damage * _life.GetHealthPercent());
			}
		}

		[Header("General")]
		[SerializeField] float _initialForce;
		[SerializeField] float _initialUpForce;
		[SerializeField] uint _maxBounces;
		[SerializeField] float _floorBounceForce;
		[SerializeField] float _maxLifetime;
		[SerializeField] EffectAudioSettings _bounceAudio;
		[SerializeField] ParticleSystem[] _particlesToDisableOnDeath;
		[SerializeField] ProjectileClass[] _projectileClashes;

		[Header("Damage")]
		[SerializeField] ElementType _element;
		[SerializeField] short _damage;
		[SerializeField] short _heal;
		[SerializeField] bool _isAoe;
		[SerializeField] bool _isPiercing;
		[SerializeField] bool _useLifeDamageFactor;
		[SerializeField] float _bonusKnockback;

		[Space]
		[SerializeField] float _heatDamage;
		[SerializeField] float _heatDamageTimeBeforeCool;

		[Space]
		[SerializeField] float _disruptTime;

		[Header("Homing")]
		[SerializeField] float _homingStrength;
		[SerializeField][Range(-1, 1)] float _homingDotThreshold;
		[SerializeField] float _delayBeforeHoming;

		[Header("References")]
		[SerializeField] Rigidbody _rb;
		[SerializeField] LifeForce _life;
		[SerializeField] BoxCollider _boxCollider;

		private Pawn _homingTarget;
		private Vector3 _startPos;
		private int _numBounces;
		private int _numWallBounces;
		private float _aliveTimer;
		private float _homingSearchCooldown;
		private float _timeSinceLastBounce = Mathf.Infinity;
		private float _lastFloorBounceTime;
		private ExpireableInfo<IReadOnlyList<Collider>> _ignoreBounceInfo;
		private List<ExpireableInfo<EntityId>> _affectedEntities;

		public Configuration Configure()
		{
			return new Configuration(this);
		}

        public override ProjectileBasicInfo GetProjectileBasicInfo()
        {
            var info = new ProjectileBasicInfo();

			info.Flags.Set((int)ProjectileBasicInfo.AttributeFlags.DoesBounce, _maxBounces > 0);
			info.Flags.Set((int)ProjectileBasicInfo.AttributeFlags.DoesHeatDamage, _heatDamage > 0);
			info.Flags.Set((int)ProjectileBasicInfo.AttributeFlags.DoesDisrupt, _disruptTime > 0);
			info.Flags.Set((int)ProjectileBasicInfo.AttributeFlags.IsHoming, _homingStrength > 0);
			info.Flags.Set((int)ProjectileBasicInfo.AttributeFlags.IsMini, gameObject.layer == LayerMask.NameToLayer("MiniProjectile"));
			info.Flags.Set((int)ProjectileBasicInfo.AttributeFlags.HasExplosionDamage, _life.HasExplosionDamageOnDeath);

			return info;
        }

        public void Detonate()
		{
			if (!Server.IsActive)
			{
				return;
			}

			if (IsDetonated)
			{
				return;
			}

			IsDetonated = true;

			_life.Kill(Owner, _element, CreateProjectileStats());
		}

		protected override void Awake()
		{
			base.Awake();

			_startPos = transform.position;
			_life.OnDeathBasic += OnDeath;
			_affectedEntities = new List<ExpireableInfo<EntityId>>();

			NetworkEvents.Subscribe<HomingTargetMsg>(OnHomingTargetMsg);
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();

			NetworkEvents.Unsubscribe<HomingTargetMsg>(OnHomingTargetMsg);
		}

        protected override void Start()
        {
            base.Start();

			_rb.linearVelocity = (transform.forward * _initialForce) + (transform.up * _initialUpForce);
        }

        protected override void OnInit()
		{
			HandleHomingSearch();
		}

		private void OnDeath()
		{
			IsDetonated = true;

			StopParticles();
			Destroy(this.gameObject);
		}

		private void StopParticles()
		{
			if (_particlesToDisableOnDeath == null
				|| _particlesToDisableOnDeath.Length == 0)
			{
				return;
			}

			var root = _particlesToDisableOnDeath[0];
			DebrisManager.Register(root.gameObject);

#if UNITY_EDITOR
			root.name = $"{name} [StopParticlesRoot]";
#endif

			for (int i = 0; i < _particlesToDisableOnDeath.Length; ++i)
			{
				var particle = _particlesToDisableOnDeath[i];

				if (particle == null)
				{
					continue;
				}

				particle.Stop();
				particle.transform.SetParent(i == 0 ? null : root.transform);

			}

			Destroy(root.gameObject, 3);
		}

		private void Update()
		{
			if (IsDetonated)
			{
				return;
			}

			_aliveTimer += Time.deltaTime;
			_timeSinceLastBounce += Time.deltaTime;

			if (_maxLifetime > 0
				&& _aliveTimer >= _maxLifetime)
			{
				Detonate();
			}

			if (_homingStrength > 0
				&& _aliveTimer >= _delayBeforeHoming)
			{
				HandleHomingTurn();

				if (Server.IsActive)
				{
					HandleHomingSearch();
				}
			}
		}

		private void FixedUpdate()
		{
			if (IsDetonated)
			{
				return;
			}

			if (_homingStrength > 0)
			{
				_rb.linearVelocity = transform.forward * _rb.linearVelocity.magnitude;
			}

			if (_affectedEntities.Count > 0)
			{
				for (int i = _affectedEntities.Count - 1; i >= 0; --i)
				{
					if (!_affectedEntities[i].HasExpired)
					{
						continue;
					}

					_affectedEntities.RemoveAt(i);
				}
			}
		}

        private void OnTriggerEnter(Collider other)
		{
			if (IsDetonated)
			{
				return;
			}

			if (HandleSpecialCollision(other))
			{
				return;
			}

			if (Server.IsActive)
			{
				if (HandleEntityCollision(other))
				{
					return;
				}
			}

			if (HandleBounce(other) != BounceResult.BounceFail)
			{
				return;
			}

			Detonate();
		}

		private void OnHomingTargetMsg(HomingTargetMsg msg)
		{
			if (identity.entityId != msg.OwnerId)
			{
				return;
			}

			if (msg.TargetId != EntityId.Invalid)
			{
				EntityManager.ActivePawns.TryGetValue(msg.TargetId, out _homingTarget);
			}
			else
			{
				_homingTarget = null;
			}
		}

		private void HandleHomingTurn()
		{
			if (_homingTarget == null)
			{
				return;
			}

			if (_timeSinceLastBounce < 0.1f)
			{
				return;
			}

			Vector3 targetPos = _homingTarget.Position;

			// use historic position if not server and target is a local entity
			if (!Server.IsActive
				&& GameClient.HasAuthority(_homingTarget.identity))
			{
				if (EntityManager.TryGetEntitySnapshot(_homingTarget.identity.entityId, GameClient.Latency, out var snapshot))
				{
					targetPos = snapshot.Position;
				}
				else
				{
					Log.Warning(LogChannel.Projectile, $"HandleHomingTurn - couldn't get historic snapshot for {_homingTarget.identity.entityId} with latency: {GameClient.Latency}");
				}
			}

			targetPos.y = transform.position.y;

			Vector3 targetDir = (targetPos - transform.position);
			transform.Rotate(Vector3.up, JHelper.AngleDir(transform.forward, targetDir, Vector3.up) * _homingStrength * Time.deltaTime);
		}

		private void HandleHomingSearch()
		{
			if (!Server.IsActive)
			{
				return;
			}

			if (_homingStrength <= 0)
			{
				return;
			}

			_homingSearchCooldown -= Time.deltaTime;

			// periodically scan for a target
			if (_homingSearchCooldown > 0)
			{
				return;
			}

			EntityId prevTargetId = _homingTarget != null ? _homingTarget.identity.entityId : EntityId.Invalid;

			_homingTarget = null;
			_homingSearchCooldown = 0.75f;

			float highestScore = 0;

			// determine new homing target
			foreach (var team in EntityManager.ActivePawnsByTeam)
			{
				// skip same team
				if (JHelper.SameTeam(team.Key, Owner.TeamId))
				{
					continue;
				}

				foreach (var pawn in team.Value.Values)
				{
					// skip self
					if (pawn.identity.entityId == Owner.EntityId)
					{
						continue;
					}

					// can't target due to status
					if (pawn.StatusEffectManager.HasStatus(StatusClass.StealthChip))
					{
						continue;
					}

					Vector3 dirToVeh = (pawn.Position - transform.position).normalized;
					float dot = Vector3.Dot(transform.forward, dirToVeh);

					if (dot < _homingDotThreshold)
					{
						continue;
					}

					float distScore = 250 / Mathf.Max(1, Vector3.Distance(transform.position, pawn.Position));
					float dotScore = (dot + 1) * 50;

					float score = distScore + dotScore;

					if (score > highestScore)
					{
						highestScore = score;
						_homingTarget = pawn;
					}
				}
			}

			EntityId currTargetId = _homingTarget != null ? _homingTarget.identity.entityId : EntityId.Invalid;

			// only send changes to target
			if (currTargetId != prevTargetId)
			{
				using (var sendMsg = new HomingTargetMsg()
				{
					OwnerId = identity.entityId,
					TargetId = currTargetId,
				})
				{
					ServerSend.ToAllExceptHost(sendMsg);
				}
			}
		}

		private bool HandleSpecialCollision(Collider other)
		{
			if (other.gameObject.layer != LayerMask.NameToLayer("ProjectileOnly"))
			{
				return false;
			}

			if (other.CompareTag("ProjectileGC"))
			{
				StopParticles();
				Destroy(this.gameObject);

				return true;
			}

			return false;
		}

		private bool HandleEntityCollision(Collider other)
		{
			NetworkEntity entity = other.GetComponent<NetworkEntity>();

			if (entity == null)
			{
				// didn't hit any sort of entity
				return false;
			}

			if (_affectedEntities.Find(elem => elem.Value == entity.identity.entityId) != null)
			{
				// already hit this entity, it must have multiple colliders
				return true;
			}

			Pawn otherPawn = null;
			ProjectileDirect otherProjectile = null;
			LifeForce otherLife;

			// determine entity type
			if (entity is Pawn pawnTest)
			{
				otherPawn = pawnTest;
				otherLife = otherPawn.Life;
			}
			else if (entity is ProjectileDirect projTest)
			{
				// don't collide with dead projectiles
				if (projTest.IsDetonated)
				{
					return true;
				}

				otherProjectile = projTest;
				otherLife = projTest._life;
			}
			else // possibly other misc entity
			{
				otherLife = entity.GetComponent<LifeForce>();
			}

			if (otherLife == null)
			{
				// nothing to damage on entity
				return false;
			}

			bool shouldAffectEntity = false;
			bool shouldDestroySelf = false;

			if (otherPawn != null)
			{
				// hit self
				if (otherPawn.identity.entityId == Owner.EntityId)
				{
					if (_aliveTimer >= 1
						|| _numBounces > 0)
					{
						shouldAffectEntity = true;
						shouldDestroySelf = true;
					}
					else
					{
						return true;
					}
				}
				// hit another entity
				else
				{
					shouldAffectEntity = true;
					shouldDestroySelf = true;
				}
			}
			else if (otherProjectile != null)
			{
				// same class always cancels self out
				if (otherProjectile.ProjectileClass == ProjectileClass
					|| otherProjectile._projectileClashes.Contains(ProjectileClass))
				{
					_life.Kill(otherProjectile.Owner, otherProjectile._element, otherProjectile.CreateProjectileStats());
					otherLife.Kill(Owner, _element, CreateProjectileStats());

					return true;
				}
				else
				{
					shouldAffectEntity = true;

					// the other proj damages this one
					var elementFlags = otherProjectile._isAoe ? ElementFlags.IsAoe : ElementFlags.None;
					_life.Damage(new DamageInfo(otherProjectile.Owner, new ElementData(otherProjectile._element, otherProjectile.ActualDamage, elementFlags),
						otherProjectile.CreateProjectileStats()));

					// prevent duplicate collision processing
					otherProjectile._affectedEntities.Add(new ExpireableInfo<EntityId>(identity.entityId, 0.1f));
				}
			}
			else if (otherLife != null)
			{
				shouldAffectEntity = true;
				shouldDestroySelf = true;
			}

			if (shouldAffectEntity)
			{
				AffectEntity(otherLife, otherPawn, otherProjectile);
			}

			if (shouldDestroySelf
				&& !_isPiercing)
			{
				Detonate();
			}

			return true;
		}

		private void AffectEntity(LifeForce otherLife, Pawn otherPawn, ProjectileDirect otherProjectile)
		{
			_affectedEntities.Add(new ExpireableInfo<EntityId>(otherLife.identity.entityId, 0.1f));

			// damage other
			if (ActualDamage > 0)
			{
				var elementFlags = _isAoe ? ElementFlags.IsAoe : ElementFlags.None;
				otherLife.Damage(new DamageInfo(Owner, new ElementData(_element, ActualDamage, elementFlags), CreateProjectileStats()));
			}

			// heal other
			if (_heal > 0)
			{
				otherLife.Heal(new HealInfo(_heal, _element, _isAoe));
			}

			// additional effects if target still alive
			if (otherLife.IsAlive)
			{
				if (otherProjectile == null)
				{
					// deal heat damage to other
					otherLife.HeatDamage(new HeatDamageInfo(Owner, _heatDamage, _heatDamageTimeBeforeCool));
				}

				// apply knockback to moveable non-projectile entities
				if (ActualDamage > 0
					&& otherProjectile == null
					&&
					((otherPawn != null && otherPawn.CanMove())
					||
					otherPawn == null))
				{
					using (var sendMsg = new EntityImpulseMsg()
					{
						EntityId = otherLife.identity.entityId,
						Direction = (JHelper.FlatDirection(transform.position, otherLife.transform.position) + transform.forward).normalized,
						Magnitude = (ActualDamage * 0.25f) + _bonusKnockback,
					})
					{
						ServerSend.ToAll(sendMsg);
					}
				}

				if (_disruptTime > 0
					&& otherPawn != null)
				{
					otherPawn.StatusEffectManager.Add(StatusClass.Disrupted, _disruptTime);
				}
			}
		}

		private BounceResult HandleBounce(Collider other)
		{
			// no bounce capability
			if (_maxBounces == 0)
			{
				return BounceResult.BounceFail;
			}

			// damage self if would instantly bounce back
			if (_aliveTimer < 0.075f)
			{
				if (Server.IsActive
					&& EntityManager.ActivePawns.TryGetValue(Owner.EntityId, out var ownerPawn))
				{
					transform.forward = -ownerPawn.transform.forward;
					AffectEntity(ownerPawn.Life, ownerPawn, null);
				}

				return BounceResult.BounceFail;
			}

			// ignore multi-bounce
			if (_ignoreBounceInfo != null
				&& !_ignoreBounceInfo.HasExpired
				&& _ignoreBounceInfo.Value.Contains(other))
			{
				return BounceResult.MultiBounce;
			}

			// no remaining bounces
			if (!CanBounce)
			{
				return BounceResult.BounceFail;
			}

			var hitPoint = other.ClosestPoint(transform.position);
			var hitDir = ((hitPoint - transform.position) + _rb.linearVelocity).normalized;
			var distToHitPoint = JHelper.FlatDistance(transform.position, hitPoint);
			var isOnRight = Vector3.Dot(transform.right, hitDir) > 0;
			var rightShift = (transform.right * (isOnRight ? _boxCollider.size.x : -_boxCollider.size.x) / 2) * (distToHitPoint / _boxCollider.size.x);
			var scanStart = transform.position - hitDir + rightShift;
			var scanLength = 4f;

			Debug.DrawLine(scanStart, scanStart + hitDir * scanLength * 0.8f, Color.blue, 3f);
			Debug.DrawLine(scanStart + hitDir * scanLength * 0.8f, scanStart + hitDir * scanLength, Color.cyan, 3f);
			Debug.DrawLine(hitPoint, hitPoint + Vector3.up, Color.red, 3f);

			if (Physics.Raycast(scanStart, hitDir, out var startHitInfo, scanLength, GameManager.instance.BounceLayer))
			{
				var nearColliders = Physics.OverlapSphere(transform.position, _boxCollider.size.z, GameManager.instance.BounceLayer);
				var ignoreColliders = new List<Collider>(nearColliders);
				_ignoreBounceInfo = new ExpireableInfo<IReadOnlyList<Collider>>(ignoreColliders);

				Vector3 newScanStart = transform.position;
				Vector3 prevForward = transform.forward;

				// floor bounce
				if (_rb.useGravity
					&& startHitInfo.normal == Vector3.up)
				{
					BounceEffect(false);
					ApplyFloorBounce();

					// all good if not about to hit a wall
					if (!TryBasicWallTest(newScanStart, prevForward, out _, out _))
					{
						ignoreColliders.Add(startHitInfo.collider);
						return BounceResult.BounceSuccess;
					}
				}

				BounceEffect(true);
				ApplyWallBounce(startHitInfo.normal);

				Collider lastHitCollider = startHitInfo.collider;

				// wall bounces
				while (TryBasicWallTest(newScanStart, prevForward, out var newHitInfo, out var newScanDir))
				{
					if (newHitInfo.collider == lastHitCollider)
					{
						return BounceResult.BounceSuccess;
					}

					if (!CanBounce)
					{
						return BounceResult.BounceFail;
					}

					prevForward = transform.forward;
					transform.forward = newScanDir;

					BounceEffect(true);
					ApplyWallBounce(newHitInfo.normal);

					if (!ignoreColliders.Contains(newHitInfo.collider))
					{
						ignoreColliders.Add(newHitInfo.collider);
					}

					lastHitCollider = newHitInfo.collider;
					newScanStart = newHitInfo.point;
				}

				// could be intersecting with the floor so try bounce up immediately
				if (_rb.useGravity)
				{
					Vector3 floorTestPos = transform.position + transform.forward;

					if (Physics.Raycast(floorTestPos + Vector3.up, Vector3.down, out var floorTestInfo, 2, GameManager.instance.FloorLayer)
						&& Vector3.Distance(floorTestPos, floorTestInfo.point) <= _boxCollider.size.y)
					{
						if (CanBounce)
						{
							ignoreColliders.Add(floorTestInfo.collider);

							BounceEffect(false, Time.time - _lastFloorBounceTime > 0.1f);
							ApplyFloorBounce();

							return BounceResult.BounceSuccess;
						}

						return BounceResult.BounceFail;
					}
				}

				return BounceResult.BounceSuccess;
			}
			else
			{
				return BounceResult.HitNothing;
			}
		}

		private bool TryBasicWallTest(Vector3 fromPos, Vector3 prevFwd, out RaycastHit hitInfo, out Vector3 scanDir)
		{
			Vector3 scanStart = new Vector3(fromPos.x, 0.5f, fromPos.z) - (prevFwd * (_boxCollider.size.z / 2));
			scanDir = (transform.forward + (prevFwd * 0.5f)).normalized;
			float scanLength = _boxCollider.size.z * 2.5f;

			if (Physics.Raycast(scanStart, scanDir, out hitInfo, scanLength, GameManager.instance.BounceLayer))
			{
				Debug.DrawLine(scanStart, hitInfo.point, Color.cyan, 3f);
				return true;
			}

			Debug.DrawLine(scanStart, scanStart + scanDir * scanLength * 0.8f, Color.red, 3f);
			Debug.DrawLine(scanStart + scanDir * scanLength * 0.8f, scanStart + scanDir * scanLength, Color.yellow, 3f);
			return false;
		}

		private void BounceEffect(bool wasWallBounce, bool playSound = true)
		{
			if (playSound)
			{
				AudioManager.PlayClipAtPoint(_bounceAudio, transform.position);
			}

			// increment bounce count
			_timeSinceLastBounce = 0;
			++_numBounces;

			if (wasWallBounce)
			{
				++_numWallBounces;
			}
		}

		private void ApplyWallBounce(Vector3 normal)
		{
			transform.forward = Vector3.Reflect(transform.forward, normal);
			_rb.linearVelocity = transform.forward * _initialForce;
		}

		private void ApplyFloorBounce()
		{
			_rb.linearVelocity = (transform.forward * _initialForce) + (Vector3.up * _floorBounceForce);
			_lastFloorBounceTime = Time.time + 0.1f;
		}

		private ProjectileStats CreateProjectileStats()
		{
			float distToStart = Vector3.Distance(_startPos, transform.position);
			return new ProjectileStats(_numWallBounces, distToStart);
		}
	}
}
