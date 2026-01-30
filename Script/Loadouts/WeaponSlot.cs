using System;
using UnityEngine;

namespace HoverTanks.Loadouts
{
    [Serializable]
	public struct WeaponSlot
    {
        public Transform[] ShootPoints;
        public Weapon Prefab;
    }
}
