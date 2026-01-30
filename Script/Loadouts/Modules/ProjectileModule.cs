using HoverTanks.Networking;
using UnityEngine;

namespace HoverTanks.Loadouts
{
    [CreateAssetMenu(menuName = "Loadout/Modules/Projectile")]
    public class ProjectileModule : Module
    {
        [Header("Projectile Parameters")]
        [SerializeField] ProjectileClass _projectileClass;
        [SerializeField] float _spread;

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

            Vector3 variance = _spread == 0 ? default : new Vector3(Random.Range(-_spread, _spread), 0, Random.Range(-_spread, _spread));

            Vector3 spawnPos = shootPoint.position;
            Quaternion spawnRot = variance == default ? shootPoint.rotation : Quaternion.LookRotation(shootPoint.forward + variance, Vector3.up);

            float heading = JHelper.RotationToHeading(spawnRot);

            ServerSpawn.Projectile(new ServerCreateProjectileData()
            {
                Class = _projectileClass,
                OwnerEquipmentType = EquipmentType.Module,
                Position = spawnPos,
                Heading = heading,
                Owner = Owner.identity,
            });
        }
    }
}
