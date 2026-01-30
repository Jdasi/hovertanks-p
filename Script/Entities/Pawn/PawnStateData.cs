using UnityEngine;

namespace HoverTanks.Entities
{
    public struct PawnStateData
    {
        public Vector3 Position;
        public Quaternion Rotation;

        public void Refresh(Vector3 position, Quaternion rotation)
        {
            Position = position;
            Rotation = rotation;
        }
    }
}
