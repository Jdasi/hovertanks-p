using HoverTanks.Entities;
using HoverTanks.Events;
using HoverTanks.Networking;
using System.Collections.Generic;
using UnityEngine;

namespace HoverTanks.Effects
{
    public abstract class SustainedWeaponEffect : MonoBehaviour
    {
        protected class TargetInfo
        {
            public readonly LifeForce Life;
            public float DamageTimer;

            public TargetInfo(LifeForce life)
            {
                Life = life;
            }
        }

        protected NetworkIdentity Owner { get; private set; }
        protected Transform ShootPoint { get; private set; }
        protected LifeForce Target { get; set; }
        protected abstract float ScanWidth { get; }
        protected abstract float ScanLength { get; }
        protected LayerMask ScanLayer => _scanLayer;

        [SerializeField] SustainedEffectTypes _effectType;

        [Header("Damage")]
        [SerializeField] ElementType _element;
        [SerializeField] int _damage;
        [SerializeField] float _heatDamage;
        [SerializeField] float _heatDamageCoolTime;
		[SerializeField] float _damageInterval;
        [SerializeField] float _effectiveRange;
        [SerializeField] LayerMask _scanLayer;

        [Header("Audio")]
        [SerializeField] EffectAudioSettings _loopAudio;
        [SerializeField] EffectsSource _source;

        [Header("Base References")]
        [SerializeField] ParticleSystem _muzzleEffect;
        [SerializeField] ParticleSystem _ejectorEffect;

        private Dictionary<EntityId, TargetInfo> _targetHistory;

        public ProjectileBasicInfo GetProjectileBasicInfo()
        {
            var info = new ProjectileBasicInfo();

            info.Flags.Set((int)ProjectileBasicInfo.AttributeFlags.IsLineEffect, _effectType == SustainedEffectTypes.Line);
            info.Flags.Set((int)ProjectileBasicInfo.AttributeFlags.IsConeEffect, _effectType == SustainedEffectTypes.Cone);
            info.Flags.Set((int)ProjectileBasicInfo.AttributeFlags.DoesHeatDamage, _heatDamage > 0);
			info.Flags.Set((int)ProjectileBasicInfo.AttributeFlags.IsMini, gameObject.layer == LayerMask.NameToLayer("MiniProjectile"));

            return info;
        }

        public void Init(NetworkIdentity owner, Transform shootPoint)
        {
            Owner = owner;
            ShootPoint = shootPoint;

            _targetHistory = new Dictionary<EntityId, TargetInfo>(3);
            _source.Play(_loopAudio);

            if (_muzzleEffect != null)
            {
                _muzzleEffect.gameObject.SetActive(false);
            }

            if (_ejectorEffect != null)
            {
                _ejectorEffect.gameObject.SetActive(false);
            }

            NetworkEvents.Subscribe<HitRequestMsg>(OnHitRequestMsg);

            OnInit();
        }

        public void End()
        {
            _source.Stop();

            if (_muzzleEffect != null)
            {
                _muzzleEffect.gameObject.SetActive(false);
                _muzzleEffect.Stop();
            }

            // detach ejector
            if (_ejectorEffect != null)
            {
                _ejectorEffect.transform.SetParent(null);
                _ejectorEffect.Stop();
            }

            // try squeeze in one last damage tick
            if (Target != null)
            {
                var targetInfo = _targetHistory[Target.identity.entityId];

                if (targetInfo.DamageTimer / _damageInterval >= 0.75f)
                {
                    Damage(targetInfo.Life);
                }
            }

            NetworkEvents.Unsubscribe<HitRequestMsg>(OnHitRequestMsg);

            OnEnd();
        }

        protected virtual void FixedUpdate()
        {
            if (ShootPoint == null)
            {
                return;
            }

            if (_muzzleEffect != null)
            {
                _muzzleEffect.transform.position = ShootPoint.position;
                _muzzleEffect.transform.forward = ShootPoint.forward;

                if (!_muzzleEffect.gameObject.activeSelf)
                {
                    _muzzleEffect.gameObject.SetActive(true);
                }
            }

            if (_ejectorEffect != null)
            {
                _ejectorEffect.transform.position = ShootPoint.position - ShootPoint.forward * 0.75f;
                _ejectorEffect.transform.forward = ShootPoint.forward;

                if (!_ejectorEffect.gameObject.activeSelf)
                {
                    _ejectorEffect.gameObject.SetActive(true);
                }
            }
        }

        private void Update()
        {
            HandleDamageTick();
        }

        protected TargetInfo AcknowledgeTarget(LifeForce target)
        {
            // record in target history
            if (!_targetHistory.TryGetValue(target.identity.entityId, out var targetInfo))
            {
                targetInfo = new TargetInfo(target);
                _targetHistory.Add(target.identity.entityId, targetInfo);
            }

            return targetInfo;
        }

        protected void Damage(LifeForce target)
        {
            if (target == null
                || !target.IsAlive
                || ShootPoint == null)
            {
                return;
            }

            float distance = Vector3.Distance(ShootPoint.position, target.transform.position);
            bool wasGlancingBlow = _effectiveRange > 0 && distance > _effectiveRange;

            if (Server.IsActive)
            {
                ServerDamage(target, distance, wasGlancingBlow);
            }
            else
            {
                ClientSend.HitRequest(HitRequestType.SustainedWeapon, Owner.entityId, target.identity.entityId, wasGlancingBlow);
            }
        }

        protected virtual void OnInit() { }
        protected virtual void OnEnd() { }

        private void HandleDamageTick()
        {
            if (!GameClient.HasAuthority(Owner))
            {
                return;
            }

            if (ShootPoint == null)
            {
                return;
            }

            if (Target == null)
            {
                return;
            }

            var targetInfo = _targetHistory[Target.identity.entityId];
            targetInfo.DamageTimer += Time.deltaTime;

            while (targetInfo.DamageTimer >= _damageInterval)
            {
                Damage(targetInfo.Life);
                targetInfo.DamageTimer -= _damageInterval;
            }
        }

        private void ServerDamage(LifeForce target, float distance, bool wasGlancingBlow)
        {
            if (!Server.IsActive)
            {
                return;
            }

            int damage = Mathf.Max(1, _damage);

            if (wasGlancingBlow)
            {
                damage = Mathf.CeilToInt(_damage * 0.5f);
            }

            var elementFlags = wasGlancingBlow ? ElementFlags.WasGlancingBlow : ElementFlags.None;
            target.Damage(new DamageInfo(Owner, new ElementData(_element, damage, elementFlags), new ProjectileStats(0, distance)));

            if (_heatDamage > 0)
            {
                target.HeatDamage(new HeatDamageInfo(Owner, _heatDamage, _heatDamageCoolTime));
            }
        }

        private void OnHitRequestMsg(HitRequestMsg msg)
        {
            if (!Server.IsActive)
            {
                return;
            }

            if (msg.Type != HitRequestType.SustainedWeapon)
            {
                return;
            }

            if (Owner.entityId != msg.OwnerId)
            {
                return;
            }

            if (!EntityManager.TryGetEntity(msg.TargetId, out var entity))
            {
                return;
            }

            var life = entity.GetComponent<LifeForce>();

            if (life == null)
            {
                Log.Error(LogChannel.SustainedWeaponEffect, $"OnHitRequestMsg - entity id {msg.TargetId} did not have LifeForce");
                return;
            }

            Log.Info(LogChannel.SustainedWeaponEffect, $"OnHitRequestMsg - success! {msg.TargetId} was hit");

            float distance = Vector3.Distance(ShootPoint.position, entity.transform.position);
            ServerDamage(life, distance, msg.WasGlancingBlow);
        }
    }
}
