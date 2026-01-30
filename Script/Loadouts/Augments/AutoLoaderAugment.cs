using HoverTanks.Entities;
using UnityEngine;

namespace HoverTanks.Loadouts
{
    [CreateAssetMenu(menuName = "Loadout/Augments/Auto-Loader")]
    public class AutoLoaderAugment : Augment
    {
        [SerializeField] float _factor;

        public override void Apply(Pawn pawn)
        {
            pawn.WeaponInfo?.Configure().AddTimeToReloadFactor(_factor);
        }
    }
}
