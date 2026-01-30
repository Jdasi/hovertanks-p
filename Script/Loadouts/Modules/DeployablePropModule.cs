using HoverTanks.Networking;
using UnityEngine;

namespace HoverTanks.Loadouts
{
    [CreateAssetMenu(menuName = "Loadout/Modules/Deployable Prop")]
    public class DeployablePropModule : Module
    {
        [Header("Prop Parameters")]
        [SerializeField] PropClass _propClass;
        [SerializeField] float _forwardOffset;

        private const float DELAY_BETWEEN_TESTS = 0.1f;

        private float _nextTestTime;

        protected override void OnInit()
        {
            SetDisabledActionMessages(new PawnEquipmentActionMsg.Actions[]
            {
                PawnEquipmentActionMsg.Actions.OnActivate,
                PawnEquipmentActionMsg.Actions.OnDeactivate,
                PawnEquipmentActionMsg.Actions.ReloadStart,
                PawnEquipmentActionMsg.Actions.ReloadFinish,
            });
        }

        protected override void OnActiveUpdateLocal()
        {
            if (!TestNextProc())
            {
                return;
            }

            if (Time.time < _nextTestTime)
            {
                return;
            }

            _nextTestTime = Time.time + DELAY_BETWEEN_TESTS;

            if (!JHelper.TryGetFloorAtPos(CurrentShootPoint.position, out _))
            {
                return;
            }

            _nextProcTime = Time.time + _delayBetweenProcs;

            ConsumeCharge();
            OnProcEffect();
        }

        protected override void OnProcEffect()
        {
            CommonModuleEffects();

            // shot message
            SendActionMessage(PawnEquipmentActionMsg.Actions.Proc);

            if (Server.IsActive)
            {
                ServerRequestProjectile(ProcPoint);
            }
        }

        private void ServerRequestProjectile(Transform shootPoint)
        {
            if (!Server.IsActive)
            {
                return;
            }

            Vector3 spawnPos = shootPoint.position + shootPoint.forward * _forwardOffset;
            spawnPos.y = 0.1f;

            float heading = JHelper.RotationToHeading(shootPoint.rotation);

            ServerSpawn.Prop(new ServerCreatePropData()
            {
                Class = _propClass,
                Position = spawnPos,
                Heading = heading,
                Owner = Owner.identity,
            });
        }
    }
}
