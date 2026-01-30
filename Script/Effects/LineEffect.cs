using HoverTanks.Entities;
using UnityEngine;

namespace HoverTanks.Effects
{
    public abstract class LineEffect : SustainedWeaponEffect
    {
        protected enum ScanHitResult
        {
            None,
            Entity,
            Surface,
        }

        protected override float ScanLength => _rayLength;
        protected override float ScanWidth => _rayWidth;

        [Header("Scan Parameters")]
        [SerializeField] float _rayLength;
        [SerializeField] float _rayWidth;

        protected void Scan(out Vector3 endPos, out ScanHitResult result)
        {
            // prevent shooting through targets up close
            Vector3 startPos = ShootPoint.position - ShootPoint.forward * 0.25f;

            Physics.SphereCast(startPos, _rayWidth, ShootPoint.forward, out var hitInfo, _rayLength, ScanLayer);

            if (hitInfo.transform == null)
            {
                endPos = ShootPoint.position + ShootPoint.forward * _rayLength;

                Target = null;
                result = ScanHitResult.None;
            }
            else
            {
                endPos = ShootPoint.position + ShootPoint.forward * hitInfo.distance;

                var prevTarget = Target;
                Target = hitInfo.collider.GetComponent<LifeForce>();

                // hit new target
                if (Target != null && Target != prevTarget)
                {
                    AcknowledgeTarget(Target);
                }

                result = Target != null ? ScanHitResult.Entity : ScanHitResult.Surface;
            }
        }
    }
}
