using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Data sent across the network.
/// </summary>
namespace HoverTanks.Networking
{
    public class WelcomeMsg : NetworkMessage
    {
        public override MessageId _MessageId => MessageId.Welcome;

        public ClientId AssignedClientId;
        public PlayerId AssignedPlayerId;
        public string Bulletin;

        public override void Write(Packet packet)
        {
            packet.Write(AssignedClientId);
            packet.Write(AssignedPlayerId);
            packet.Write(Bulletin);
        }

        public override void Read(Packet packet)
        {
            AssignedClientId = packet.ReadClientId();
            AssignedPlayerId = packet.ReadPlayerId();
            Bulletin = packet.ReadString();
        }
    }

    public class ServerPingMsg : NetworkMessage
    {
        public override MessageId _MessageId => MessageId.ServerPing;

        public float Timestamp;

        public override void Write(Packet packet)
        {
            packet.Write(Timestamp);
        }

        public override void Read(Packet packet)
        {
            Timestamp = packet.ReadFloat();
        }
    }

    public class ClientPongMsg : NetworkMessage
    {
        public override MessageId _MessageId => MessageId.ClientPong;

        public float ServerTimestamp;
        public float ClientTimestamp;

        public override void Write(Packet packet)
        {
            packet.Write(ServerTimestamp);
            packet.Write(ClientTimestamp);
        }

        public override void Read(Packet packet)
        {
            ServerTimestamp = packet.ReadFloat();
            ClientTimestamp = packet.ReadFloat();
        }
    }

    public class ServerPongMsg : NetworkMessage
    {
        public override MessageId _MessageId => MessageId.ServerPong;

        public float Timestamp;

        public override void Write(Packet packet)
        {
            packet.Write(Timestamp);
        }

        public override void Read(Packet packet)
        {
            Timestamp = packet.ReadFloat();
        }
    }

    public class WelcomeReceivedMsg : NetworkMessage
    {
        public override MessageId _MessageId => MessageId.WelcomeReceived;

        public ClientId ClientId;
        public PlayerId PlayerId;
        public string Username;

        public override void Write(Packet packet)
        {
            packet.Write(ClientId);
            packet.Write(PlayerId);
            packet.Write(Username);
        }

        public override void Read(Packet packet)
        {
            ClientId = packet.ReadClientId();
            PlayerId = packet.ReadPlayerId();
            Username = packet.ReadString();
        }
    }

	public class ClientConnectedMsg : NetworkMessage
	{
        public override MessageId _MessageId => MessageId.ClientConnected;

		public ClientId ClientId;
        public PlayerId PlayerId;
        public string Username;

        public override void Write(Packet packet)
        {
            packet.Write(ClientId);
            packet.Write(PlayerId);
            packet.Write(Username);
        }

        public override void Read(Packet packet)
        {
            ClientId = packet.ReadClientId();
            PlayerId = packet.ReadPlayerId();
            Username = packet.ReadString();
        }
    }

	public class ClientDisconnectedMsg : NetworkMessage
	{
        public override MessageId _MessageId => MessageId.ClientDisconnected;

		public ClientId ClientId;
        public PlayerId[] PlayerIds;

        public override void Write(Packet packet)
        {
            packet.Write(ClientId);
            packet.Write((byte)PlayerIds.Length);

            for (int i = 0; i < PlayerIds.Length; ++i)
            {
                packet.Write(PlayerIds[i]);
            }
        }

        public override void Read(Packet packet)
        {
            ClientId = packet.ReadClientId();
            var idsList = new List<PlayerId>();

            int numLocalPlayers = packet.ReadByte();

            for (int i = 0; i < numLocalPlayers; ++i)
            {
                idsList.Add(packet.ReadPlayerId());
            }

            PlayerIds = idsList.ToArray();
        }
    }

    public class ServerShuttingDownMsg : NetworkMessage
    {
        public override MessageId _MessageId => MessageId.ServerShuttingDown;
    }

	public class CreatePawnMsg : NetworkMessage
	{
        public override MessageId _MessageId => MessageId.CreatePawn;

        public PawnClass Class;
		public ClientId ClientId;
        public PlayerId PlayerId;
        public EntityId EntityId;
        public TeamId TeamId;
        public Vector3 Position;
        public float Heading;
        public byte HealthModifier;

        public override void Write(Packet packet)
        {
            packet.Write((byte)Class);
            packet.Write(ClientId);
            packet.Write(PlayerId);
            packet.Write(EntityId);
            packet.Write(TeamId);
            packet.Write(Position);
            packet.Write(Heading);
            packet.Write(HealthModifier);
        }

        public override void Read(Packet packet)
        {
            Class = (PawnClass)packet.ReadByte();
            ClientId = packet.ReadClientId();
            PlayerId = packet.ReadPlayerId();
            EntityId = packet.ReadEntityId();
            TeamId = packet.ReadTeamId();
            Position = packet.ReadVector3();
            Heading = packet.ReadFloat();
            HealthModifier = packet.ReadByte();
        }
    }

    public class KillEntityMsg : NetworkMessage
    {
        public override MessageId _MessageId => MessageId.KillEntity;

        public EntityId EntityId;

        public override void Write(Packet packet)
        {
            packet.Write(EntityId);
        }

        public override void Read(Packet packet)
        {
            EntityId = packet.ReadEntityId();
        }
    }

    public class CreateProjectileMsg : NetworkMessage
    {
        public override MessageId _MessageId => MessageId.CreateProjectile;

        public ProjectileClass Class;
        public EquipmentType OwnerEquipmentType;
        public EntityId EntityId;
        public EntityId OwnerEntityId;
        public Vector3 Position;
        public float Heading;

        public override void Write(Packet packet)
        {
            packet.Write(Class);
            packet.Write((byte)OwnerEquipmentType);
            packet.Write(EntityId);
            packet.Write(OwnerEntityId);
            packet.Write(Position);
            packet.Write(Heading);
        }

        public override void Read(Packet packet)
        {
            Class = packet.ReadProjectileType();
            OwnerEquipmentType = (EquipmentType)packet.ReadByte();
            EntityId = packet.ReadEntityId();
            OwnerEntityId = packet.ReadEntityId();
            Position = packet.ReadVector3();
            Heading = packet.ReadFloat();
        }
    }

    public class CreatePickupMsg : NetworkMessage
    {
        public override MessageId _MessageId => MessageId.CreatePickup;

        public PickupClass Class;
        public EntityId EntityId;
        public Vector3 Position;
        public Vector3 Impulse;

        public override void Write(Packet packet)
        {
            packet.Write((byte)Class);
            packet.Write(EntityId);
            packet.Write(Position);
            packet.Write(Impulse);
        }

        public override void Read(Packet packet)
        {
            Class = (PickupClass)packet.ReadByte();
            EntityId = packet.ReadEntityId();
            Position = packet.ReadVector3();
            Impulse = packet.ReadVector3();
        }
    }

    public class CreatePropMsg : NetworkMessage
    {
        public override MessageId _MessageId => MessageId.CreateProp;

        public PropClass Class;
        public EntityId EntityId;
        public Vector3 Position;
        public float Heading;

        public override void Write(Packet packet)
        {
            packet.Write((byte)Class);
            packet.Write(EntityId);
            packet.Write(Position);
            packet.Write(Heading);
        }

        public override void Read(Packet packet)
        {
            Class = (PropClass)packet.ReadByte();
            EntityId = packet.ReadEntityId();
            Position = packet.ReadVector3();
            Heading = packet.ReadFloat();
        }
    }

    public class EntityDamageMsg : NetworkMessage
    {
        public override MessageId _MessageId => MessageId.EntityDamage;

        public EntityId EntityId;
        public ElementType Element;
        public int Amount;
        public ElementFlags Flags;

        public override void Write(Packet packet)
        {
            packet.Write(EntityId);

            int data = (int)Element
                | Amount << 5;

            if (Bit.IsSet((int)Flags, (int)ElementFlags.IsAoe))
            {
                Bit.Set(ref data, Bits.Bit13);
            }

            if (Bit.IsSet((int)Flags, (int)ElementFlags.WasGlancingBlow))
            {
                Bit.Set(ref data, Bits.Bit14);
            }

            packet.Write((short)data);
        }

        public override void Read(Packet packet)
        {
            EntityId = packet.ReadEntityId();

            int data = packet.ReadShort();

            Element = (ElementType)(data & 31);
            Amount = (data >> 5) & 127;
            Flags = ElementFlags.None;

            if (Bit.IsSet(data, Bits.Bit13))
            {
                Flags |= ElementFlags.IsAoe;
            }

            if (Bit.IsSet(data, Bits.Bit14))
            {
                Flags |= ElementFlags.WasGlancingBlow;
            }
        }
    }

    public class EntityHealMsg : NetworkMessage
    {
        public override MessageId _MessageId => MessageId.EntityHeal;

        public EntityId EntityId;
        public ElementType Element;
        public int Amount;
        public bool IsAoe;

        public override void Write(Packet packet)
        {
            packet.Write(EntityId);

            int data = (int)Element
                | Amount << 5;

            if (IsAoe)
            {
                Bit.Set(ref data, Bits.Bit13);
            }

            packet.Write((short)data);
        }

        public override void Read(Packet packet)
        {
            EntityId = packet.ReadEntityId();

            int data = packet.ReadShort();

            Element = (ElementType)(data & 31);
            Amount = (data >> 5) & 127;
            IsAoe = Bit.IsSet(data, Bits.Bit13);
        }
    }

    public class EntityHeatLevelChangedMsg : NetworkMessage
    {
        public override MessageId _MessageId => MessageId.EntityHeatLevelChanged;

        public EntityId EntityId;
        public HeatLevel Level;
        public bool JustIncreasedToCritical;

        public override void Write(Packet packet)
        {
            packet.Write(EntityId);

            int data = (int)Level;

            if (JustIncreasedToCritical)
            {
                Bit.Set(ref data, Bits.Bit7);
            }

            packet.Write((byte)data);
        }

        public override void Read(Packet packet)
        {
            EntityId = packet.ReadEntityId();

            int data = packet.ReadByte();

            Level = (HeatLevel)(data & 127);

            if (Bit.IsSet(data, Bits.Bit7))
            {
                JustIncreasedToCritical = true;
            }
        }
    }

    public class EntityImpulseMsg : NetworkMessage
    {
        public override MessageId _MessageId => MessageId.EntityImpulse;

        public EntityId EntityId;
        public Vector3 Direction;
        public float Magnitude;

        public override void Write(Packet packet)
        {
            packet.Write(EntityId);

            float radians = Mathf.Atan2(Direction.z, Direction.x);
	        float degrees = (int)(radians * Mathf.Rad2Deg);

            packet.Write(degrees);
            packet.Write(Magnitude);
        }

        public override void Read(Packet packet)
        {
            EntityId = packet.ReadEntityId();

            float degrees = packet.ReadFloat();
            float radians = degrees * Mathf.Deg2Rad;
            Direction = new Vector3(Mathf.Cos(radians), 0, Mathf.Sin(radians));

            Magnitude = packet.ReadFloat();
        }
    }

	public class PawnStateMsg : NetworkMessage
	{
        public override MessageId _MessageId => MessageId.PawnState;

		public EntityId EntityId;
        public Vector3 Position;
        public float Heading;
        public Vector3 MoveDir;
        public Vector3 AimDir;
        public float TurnSpeed;
        public bool IsTurboOn;
        public bool HasPosition;

        public PawnStateMsg() { }

        public PawnStateMsg(bool usePosition = true)
        {
            HasPosition = usePosition;
        }

		public override void Write(Packet packet)
		{
            packet.Write(EntityId);
            packet.Write(Heading);

            int data = 0;

            if (IsTurboOn)
            {
                Bit.Set(ref data, Bits.Bit0);
            }

			if (MoveDir != default)
            {
                Bit.Set(ref data, Bits.Bit1);

                float radians = Mathf.Atan2(MoveDir.z, MoveDir.x);
	            int degrees = (int)(radians * Mathf.Rad2Deg);

                if (degrees < 0)
                {
                    Bit.Set(ref data, Bits.Bit3);
                }

                data |= Mathf.Abs(degrees) << 6;
            }

            if (AimDir != default)
            {
                Bit.Set(ref data, Bits.Bit2);

                float radians = Mathf.Atan2(AimDir.z, AimDir.x);
	            int degrees = (int)(radians * Mathf.Rad2Deg);

                if (degrees < 0)
                {
                    Bit.Set(ref data, Bits.Bit4);
                }

                data |= Mathf.Abs(degrees) << 14;
            }

            if (TurnSpeed != default)
            {
                if (TurnSpeed < 0)
                {
                    Bit.Set(ref data, Bits.Bit5);
                }

                data |= Mathf.Abs((short)TurnSpeed) << 22;
            }

            if (HasPosition)
            {
                Bit.Set(ref data, Bits.Bit31);
            }

            packet.Write(data);

            if (HasPosition)
            {
                packet.Write(Position);
            }
		}

        public override void Read(Packet packet)
        {
            EntityId = packet.ReadEntityId();
            Heading = packet.ReadFloat();

            int data = packet.ReadInt();

            IsTurboOn = Bit.IsSet(data, Bits.Bit0);

            // unpack move dir
            if (Bit.IsSet(data, Bits.Bit1))
            {
                int packedDegrees = (data >> 6) & 255;
                float radians = (float)(Bit.IsSet(data, Bits.Bit3) ? -packedDegrees : packedDegrees) * Mathf.Deg2Rad;
                MoveDir = new Vector3(Mathf.Cos(radians), 0, Mathf.Sin(radians));
            }
            else
            {
                MoveDir = default;
            }

            // unpack aim dir
            if (Bit.IsSet(data, Bits.Bit2))
            {
                int packedDegrees = (data >> 14) & 255;
                float radians = (float)(Bit.IsSet(data, Bits.Bit4) ? -packedDegrees : packedDegrees) * Mathf.Deg2Rad;
                AimDir = new Vector3(Mathf.Cos(radians), 0, Mathf.Sin(radians));
            }
            else
            {
                AimDir = default;
            }

			// unpack turn speed
            int packedTurnSpeed = (data >> 22) & 255;
            TurnSpeed = Bit.IsSet(data, Bits.Bit5) ? -packedTurnSpeed : packedTurnSpeed;

            // read position
            if (Bit.IsSet(data, Bits.Bit31))
            {
                HasPosition = true;
                Position = packet.ReadVector3();
            }
        }
    }

    public class PawnEquipmentActionMsg : NetworkMessage
    {
        public override MessageId _MessageId => MessageId.PawnEquipmentAction;

        public enum Actions
        {
            OnActivate = 0,
            OnDeactivate = 1,
            SpoolUp = 2,
            SpoolDown = 3,
            RechargeStart = 4,
            RechargeStop = 5,
            Proc = 6,
            ReloadStart = 7,
            ReloadFinish = 8,
        }

        public EntityId EntityId;
        public EquipmentType EquipmentType;
        public Actions Action;
        public Vector3 TargetPos;
        public EntityId TargetId;

        public override void Write(Packet packet)
        {
            packet.Write(EntityId);

            bool shouldWriteTargetPos = Action == Actions.Proc
                && TargetPos != default;

            bool shouldWriteTargetId = Action == Actions.Proc
                && TargetId != EntityId.Invalid;

            int data = 0;
            
            data |= (int)EquipmentType << 0;
            data |= (int)Action << 2;

            if (shouldWriteTargetPos)
            {
                Bit.Set(ref data, Bits.Bit6);
            }

            if (shouldWriteTargetId)
            {
                Bit.Set(ref data, Bits.Bit7);
            }

            packet.Write(data);

            if (shouldWriteTargetPos)
            {
                Vector2 targetPos = new Vector2(TargetPos.x, TargetPos.z);
                packet.Write(targetPos);
            }

            if (shouldWriteTargetId)
            {
                packet.Write(TargetId);
            }
        }

        public override void Read(Packet packet)
        {
            EntityId = packet.ReadEntityId();

            int data = packet.ReadInt();

            EquipmentType = (EquipmentType)(data & 3);
            Action = (Actions)((data >> 2) & 15);

            if (Bit.IsSet(data, Bits.Bit6))
            {
                Vector2 targetPos = packet.ReadVector2();
                TargetPos = new Vector3(targetPos.x, 0, targetPos.y);
            }

            if (Bit.IsSet(data, Bits.Bit7))
            {
                TargetId = packet.ReadEntityId();
            }
        }
    }

    public class AccoladeAwardedMsg : NetworkMessage
    {
        public override MessageId _MessageId => MessageId.AccoladeAwarded;

        public PlayerId PlayerId;
        public AccoladeType AccoladeType;

        public override void Write(Packet packet)
        {
            packet.Write(PlayerId);
            packet.Write(AccoladeType);
        }

        public override void Read(Packet packet)
        {
            PlayerId = packet.ReadPlayerId();
            AccoladeType = packet.ReadAccoladeType();
        }
    }

    public class DestroyEntityMsg : NetworkMessage
    {
        public override MessageId _MessageId => MessageId.DestroyEntity;

        public EntityId EntityId;

        public override void Write(Packet packet)
        {
            packet.Write(EntityId);
        }

        public override void Read(Packet packet)
        {
            EntityId = packet.ReadEntityId();
        }
    }

    public class PickupCollectedMsg : NetworkMessage
    {
        public override MessageId _MessageId => MessageId.PickupCollected;

        public EntityId EntityId;

        public override void Write(Packet packet)
        {
            packet.Write(EntityId);
        }

        public override void Read(Packet packet)
        {
            EntityId = packet.ReadEntityId();
        }
    }

    public class HomingTargetMsg : NetworkMessage
    {
        public override MessageId _MessageId => MessageId.HomingTarget;

        public EntityId OwnerId;
        public EntityId TargetId;

        public override void Write(Packet packet)
        {
            packet.Write(OwnerId);
            packet.Write(TargetId);
        }

        public override void Read(Packet packet)
        {
            OwnerId = packet.ReadEntityId();
            TargetId = packet.ReadEntityId();
        }
    }

    public class HitRequestMsg : NetworkMessage
    {
        public override MessageId _MessageId => MessageId.HitRequest;

        public HitRequestType Type;
        public EntityId OwnerId;
        public EntityId TargetId;
        public bool WasGlancingBlow;

        public override void Write(Packet packet)
        {
            int data = (int)Type;

            if (WasGlancingBlow)
            {
                Bit.Set(ref data, Bits.Bit5);
            }

            packet.Write((byte)data);
            packet.Write(OwnerId);
            packet.Write(TargetId);
        }

        public override void Read(Packet packet)
        {
            int data = packet.ReadByte();

            Type = (HitRequestType)(data & 31);

            if (Bit.IsSet(data, Bits.Bit5))
            {
                WasGlancingBlow = true;
            }

            OwnerId = packet.ReadEntityId();
            TargetId = packet.ReadEntityId();
        }
    }

    public class RamRequestMsg : NetworkMessage
    {
        public override MessageId _MessageId => MessageId.RamRequest;

        public EntityId OwnerId;
        public EntityId TargetId;
        public Vector3 Position;
        public float Heading;
        public float Magnitude;

        public override void Write(Packet packet)
        {
            packet.Write(OwnerId);
            packet.Write(TargetId);
            packet.Write(Position);
            packet.Write(Heading);
            packet.Write(Magnitude);
        }

        public override void Read(Packet packet)
        {
            OwnerId = packet.ReadEntityId();
            TargetId = packet.ReadEntityId();
            Position = packet.ReadVector3();
            Heading = packet.ReadFloat();
            Magnitude = packet.ReadFloat();
        }
    }

    public class AddStatusEffectMsg : NetworkMessage
    {
        public override MessageId _MessageId => MessageId.AddStatusEffect;

        public EntityId EntityId;
        public StatusClass Class;
        public int Handle;
        public float Duration;

        public override void Write(Packet packet)
        {
            packet.Write(EntityId);
            packet.Write((byte)Class);
            packet.Write(Handle);
            packet.Write(Duration);
        }

        public override void Read(Packet packet)
        {
            EntityId = packet.ReadEntityId();
            Class = (StatusClass)packet.ReadByte();
            Handle = packet.ReadInt();
            Duration = packet.ReadFloat();
        }
    }

    public class RemoveStatusEffectMsg : NetworkMessage
    {
        public override MessageId _MessageId => MessageId.RemoveStatusEffect;

        public EntityId EntityId;
        public int Handle;

        public override void Write(Packet packet)
        {
            packet.Write(EntityId);
            packet.Write(Handle);
        }

        public override void Read(Packet packet)
        {
            EntityId = packet.ReadEntityId();
            Handle = packet.ReadInt();
        }
    }

    public class AuthorityChangeMsg : NetworkMessage
    {
        public override MessageId _MessageId => MessageId.AuthorityChange;

        public enum ChangeMode
        {
            Give,
            Remove,
        }

        public EntityId EntityId;
        public ClientId ClientId;
        public PlayerId PlayerId;
        public ChangeMode Mode;

        public override void Write(Packet packet)
        {
            packet.Write(EntityId);

            int data = (int)ClientId << 0
                | (int)PlayerId << 3
                | (int)Mode << 6;

            packet.Write(data);
        }

        public override void Read(Packet packet)
        {
            EntityId = packet.ReadEntityId();

            int data = packet.ReadInt();

            ClientId = (ClientId)(data & 7);
            PlayerId = (PlayerId)((data >> 3) & 7);
            Mode = (ChangeMode)((data >> 6) & 1);
        }
    }

    public class PlayerRegisterMsg : NetworkMessage
    {
        public override MessageId _MessageId => MessageId.PlayerRegister;

        public IReadOnlyDictionary<PlayerId, PlayerInfo> PlayerInfos { get; private set; }

        public override void Write(Packet packet)
        {
            packet.Write((byte)PlayerManager.ActivePlayers.Count);

            foreach (var elem in PlayerManager.ActivePlayers)
            {
                packet.Write(elem.Key);
                packet.Write((byte)elem.Value.PawnClass);
                packet.Write(elem.Value.DisplayName);
            }
        }

        public override void Read(Packet packet)
        {
            var dict = new Dictionary<PlayerId, PlayerInfo>();

            int numPlayers = (int)packet.ReadByte();

            for (int i = 0; i < numPlayers; ++i)
            {
                PlayerId playerId = packet.ReadPlayerId();
                PawnClass pawnClass = (PawnClass)packet.ReadByte();
                string userName = packet.ReadString();

                if (!PlayerManager.GetPlayerInfo(playerId, out var playerInfo))
                {
                    continue;
                }

                playerInfo.PawnClass = pawnClass;
                playerInfo.DisplayName = userName;

                dict.Add(playerId, playerInfo);
            }

            PlayerInfos = dict;
        }
    }

    public class PawnClassSelectMsg : NetworkMessage
    {
        public override MessageId _MessageId => MessageId.PawnClassSelect;

        public PlayerId PlayerId;
        public PawnClass PawnClass;

        public override void Write(Packet packet)
        {
            packet.Write(PlayerId);
            packet.Write((byte)PawnClass);
        }

        public override void Read(Packet packet)
        {
            PlayerId = packet.ReadPlayerId();
            PawnClass = (PawnClass)packet.ReadByte();
        }
    }

    public class StateMsg : NetworkMessage
    {
        public override MessageId _MessageId => MessageId.State;

        public int State;

        public override void Write(Packet packet)
        {
            packet.Write(State);
        }

        public override void Read(Packet packet)
        {
            State = packet.ReadInt();
        }
    }

    public class ProceedMsg : NetworkMessage
    {
        public override MessageId _MessageId => MessageId.Proceed;
    }

    public class LoadSceneMsg : NetworkMessage
    {
        public override MessageId _MessageId => MessageId.LoadScene;

        public string SceneName;
        public LoadSceneMode Mode;

        public override void Write(Packet packet)
        {
            packet.Write(SceneName);
            packet.Write((byte)Mode);
        }

        public override void Read(Packet packet)
        {
            SceneName = packet.ReadString();
            Mode = (LoadSceneMode)packet.ReadByte();
        }
    }

    public class ArcadeLevelConfigMsg : NetworkMessage
    {
        public override MessageId _MessageId => MessageId.ArcadeLevelConfig;

        public TeleportFlags TeleportFlags;

        public override void Write(Packet packet)
        {
            int data = 0;

            if (Bit.IsSet((int)TeleportFlags, (int)TeleportFlags.TeleportIn))
            {
                Bit.Set(ref data, Bits.Bit3);
            }

            if (Bit.IsSet((int)TeleportFlags, (int)TeleportFlags.TeleportOut))
            {
                Bit.Set(ref data, Bits.Bit4);
            }

            packet.Write((byte)data);
        }

        public override void Read(Packet packet)
        {
            int data = packet.ReadByte();

            if (Bit.IsSet(data, Bits.Bit3))
            {
                TeleportFlags |= TeleportFlags.TeleportIn;
            }

            if (Bit.IsSet(data, Bits.Bit4))
            {
                TeleportFlags |= TeleportFlags.TeleportOut;
            }
        }
    }

    public class PawnRamStateChangeMsg : NetworkMessage
    {
        public override MessageId _MessageId => MessageId.PawnRamStateChange;

        public EntityId EntityId;
        public bool IsInRamState;

        public override void Write(Packet packet)
        {
            packet.Write(EntityId);
            packet.Write((byte)(IsInRamState ? 1 : 0));
        }

        public override void Read(Packet packet)
        {
            EntityId = packet.ReadEntityId();
            IsInRamState = packet.ReadByte() == 1;
        }
    }

    public class CreditsAwardedMsg : NetworkMessage
    {
        public override MessageId _MessageId => MessageId.CreditsAwarded;

        public PlayerId PlayerId;
        public AwardCreditsReason Reason;
        public int Amount;
        public bool HasPosition;
        public Vector3 Position;

        public override void Write(Packet packet)
        {
            packet.Write(PlayerId);

            int data = (int)Reason
                | Amount << 3;

            if (HasPosition)
            {
                Bit.Set(ref data, Bits.Bit15);
            }

            packet.Write((short)data);

            if (HasPosition)
            {
                packet.Write(Position);
            }
        }

        public override void Read(Packet packet)
        {
            PlayerId = packet.ReadPlayerId();

            int data = packet.ReadShort();

            Reason = (AwardCreditsReason)(data & 7);
            Amount = (data >> 3) & 2047;

            if (Bit.IsSet(data, Bits.Bit15))
            {
                HasPosition = true;
                Position = packet.ReadVector3();
            }
        }
    }

    public class EntityEventMsg : NetworkMessage
    {
        public override MessageId _MessageId => MessageId.EntityEvent;

        public EntityId EntityId;
        public byte EventId;

        public override void Write(Packet packet)
        {
            packet.Write(EntityId);
            packet.Write(EventId);
        }

        public override void Read(Packet packet)
        {
            EntityId = packet.ReadEntityId();
            EventId = packet.ReadByte();
        }
    }
}
