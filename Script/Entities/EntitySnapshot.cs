using UnityEngine;

namespace HoverTanks.Entities
{
    public readonly struct EntitySnapshot
	{
		public readonly Vector3 Position;

		public EntitySnapshot(Vector3 pos)
		{
			Position = pos;
		}
	}
}
