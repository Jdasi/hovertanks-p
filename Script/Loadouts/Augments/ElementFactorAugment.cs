using HoverTanks.Entities;
using UnityEngine;

namespace HoverTanks.Loadouts
{
    [CreateAssetMenu(menuName = "Loadout/Augments/Element Factor")]
    public class ElementFactorAugment : Augment
    {
        [SerializeField] LifeForce.ElementFactorData[] _factors;

        public override void Apply(Pawn pawn)
        {
            var config = pawn.Life.Configure();

            foreach (var factors in _factors)
            {
                config.AddElementFactor(factors.Type, factors.Direct, factors.AOE);
            }
        }
    }
}
