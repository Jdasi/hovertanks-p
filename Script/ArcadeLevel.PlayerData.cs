using HoverTanks.Entities;
using UnityEngine;

public partial class ArcadeLevel_Base
{
    protected class PlayerData
    {
        public Pawn Pawn { get; }
        public bool IsPawnAlive => Pawn != null && Pawn.Life.IsAlive;
        public bool IsPawnExtracted
        {
            get => _isPawnExtracted;
            set
            {
                if (_isPawnExtracted == value)
                {
                    return;
                }

                _isPawnExtracted = value;

                if (_isPawnExtracted)
                {
                    _extractedTime = Time.time;
                }
            }
        }
        public float TimeSinceExtract => Time.time - _extractedTime;
        public Vector3 TargetIntroPos { get; }

        private bool _isPawnExtracted;
        private float _extractedTime;

        public PlayerData(Pawn pawn, Direction entryDir, float travelDist)
        {
            Pawn = pawn;
            TargetIntroPos = pawn.Position + entryDir.DirectionToVector() * travelDist;
        }
    }
}
