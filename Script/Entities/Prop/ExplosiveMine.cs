using HoverTanks.Events;
using HoverTanks.Networking;
using UnityEngine;

namespace HoverTanks.Entities
{
	public class ExplosiveMine : DeployableProp
	{
		private enum Events
		{
			Arm,
			Trigger,
		}

		[Header("Parameters")]
		[SerializeField] float _armDelay;
		[SerializeField] EffectAudioSettings _armAudio;

		[Space]
		[SerializeField] float _triggerDelay;
		[SerializeField] EffectAudioSettings _triggerAudio;

		[Space]
		[SerializeField] float _autoTriggerDelay;

		[Header("References")]
		[SerializeField] LifeForce _life;
		[SerializeField] Blinker _blinker;
		[SerializeField] CollisionEventForwarder _collisionEvents;

		private bool _armed;
		private bool _triggered;
		private bool _detonated;
		private float _autoTriggerTime = -1;
		private float _armTime;
		private float _detonateTimestamp;

        protected override void Awake()
		{
			base.Awake();

			_blinker.enabled = false;
			_life.OnDeathBasic += OnDeath;
			_collisionEvents.TriggerStayed += TriggerStay;

			if (Server.IsActive)
			{
				_armTime = Time.time + _armDelay;
				_autoTriggerTime = Time.time + _autoTriggerDelay;
			}

			NetworkEvents.Subscribe<EntityEventMsg>(OnEntityEventMsg);
		}

        protected override void OnDestroy()
        {
            base.OnDestroy();

			NetworkEvents.Unsubscribe<EntityEventMsg>(OnEntityEventMsg);
        }

        protected override void OnFixedUpdate()
		{
			if (!Server.IsActive)
			{
				return;
			}

			if (_detonated)
			{
				return;
			}

			// handle arming
			if (!_armed && Time.time >= _armTime)
			{
				_armed = true;
				ServerSend.Helpers.EntityEvent(identity.entityId, (byte)Events.Arm);
			}

			// handle detonation from trigger
			if (_triggered && Time.time >= _detonateTimestamp)
			{
				Detonate();
			}

			// handle automatic trigger
			if (_autoTriggerDelay > 0
				&& _armed
				&& Time.time >= _autoTriggerTime)
			{
				Trigger();
			}
		}

		private void OnDeath()
		{
			if (_detonated)
			{
				return;
			}

			_detonated = true;

			DynamicDecals.PaintExplosion(transform.position, 0.8f);
			CameraShake.Shake(0.2f, 0.2f);

			Destroy(this.gameObject);
		}

		private void TriggerStay(GameObject sender, Collider other)
		{
			if (!Server.IsActive)
			{
				return;
			}

			if (_detonated)
			{
				return;
			}

			if (!_armed)
			{
				return;
			}

			if (_triggered)
			{
				return;
			}

			if (Time.frameCount % 2 != 0)
			{
				return;
			}

			if (!other.CompareTag("Unit"))
			{
				return;
			}

			Trigger();
		}

		private void Trigger()
		{
			if (_triggered)
			{
				return;
			}

			_triggered = true;
			_detonateTimestamp = Time.time + _triggerDelay;

			ServerSend.Helpers.EntityEvent(identity.entityId, (byte)Events.Trigger);
		}

		private void Detonate()
		{
			if (_detonated)
			{
				return;
			}

			_life.Kill(Owner);
		}

		private void OnEntityEventMsg(EntityEventMsg msg)
		{
			if (msg.EntityId != identity.entityId)
			{
				return;
			}

			switch ((Events)msg.EventId)
			{
				case Events.Arm:
				{
					_armed = true;
					_blinker.enabled = true;

					if (_armDelay > 0)
					{
						AudioManager.PlayClipAtPoint(_armAudio, transform.position);
					}
				} break;

				case Events.Trigger:
				{
					_triggered = true;
					_blinker.ForceOn(true);

					AudioManager.PlayClipAtPoint(_triggerAudio, transform.position);
				} break;
			}
		}
	}
}
