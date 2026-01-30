using HoverTanks.Events;
using HoverTanks.Networking;
using UnityEngine;

namespace HoverTanks.Entities
{
    public class HealthOrb : Pickup
	{
		[SerializeField] int _healAmount = 2;
		[SerializeField] float _seekForce = 1;
		[SerializeField] float _searchRadius = 5;
		[SerializeField] LayerMask _searchLayer;

		[Space]
		[SerializeField] float _explosionOutwardForce;
		[SerializeField] float _explosionRadius;
		[SerializeField] float _explosionUpwardModifier;

		private const float SEARCH_INTERVAL_IDLE = 0.5f;
		private const float SEARCH_INTERVAL_HOMING = 2f;

		private bool _isUsed;
		private float _searchCooldown;
		private Pawn _targetPawn;

        protected override void Awake()
        {
			base.Awake();

            NetworkEvents.Subscribe<HomingTargetMsg>(OnHomingTargetMsg);
			NetworkEvents.Subscribe<PickupCollectedMsg>(OnPickupCollectedMsg);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

			NetworkEvents.Unsubscribe<HomingTargetMsg>(OnHomingTargetMsg);
			NetworkEvents.Unsubscribe<PickupCollectedMsg>(OnPickupCollectedMsg);
        }

        private void Update()
        {
            if (Server.IsActive)
			{
				SearchForTarget();
			}
        }

        private void FixedUpdate()
		{
			if (_isUsed)
			{
				return;
			}



			if (_targetPawn == null)
			{
				return;
			}

			Vector3 dir = (_targetPawn.transform.position - transform.position).normalized;
			_rb.AddForce(dir * _seekForce, ForceMode.Force);
		}

		private void SearchForTarget()
		{
			if (!Server.IsActive)
			{
				return;
			}

			_searchCooldown -= Time.deltaTime;

			if (_searchCooldown > 0)
			{
				return;
			}

			EntityId prevTargetId = _targetPawn != null ? _targetPawn.identity.entityId : EntityId.Invalid;
			_targetPawn = null;

			var hits = Physics.SphereCastAll(transform.position, _searchRadius, Vector3.up, _searchRadius, _searchLayer);

			if (hits == null || hits.Length == 0)
			{
				return;
			}

			float closestDist = float.MaxValue;

			// find the closest target
			foreach (var hit in hits)
			{
				var pawn = hit.collider.gameObject.GetComponent<Pawn>();

				if (pawn == null)
				{
					continue;
				}

				var dist = Vector3.SqrMagnitude(hit.transform.position - transform.position);

				if (dist >= closestDist)
				{
					continue;
				}

				_targetPawn = pawn;
				closestDist = dist;
			}

			EntityId currTargetId = _targetPawn != null ? _targetPawn.identity.entityId : EntityId.Invalid;

			// enact a longer cooldown if we have a target
			_searchCooldown = _targetPawn != null ? SEARCH_INTERVAL_HOMING : SEARCH_INTERVAL_IDLE;

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

		private void OnHomingTargetMsg(HomingTargetMsg msg)
		{
			if (identity.entityId != msg.OwnerId)
			{
				return;
			}

			EntityManager.ActivePawns.TryGetValue(msg.TargetId, out _targetPawn);
		}

		private void OnPickupCollectedMsg(PickupCollectedMsg msg)
		{
			if (identity.entityId != msg.EntityId)
			{
				return;
			}

			OnPickupEffect();
		}

		private void OnTriggerEnter(Collider other)
		{
			if (!Server.IsActive)
			{
				return;
			}

			if (_isUsed)
			{
				return;
			}

			var life = other.GetComponent<LifeForce>();

			if (life == null
				|| !life.IsAlive)
			{
				return;
			}

			_isUsed = true;

			life.Heal(new HealInfo(_healAmount, ElementType.HealthOrb));

			using (var sendMsg = new PickupCollectedMsg()
			{
				EntityId = identity.entityId,
			})
			{
				ServerSend.ToAllExceptHost(sendMsg);
			}

			OnPickupEffect();
		}

		private void OnPickupEffect()
		{
			Destroy(this.gameObject);
		}
	}
}
