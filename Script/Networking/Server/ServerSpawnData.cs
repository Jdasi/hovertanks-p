using UnityEngine;

namespace HoverTanks.Networking
{
    public class ServerCreatePawnData
    {
        public PawnClass PawnClass;
        public ClientId ClientId;
        public PlayerId PlayerId;
        public TeamId TeamId;
        public Vector3 Position;
        public float Heading;
        public byte HealthModifier;
    }

	public class ServerCreateProjectileData
    {
        public ProjectileClass Class;
        public EquipmentType OwnerEquipmentType;
        public Vector3 Position;
        public float Heading;
        public NetworkIdentity Owner;
    }

    public class ServerCreatePickupData
    {
        public PickupClass Class;
        public Vector3 Position;
        public Vector3 Impulse;
    }

    public class ServerCreatePropData
    {
        public PropClass Class;
        public Vector3 Position;
        public NetworkIdentity Owner;
        public float Heading;
    }
}
