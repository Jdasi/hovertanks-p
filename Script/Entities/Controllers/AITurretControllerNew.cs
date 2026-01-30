using HoverTanks.AI;
using UnityEngine;

namespace HoverTanks.Entities
{
    [CreateAssetMenu(menuName = "Controllers/AI Turret Controller New")]
    public class AITurretControllerNew : AIPawnController
    {
        [Header("Target Acquisition")]
        [SerializeField] LayerMask _obstacleLayer;

        [Header("Skills")]
        [SerializeField] float _maxShootAngle;
        [SerializeField] FloatRange _randomTurnCooldown;
        [SerializeField] Chance _directShotLikelihood;
        [SerializeField] Chance _bounceShotLikelihood;

        private AITargetingModule _targetingModule;
        private Vector3 _randomTurnToPos;
        private float _acquireRandomTurnPosCooldown;

        protected override void OnInit()
        {
            base.OnInit();

            _targetingModule = new AITargetingModule(Pawn, _obstacleLayer);
        }

        protected override void OnEnabled()
        {
            TriggerShootCooldown(0.5f);
        }

        protected override void OnFixedUpdate()
        {
            _targetingModule.FixedUpdate();
            _acquireRandomTurnPosCooldown -= Time.fixedDeltaTime;

            HandleAiming();
            HandleShooting();
        }

        private void HandleAiming()
        {
            if (Pawn.WeaponInfo?.IsRecharging ?? false)
            {
                Pawn.StopAiming();
                return;
            }

            if (_acquireRandomTurnPosCooldown <= 0)
            {
                Vector2 random = Random.insideUnitCircle;
                _randomTurnToPos = Pawn.Position + new Vector3(random.x, 0, random.y);
                _acquireRandomTurnPosCooldown = Random.Range(_randomTurnCooldown.Min, _randomTurnCooldown.Max);
            }

            Pawn.StartAiming(_randomTurnToPos);
        }

        private void HandleShooting()
        {
            if (_targetingModule.Target == null)
            {
                Pawn.StopShoot();
                return;
            }

            if (!CanShoot())
            {
                Pawn.StopShoot();
                return;
            }

            bool hasShot = CalculateShot();

            if (!hasShot)
            {
                TriggerShootCooldown(0.5f);
                Pawn.StopShoot();

                return;
            }

            Pawn.Shoot();

            TriggerShootCooldown();
        }

        private bool CalculateShot()
        {
            // abort if no target
            if (_targetingModule.Target == null)
            {
                return false;
            }

            Pawn target = _targetingModule.Target;
            Vector3 dirFromSight = JHelper.FlatDirection(Pawn.SightPoint.position, target.Position);

            // early pass if direct shot at target
            if (_targetingModule.HasLOS
                && Vector3.Angle(Pawn.SightPoint.forward, dirFromSight) <= _maxShootAngle)
            {
                // test for direct shot
                if (_directShotLikelihood.Test())
                {
                    return true;
                }
            }

            /*
            int numBounces = _pawn.WeaponInfo.GetProjectileMaxBounces();

            // abort if no abilty to bounce
            if (numBounces == 0)
            {
                return false;
            }

            // abort if no desire to take a bounce shot this time
            if (!_bounceShotLikelihood.Test())
            {
                return false;
            }

            Vector3 sightPos = _pawn.SightPoint.position;
            Vector3 sightForward = _pawn.SightPoint.forward;

            // test for bounce
            for (int i = 0; i < numBounces; ++i)
            {
                // show trajectory up to bounce
                Physics.Raycast(sightPos, sightForward, out var hitInfo, 100, GameManager.instance.GeometryLayer);
                Debug.DrawRay(sightPos, hitInfo.point - sightPos, Color.yellow, 1);

                // calculate bounce
                Vector3 reflect = Vector3.Reflect(sightForward, hitInfo.normal);
                Vector3 reflectPos = hitInfo.point + hitInfo.normal * 0.1f;
                Vector3 dirFromReflect = (target.Position - reflectPos).normalized;

                // show the bounce trajectory
                Physics.Raycast(reflectPos, reflect, out var bounceHitInfo, 100, _obstacleLayer);
                Debug.DrawRay(hitInfo.point, bounceHitInfo.point - hitInfo.point, Color.yellow, 1);

                // abort if the bounce would hit self
                if (bounceHitInfo.transform == _pawn.Transform)
                {
                    return false;
                }

                // check for dot tolerance and ability to see target from the new position
                if (Vector3.Angle(reflect, dirFromReflect) <= _maxShootAngle
                    && JHelper.CanSeeTransform(reflectPos, target.transform, _obstacleLayer, false))
                {
                    return true;
                }

                // update sight for subsequent bounces
                sightPos = reflectPos;
                sightForward = reflect;
            }
            */

            return false;
        }
    }
}
