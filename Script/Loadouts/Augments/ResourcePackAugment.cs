using HoverTanks.Entities;
using UnityEngine;

namespace HoverTanks.Loadouts
{
    [CreateAssetMenu(menuName = "Loadout/Augments/Resource Pack")]
    public class ResourcePackAugment : Augment
    {
        [SerializeField] float _factor;

        public override void Apply(Pawn pawn)
        {
            pawn.ModuleInfo?.Configure().AddTimeToRechargeFactor(_factor);
        }
    }
}
