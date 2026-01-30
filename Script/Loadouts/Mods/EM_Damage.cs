using HoverTanks.Events;
using UnityEngine;

namespace HoverTanks.Loadouts
{
    [CreateAssetMenu(menuName = "Loadout/Mods/Damage")]
    public class EM_Damage : EquipmentMod, EquipmentMod.Aspects.ICleanup
    {
        [SerializeField] short _adjustment;

        protected override void OnInit()
        {
            LocalEvents.Subscribe<ProjectileDirectCreatedData>(OnProjectileDirectCreated);
        }

        public void Cleanup()
        {
            LocalEvents.Unsubscribe<ProjectileDirectCreatedData>(OnProjectileDirectCreated);
        }

        private void OnProjectileDirectCreated(ProjectileDirectCreatedData data)
        {
            // match equipment
            if (data.OwnerEquipmentType != Equipment.EquipmentType)
            {
                return;
            }

            // match owner
            if (data.Projectile.Owner.EntityId != Equipment.Owner.identity.entityId)
            {
                return;
            }

            data.Projectile.Configure()
                .AdjustDamage(_adjustment);
        }
    }
}
