using HoverTanks.Entities;
using UnityEngine;

namespace HoverTanks.Loadouts
{
    [CreateAssetMenu(menuName = "Loadout/Augments/Gyro Unlocker")]
    public class GyroUnlockerAugment : Augment
    {
        [SerializeField] float _tiltFactor;
        [SerializeField] float _turnFactor;
        [SerializeField] float _heatFactor;

        public override void Apply(Pawn pawn)
        {
            pawn.Configure()
                .AddTiltVectorAccelFactor(_tiltFactor)
                .AddTurnSpeedFactor(_turnFactor);

            pawn.Life.Configure()
                .AddHeatDamageFactor(_heatFactor);
        }
    }
}
