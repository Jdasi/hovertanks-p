using HoverTanks.Entities;
using UnityEngine;

namespace HoverTanks.StatusEffects
{
    [CreateAssetMenu(menuName = "Statuses/Disrupted")]
    public class SE_Disrupted : StatusEffect
    {
        [SerializeField] float _speedFactor;
        [SerializeField] float _reloadFactor;

        protected override void OnStart()
        {
            if (!GetTargetAs(out Pawn pawn))
            {
                return;
            }

            pawn.Configure()
                .AddForwardSpeedFactor(_speedFactor)
                .AddStrafeSpeedFactor(_speedFactor)
                .AddReverseSpeedFactor(_speedFactor)
                .AddTurnSpeedFactor(_speedFactor);

            if (pawn.WeaponInfo != null)
            {
                pawn.WeaponInfo.Configure()
                    .AddTimeToReloadFactor(_reloadFactor);
            }
        }

        protected override void OnStop()
        {
            if (!GetTargetAs(out Pawn pawn))
            {
                return;
            }

            pawn.Configure()
                .RemoveForwardSpeedFactor(_speedFactor)
                .RemoveStrafeSpeedFactor(_speedFactor)
                .RemoveReverseSpeedFactor(_speedFactor)
                .RemoveTurnSpeedFactor(_speedFactor);

            if (pawn.WeaponInfo != null)
            {
                pawn.WeaponInfo.Configure()
                    .RemoveTimeToReloadFactor(_reloadFactor);
            }
        }
    }
}
