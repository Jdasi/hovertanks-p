using UnityEngine;

namespace HoverTanks.Entities
{
    public struct InputData
    {
        public float Timestamp;
        public Vector3 MoveDir;
        public Vector3 AimDir;
        public bool IsTurboOn;
    }
}
