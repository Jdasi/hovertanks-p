using HoverTanks.Entities;
using UnityEngine;

namespace HoverTanks.Loadouts
{
    [CreateAssetMenu(menuName = "Loadout/Augments/Thermal Regulator")]
    public class ThermalRegulatorAugment : Augment
    {
        [SerializeField] float _factor;

        public override void Apply(Pawn pawn)
        {
            pawn.Life.Configure()
                .AddHeatDamageFactor(_factor);
        }
    }
}
