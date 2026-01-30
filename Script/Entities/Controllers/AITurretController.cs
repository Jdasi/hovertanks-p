using HoverTanks.AI;
using UnityEngine;

namespace HoverTanks.Entities
{
    [CreateAssetMenu(menuName = "Controllers/AI Turret Controller")]
    public class AITurretController : AIPawnController
    {
        [Header("Target Acquisition")]
        [SerializeField] LayerMask _obstacleLayer;

        [Header("Skills")]
        [SerializeField][Range(0, 100)] int _leadTargetSkill;

        private AITargetingModule _targetingModule;

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
            HandleShooting();
        }

        private void HandleShooting()
        {
            if (_targetingModule.Target == null
                || !_targetingModule.HasLOS
                || _targetingModule.DistToTarget > _maxShootDist)
            {
                TriggerShootCooldown(0.5f);

                Pawn.StopAiming();
                Pawn.StopShoot();

                return;
            }

            // try leading the target
            var targetPos = AITargeting.LeadUnit(Pawn, _targetingModule.Target, _leadTargetSkill, _obstacleLayer);

            Pawn.StartAiming(targetPos);

            if (Pawn.IsPawnFacing(_targetingModule.Target.Position, 25)
                && CanShoot())
            {
                TriggerShootCooldown();

                Pawn.Shoot();
            }
            else
            {
                Pawn.StopShoot();
            }
        }
    }
}
