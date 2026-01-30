using HoverTanks.Entities;
using HoverTanks.Networking;
using UnityEngine;

namespace HoverTanks.Loadouts
{
    public interface IEquipmentOwner
    {
        /// <summary>
        /// Identity of the owning entity.
        /// </summary>
        NetworkIdentity identity { get; }

        /// <summary>
        /// LifeForce of the entity. May be null.
        /// </summary>
        LifeForce Life { get; }

        /// <summary>
        /// The owner's sight position.
        /// </summary>
        Transform SightPoint { get; }

        /// <summary>
        /// Any mod slots that are enabled for this equipment.
        /// </summary>
        Slots GetEnabledModsForEquipment(EquipmentType equipmentType);

        void AddForce(Vector3 force);
        void AddImpulse(Vector3 impulse);
        float StationaryTimer { get; }
        bool IsTurboOn { get; }
    }

    public interface IModuleOwner : IEquipmentOwner
    {
        bool TryGetMountPoint(MountPoint point, out Transform mount);
        Color[] GetTintColors();
    }
}
