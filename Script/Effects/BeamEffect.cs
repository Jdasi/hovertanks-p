using HoverTanks.Entities;
using HoverTanks.Networking;
using System.Collections.Generic;
using UnityEngine;

namespace HoverTanks.Effects
{
    public class BeamEffect : LineEffect
    {
        [Header("References")]
        [SerializeField] LineRenderer _line;
        [SerializeField] BoxCollider _boxCollider;
        [SerializeField] ParticleSystem _hitEffect;

        private List<EntityId> _hitProjectiles;

        protected override void OnInit()
        {
            base.OnInit();

            _line.enabled = false;
            _hitEffect.gameObject.SetActive(false);
            _hitProjectiles = new List<EntityId>();
        }

        protected override void FixedUpdate()
        {
            if (ShootPoint == null)
            {
                return;
            }

            _line.SetPosition(0, ShootPoint.position);

            Scan(out var endPos, out var result);
            UpdateBeamEffects(endPos, result != ScanHitResult.None);

            base.FixedUpdate();
        }

        private void UpdateBeamEffects(Vector3 endPos, bool didHit)
        {
            float distToEndPos = (endPos - ShootPoint.position).magnitude;

            // move parent first
            transform.position = ShootPoint.position + ShootPoint.forward * (distToEndPos / 2);
            transform.forward = ShootPoint.forward;

            // update collider size
            _boxCollider.size = new Vector3(ScanWidth, ScanWidth, distToEndPos);

            // move end effects last
            _line.SetPosition(1, endPos);
            _hitEffect.transform.position = endPos;

            if (_hitEffect.gameObject.activeSelf)
            {
                if (!didHit)
                {
                    _hitEffect.gameObject.SetActive(false);
                }
            }
            else
            {
                if (didHit)
                {
                    _hitEffect.gameObject.SetActive(true);
                }
            }

            if (!_line.enabled)
            {
                _line.enabled = true;
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (!GameClient.HasAuthority(Owner))
            {
                return;
            }

            if (ShootPoint == null)
            {
                return;
            }

            if (!other.CompareTag("Projectile"))
            {
                return;
            }

            var life = other.GetComponent<LifeForce>();

            if (life == null)
            {
                Log.Error(LogChannel.BeamEffect, $"OnTriggerEnter - {other.name} tagged as projectile but didn't have life force");
                return;
            }

            // one hit should be enough to kill a projectile
            if (_hitProjectiles.Contains(life.identity.entityId))
            {
                return;
            }

            _hitProjectiles.Add(life.identity.entityId);

            Damage(life);
        }
    }
}
