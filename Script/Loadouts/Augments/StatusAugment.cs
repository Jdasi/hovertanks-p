using HoverTanks.Entities;
using UnityEngine;

namespace HoverTanks.Loadouts
{
    [CreateAssetMenu(menuName = "Loadout/Augments/Status")]
    public class StatusAugment : Augment
    {
        [SerializeField] StatusClass _statusClass;

        public override void Apply(Pawn pawn)
        {
            pawn.StatusEffectManager.LocalAdd(_statusClass);
        }
    }
}
