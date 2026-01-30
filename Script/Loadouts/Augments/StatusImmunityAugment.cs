using HoverTanks.Entities;
using UnityEngine;

namespace HoverTanks.Loadouts
{
    [CreateAssetMenu(menuName = "Loadout/Augments/Status Immunity")]
    public class StatusImmunityAugment : Augment
    {
        [SerializeField] StatusClass _statusClass;

        public override void Apply(Pawn pawn)
        {
            pawn.StatusEffectManager.LocalAddImmunity(_statusClass);
        }
    }
}
