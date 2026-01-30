using HoverTanks.AI;
using UnityEngine;

namespace HoverTanks.Entities
{
    [CreateAssetMenu(menuName = "Controllers/AI Turret (Mortar)")]
    public class AITurretController_Mortar : AIPawnController
    {
        private AITargetingModule _targetingModule;

        protected override void OnInit()
        {
            base.OnInit();

            _targetingModule = new AITargetingModule(Pawn, default, 1);
        }

        protected override void OnEnabled()
        {
            TriggerShootCooldown();
        }

        protected override void OnFixedUpdate()
        {
            _targetingModule.FixedUpdate();
            HandleShooting();
        }

        private void HandleShooting()
        {
            if (_targetingModule.Target == null
                || _targetingModule.DistToTarget > _maxShootDist)
            {
                TriggerShootCooldown(0.5f);
                Pawn.StopAiming();

                return;
            }

            Pawn.StartAiming(_targetingModule.Target.Position);

            // try shoot
            if (Pawn.IsPawnFacing(_targetingModule.Target.Position, 25)
                && CanShoot())
            {
                TriggerShootCooldown();

                Pawn.Shoot(_targetingModule.Target.Position);
            }
            else
            {
                Pawn.StopShoot();
            }
        }
    }
}
