using UnityEngine;

namespace HoverTanks.Entities
{
    public class TurretVisualization : PawnVisualization
    {
        protected override void OnInit(Transform rotationTransform)
        {
            // life listeners
            _pawn.Life.OnDeathBasic += OnDeathBasic;
        }

        private void OnDeathBasic()
        {
            DynamicDecals.PaintExplosion(transform.position);
            JHelper.BlowIntoPieces(gameObject);
        }
    }
}
