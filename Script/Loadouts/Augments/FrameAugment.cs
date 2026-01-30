using HoverTanks.Entities;
using UnityEngine;

namespace HoverTanks.Loadouts
{
    [CreateAssetMenu(menuName = "Loadout/Augments/Frame")]
    public class FrameAugment : Augment
    {
        [SerializeField] int _healthAdjustment;
        [SerializeField] float _speedFactor;

        public override void Apply(Pawn pawn)
        {
            pawn.Life.Configure()
                .AdjustMaxHealth(_healthAdjustment);

            pawn.Configure()
                .AddForwardSpeedFactor(_speedFactor)
                .AddStrafeSpeedFactor(_speedFactor)
                .AddReverseSpeedFactor(_speedFactor);
        }
    }
}
