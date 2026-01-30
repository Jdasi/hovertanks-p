using HoverTanks.Entities;
using UnityEngine;

namespace HoverTanks.Loadouts
{
    [CreateAssetMenu(menuName = "Loadout/Augments/Movement Mode")]
    public class MovementModeAugment : Augment
    {
        [SerializeField] float _forwardSpeedFactor;
        [SerializeField] float _strafeSpeedFactor;
        [SerializeField] float _reverseSpeedFactor;

        public override void Apply(Pawn pawn)
        {
            pawn.Configure()
                .AddForwardSpeedFactor(_forwardSpeedFactor)
                .AddStrafeSpeedFactor(_strafeSpeedFactor)
                .AddReverseSpeedFactor(_reverseSpeedFactor);
        }
    }
}
