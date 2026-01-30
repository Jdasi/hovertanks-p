using HoverTanks.Entities;
using HoverTanks.Networking;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Data for events that are sent locally only.
/// </summary>
namespace HoverTanks.Events
{
    public abstract class EventData : IDisposable
    {
        public void Dispose()
        {
            OnDispose();
        }

        protected virtual void OnDispose() { }
    }

    public class DisconnectedData : EventData
    {
        public string Reason;
    }

    public class EntityGarbageCollectedData : EventData
    {
        public EntityType EntityType;
        public NetworkIdentity Identity;
    }

    public class PawnRegisteredData : EventData
    {
        public Pawn Pawn;
    }

    public class PawnUnregisteredData : EventData
    {
        public Pawn Pawn;
    }

    public class EntityDamagedData : EventData
    {
        public IdentityInfo Attacker;
        public IdentityInfo Victim;
        public ElementType Element;
        public int Amount;
        public DamageLevel DamageLevel;
        public Vector3 Position;
    }

    public class EntityHealedData : EventData
    {
        public PlayerId PlayerId;
        public ElementType Element;
        public int Amount;
        public DamageLevel NewDamageLevel;
    }

    public class ServerPawnKilledData : EventData
    {
        public Pawn Victim;
        public IdentityInfo Killer;
        public ProjectileStats ProjectileStats;
        public ElementType Element;
    }

    public class ProjectileDirectCreatedData : EventData
    {
        public ProjectileDirect Projectile;
        public EquipmentType OwnerEquipmentType;
    }

    public class SceneChangeData : EventData
    {
        public string SceneName;
        public LoadSceneMode Mode;
    }

    public class PauseMenuToggledData : EventData { }

    public class ArcadeLevelClearedData : EventData
    {
        public Pawn[] AlivePlayers;
    }

    public class ArcadeLevelOutroStartedData : EventData { }

    public class ArcadeLevelPlayerExtractedData : EventData
    {
        public PlayerId PlayerId;
    }

    public class ArcadePlayerCreditsChangedData : EventData
    {
        public PlayerId PlayerId;
        public int NewAmount;
    }

    public class ArcadeModShopStartedData : EventData
    {
        public int Player1Credits;
        public int Player2Credits;
        public int Player3Credits;
        public int Player4Credits;
    }

    public class AddInteractContextData : EventData
    {
        public int Uid;
        public PlayerId PlayerId;
        public Action<PlayerId> Callback;
        public string Description;
    }

    public class RemoveInteractContextData : EventData
    {
        public int Uid;
        public PlayerId PlayerId;
    }

    public class InteractContextChangedData : EventData
    {
        public PlayerId PlayerId;
        public string Description;
    }
}
