using HoverTanks.Entities;
using HoverTanks.Events;
using HoverTanks.Loadouts;
using HoverTanks.Networking;
using System.Collections.Generic;
using UnityEngine;

public class EntityManager : MonoBehaviour
{
	private sealed class EntityData
	{
		public readonly NetworkEntity Entity;
		public readonly HistoryBuffer<EntitySnapshot> History;

		public EntityData(NetworkEntity entity)
		{
			Entity = entity;

			if (entity.EntityType == EntityType.Pawn
				|| entity.EntityType == EntityType.Prop)
			{
				History = new HistoryBuffer<EntitySnapshot>(90);
			}
		}
	}

	// public readonly collections
	public static IReadOnlyDictionary<EntityId, Pawn> ActivePawns => _instance._activePawns;
	public static IReadOnlyDictionary<EntityId, ProjectileDirect> ActiveDirectProjectiles => _instance._activeDirectProjectiles;
	public static IReadOnlyDictionary<TeamId, Dictionary<EntityId, Pawn>> ActivePawnsByTeam => _instance._activePawnsByTeam;

	[SerializeField] PlayerPawnController _playerPawnControllerPrefab;

	private static EntityManager _instance;
	private static EntityId _nextUniqueEntityId;

	private const float RECORD_HISTORY_FPS = 30;
	private const float RECORD_HISTORY_INTERVAL = 1 / RECORD_HISTORY_FPS;
	private const float RECORD_HISTORY_INTERVAL_MS = RECORD_HISTORY_INTERVAL * 1000f;

	// capacity consts
	private const int ACTIVE_PAWNS_INITIAL_CAP = 20;
	private const int ACTIVE_DIRECT_PROJECTILES_INITIAL_CAP = 20;
	private const int ACTIVE_ENTITIES_INITIAL_CAP = 100;
	private const int ACTIVE_TEAMS_INITIAL_CAP = 5;

	// collections
	private readonly Dictionary<EntityId, Pawn> _activePawns = new Dictionary<EntityId, Pawn>(ACTIVE_PAWNS_INITIAL_CAP);
	private readonly Dictionary<EntityId, ProjectileDirect> _activeDirectProjectiles = new Dictionary<EntityId, ProjectileDirect>(ACTIVE_DIRECT_PROJECTILES_INITIAL_CAP);
	private readonly Dictionary<EntityId, EntityData> _activeEntities = new Dictionary<EntityId, EntityData>(ACTIVE_ENTITIES_INITIAL_CAP);
	private readonly Dictionary<TeamId, Dictionary<EntityId, Pawn>> _activePawnsByTeam = new Dictionary<TeamId, Dictionary<EntityId, Pawn>>(ACTIVE_TEAMS_INITIAL_CAP);

	private Dictionary<PawnClass, Pawn> _pawnPrefabs;
	private Dictionary<ProjectileClass, Projectile> _projectilePrefabs;
	private Dictionary<PickupClass, Pickup> _pickupPrefabs;
	private Dictionary<PropClass, Prop> _propPrefabs;

	private float _nextRecordHistoryTime;

	public static EntityId GetUniqueEntityId()
	{
		return ++_nextUniqueEntityId;
	}

	/// <summary>
	/// Clear all the EntityManager's references to created entities and reset unique id counter. Use when changing level.
	/// </summary>
	public static void Flush()
	{
		Log.Info(LogChannel.EntityManager, "Flush");

		_instance._activePawns.Clear();
		_instance._activePawnsByTeam.Clear();
		_instance._activeDirectProjectiles.Clear();
		_instance._activeEntities.Clear();

		ResetUniqueIdCounter();
	}

	/// <summary>
	/// Reset the unique id counter for entities.
	/// </summary>
	public static void ResetUniqueIdCounter()
	{
		_nextUniqueEntityId = default;
	}

	public static void DestroyAllEntities()
	{
		foreach (var data in _instance._activeEntities.Values)
		{
			if (data == null)
			{
				return;
			}

			Destroy(data.Entity.gameObject);
		}

		_instance._activePawns.Clear();
		_instance._activePawnsByTeam.Clear();
		_instance._activeDirectProjectiles.Clear();
		_instance._activeEntities.Clear();
	}

	public static void AcknowledgePrePlacedEntity(NetworkEntity entity, TeamId teamId)
	{
		entity.SetIdentity(ClientId.Invalid, PlayerId.Invalid, GetUniqueEntityId(), teamId);

		switch (entity.EntityType)
		{
			case EntityType.Pawn: _instance.RegisterPawn(entity as Pawn); break;
		}
	}

	public static bool TryGetActivePawnsByTeam(TeamId teamId, out IReadOnlyDictionary<EntityId, Pawn> pawns)
	{
		if (!_instance._activePawnsByTeam.TryGetValue(teamId, out var dict))
		{
			pawns = null;
			return false;
		}

		pawns = dict;
		return true;
	}

	public static bool TryGetEntitySnapshot(EntityId entityId, int latency, out EntitySnapshot snapshot)
	{
		snapshot = default;

		if (_instance._activeEntities.Count == 0)
		{
			return false;
		}

		if (!_instance._activeEntities.TryGetValue(entityId, out var data))
		{
			return false;
		}

		int numFramesBehind = Mathf.CeilToInt((float)latency / RECORD_HISTORY_INTERVAL_MS);

		if (numFramesBehind >= data.History.Size)
		{
			return false;
		}

		int index = (data.History.Head - numFramesBehind + data.History.Size) % data.History.Size;

		if (!data.History.TryGetState(index, out snapshot))
		{
			return false;
		}

		return true;
	}

	public static bool TryGetEntity(EntityId target, out NetworkEntity entity)
	{
		entity = null;

		if (!_instance._activeEntities.TryGetValue(target, out var data))
		{
			return false;
		}

		entity = data.Entity;

		return true;
	}

	public static bool IsEntityOfType(EntityId id, EntityType type)
	{
		switch (type)
		{
			case EntityType.Pawn: return ActivePawns.ContainsKey(id);
			case EntityType.Projectile: return ActiveDirectProjectiles.ContainsKey(id);
			case EntityType.Pickup: return ActiveDirectProjectiles.ContainsKey(id);
		}

		return false;
	}

	public static bool IsPawnAlive(EntityId id)
	{
		return ActivePawns.ContainsKey(id);
	}

	public static bool TryGetPawnBasicInfo(PawnClass key, out PawnBasicInfo info)
	{
		info = default;

		if (!_instance._pawnPrefabs.TryGetValue(key, out var prefab))
		{
			Log.Error(LogChannel.EntityManager, $"TryGetPawnBasicInfo - no prefab found for key: {key}");
			return false;
		}

		info = prefab.GetBasicInfo();

		return true;
	}

	public static bool TryGetProjectileBasicInfo(ProjectileClass key, out ProjectileBasicInfo info)
	{
		info = default;

		if (!_instance._projectilePrefabs.TryGetValue(key, out var prefab))
		{
			Log.Error(LogChannel.EntityManager, $"TryGetProjectileBasicInfo - no prefab found for key: {key}");
			return false;
		}

		info = prefab.GetProjectileBasicInfo();

		return true;
	}

	public static bool TryGetModulePrefab(ModuleClass id, out Module module)
	{
		if (id == ModuleClass.Invalid)
		{
			module = null;
			return false;
		}

		module = Resources.Load<Module>($"Modules/{id}");

		if (module == null)
		{
			Log.Error(LogChannel.EntityManager, $"TryGetModulePrefab - could not find prefab with id: {id}");
			return false;
		}

		return true;
	}

	public static bool TryGetAugmentPrefab(AugmentClass id, out Augment augment)
	{
		if (id == AugmentClass.Invalid)
		{
			augment = null;
			return false;
		}

		augment = Resources.Load<Augment>($"Augments/{id}");

		if (augment == null)
		{
			Log.Error(LogChannel.EntityManager, $"TryGetAugmentPrefab - could not find prefab with id: {id}");
			return false;
		}

		return true;
	}

	public static void GivePawnAuthority(Pawn pawn, ClientId targetClientId, PlayerId targetPlayerId)
	{
		if (!Server.IsActive)
		{
			return;
		}

		if (pawn == null)
		{
			return;
		}

		// abort if already has authority
		if (Server.DoesClientHaveEntityAuthority(targetClientId, pawn.identity.entityId)
			&& Server.GetPlayerIdsForClient(targetClientId).Contains(targetPlayerId))
		{
			return;
		}

		// remove current authority
		RemovePawnAuthority(pawn);

		// add server authority
		Server.RegisterEntityAuthority(targetClientId, pawn.identity.entityId);

		// notify of new authority
		using (var sendMsg = new AuthorityChangeMsg()
		{
			EntityId = pawn.identity.entityId,
			ClientId = targetClientId,
			PlayerId = targetPlayerId,
			Mode = AuthorityChangeMsg.ChangeMode.Give,
		})
        {
            ServerSend.ToAll(sendMsg);
        }
	}

	public static void RemovePawnAuthority(Pawn pawn)
	{
		if (!Server.IsActive)
		{
			return;
		}

		if (pawn == null)
		{
			return;
		}

		// abort if wouldn't remove authority
		if (!Server.DoesClientHaveEntityAuthority(pawn.identity.clientId, pawn.identity.entityId))
		{
			return;
		}

		// remove server authority
		Server.UnregisterEntityAuthority(pawn.identity.clientId, pawn.identity.entityId);

		// notify of authority removal
		using (var sendMsg = new AuthorityChangeMsg()
		{
			EntityId = pawn.identity.entityId,
			ClientId = ClientId.Invalid,
			PlayerId = PlayerId.Invalid,
			Mode = AuthorityChangeMsg.ChangeMode.Remove,
		})
        {
            ServerSend.ToAll(sendMsg);
        }
	}

	public void CreatePawn(PawnClass @class, ClientId clientId, PlayerId playerId, EntityId entityId, TeamId teamId, Vector3 position, float heading, byte healthModifier)
	{
		if (!_pawnPrefabs.TryGetValue(@class, out var prefab))
		{
			Log.Error(LogChannel.EntityManager, $"CreatePawn - no prefab found for class: {@class}");
			return;
		}

		Log.Info(LogChannel.EntityManager, $"CreatePawn - owner: [cid = {clientId}, pid = {playerId}], entity id: {entityId}");

        var pawn = Instantiate(prefab, position, JHelper.HeadingToRotation(heading));
		pawn.SetIdentity(clientId, playerId, entityId, teamId);

		if (healthModifier > 0)
		{
            pawn.Life.Configure().AdjustMaxHealth(healthModifier);
		}

		RegisterPawn(pawn);
		SmartControllerInit(pawn);
	}

	public void CreateProjectile(ProjectileClass @class, Vector3 position, float heading, EntityId entityId, EquipmentType ownerEquipmentType,
		PlayerId ownerPlayerId = PlayerId.Invalid, EntityId ownerEntityId = EntityId.Invalid, TeamId ownerTeamId = TeamId.None)
	{
		if (!_projectilePrefabs.TryGetValue(@class, out var prefab))
		{
			Log.Error(LogChannel.EntityManager, $"CreateProjectile - no prefab found for class: {@class}");
			return;
		}

		//Log.Info(LogChannel.EntityManager, $"CreateProjectile - entity id: {entityId}");

		var projectile = Instantiate(prefab, position, JHelper.HeadingToRotation(heading));
		projectile.SetIdentity(ClientId.Invalid, PlayerId.Invalid, entityId, TeamId.None);
		projectile.Init(new IdentityInfo(ownerEntityId, ownerPlayerId, ownerTeamId));

		if (projectile is ProjectileDirect projectileDirect)
		{
			// inform listeners
			LocalEvents.Invoke(new ProjectileDirectCreatedData()
			{
				Projectile = projectileDirect,
				OwnerEquipmentType = ownerEquipmentType,
			});

			_activeDirectProjectiles.Add(entityId, projectileDirect);
		}

		_activeEntities.Add(entityId, new EntityData(projectile));
	}

	public void CreatePickup(PickupClass @class, Vector3 position, Vector3 impulse, EntityId entityId)
	{
		if (!_pickupPrefabs.TryGetValue(@class, out var prefab))
		{
			Log.Error(LogChannel.EntityManager, $"CreatePickup - no prefab found for class: {@class}");
			return;
		}

		//Log.Info(LogChannel.EntityManager, $"CreatePickup - entity id: {entityId}");

		var pickup = Instantiate(prefab, position, Quaternion.identity);
		pickup.SetIdentity(ClientId.Invalid, PlayerId.Invalid, entityId, TeamId.None);
		pickup.AddImpulse(impulse);

		_activeEntities.Add(entityId, new EntityData(pickup));
	}

	public void CreateProp(PropClass @class, Vector3 position, float heading,
		EntityId entityId, PlayerId ownerPlayerId = PlayerId.Invalid, EntityId ownerEntityId = EntityId.Invalid, TeamId ownerTeamId = TeamId.None)
	{
		if (!_propPrefabs.TryGetValue(@class, out var prefab))
		{
			Log.Error(LogChannel.EntityManager, $"CreateProp - no prefab found for class: {@class}");
			return;
		}

		//Log.Info(LogChannel.EntityManager, $"CreateProp - entity id: {entityId}");

		var prop = Instantiate(prefab, position, JHelper.HeadingToRotation(heading));
		prop.SetIdentity(ClientId.Invalid, PlayerId.Invalid, entityId, TeamId.None);
		prop.Init(new IdentityInfo(ownerEntityId, ownerPlayerId, ownerTeamId));

		_activeEntities.Add(entityId, new EntityData(prop));
	}

	private void Awake()
	{
		if (_instance == null)
		{
			InitSingleton();
		}
		else
		{
			Destroy(this.gameObject);
		}
	}

    private void LateUpdate()
    {
		if (Time.time < _nextRecordHistoryTime)
		{
			return;
		}

		_nextRecordHistoryTime = Time.time + RECORD_HISTORY_INTERVAL;

        foreach (var elem in _activeEntities)
		{
			if (!GameClient.HasAuthority(elem.Value.Entity.identity)
				&& !Server.IsActive)
			{
				continue;
			}

			if (elem.Value.History == null)
			{
				continue;
			}

			elem.Value.History.Record(new EntitySnapshot(elem.Value.Entity.transform.position));
		}
    }

    private void InitSingleton()
	{
		_instance = this;

		// load assets
		LoadPawnPrefabs(out _pawnPrefabs);
		LoadProjectilePrefabs(out _projectilePrefabs);
		LoadPickupPrefabs(out _pickupPrefabs);
		LoadPropPrefabs(out _propPrefabs);

		// network events
		NetworkEvents.Subscribe<ClientDisconnectedMsg>(OnClientDisconnectedMsg);
		NetworkEvents.Subscribe<ServerShuttingDownMsg>(OnServerShuttingDownMsg);
		NetworkEvents.Subscribe<CreatePawnMsg>(OnCreatePawnMsg);
		NetworkEvents.Subscribe<CreateProjectileMsg>(OnCreateProjectileMsg);
		NetworkEvents.Subscribe<CreatePickupMsg>(OnCreatePickupMsg);
		NetworkEvents.Subscribe<CreatePropMsg>(OnCreatePropMsg);
		NetworkEvents.Subscribe<PawnStateMsg>(OnPawnStateMsg);
		NetworkEvents.Subscribe<DestroyEntityMsg>(OnDestroyEntityMsg);
		NetworkEvents.Subscribe<AuthorityChangeMsg>(OnAuthorityChangeMsg);

		// local events
		LocalEvents.Subscribe<DisconnectedData>(OnDisconnected);
		LocalEvents.Subscribe<EntityGarbageCollectedData>(OnEntityGarbageCollected);
	}

    private void LoadPawnPrefabs(out Dictionary<PawnClass, Pawn> prefabs)
	{
		var list = Resources.LoadAll<Pawn>("Pawns");
		prefabs = new Dictionary<PawnClass, Pawn>(list.Length);

		foreach (var item in list)
		{
			if (item.EntityType != EntityType.Pawn)
			{
				Log.Error(LogChannel.EntityManager, $"LoadPawnPrefabs - prefab '{item.name}' with class {item.PawnClass} had invalid entity type: {item.EntityType}");
				continue;
			}

			_pawnPrefabs.Add(item.PawnClass, item);
		}
	}

	private void LoadProjectilePrefabs(out Dictionary<ProjectileClass, Projectile> prefabs)
	{
		var list = Resources.LoadAll<Projectile>("Projectiles");
		prefabs = new Dictionary<ProjectileClass, Projectile>(list.Length);

		foreach (var item in list)
		{
			if (item.EntityType != EntityType.Projectile)
			{
				Log.Error(LogChannel.EntityManager, $"LoadProjectilePrefabs - prefab '{item.name}' with class {item.ProjectileClass} had invalid entity type: {item.EntityType}");
				continue;
			}

			_projectilePrefabs.Add(item.ProjectileClass, item);
		}
	}

	private void LoadPickupPrefabs(out Dictionary<PickupClass, Pickup> prefabs)
	{
		var list = Resources.LoadAll<Pickup>("Pickups");
		prefabs = new Dictionary<PickupClass, Pickup>(list.Length);

		foreach (var item in list)
		{
			if (item.EntityType != EntityType.Pickup)
			{
				Log.Error(LogChannel.EntityManager, $"LoadPickupPrefabs - prefab '{item.name}' with class {item.PickupClass} had invalid entity type: {item.EntityType}");
				continue;
			}

			_pickupPrefabs.Add(item.PickupClass, item);
		}
	}

	private void LoadPropPrefabs(out Dictionary<PropClass, Prop> prefabs)
	{
		var list = Resources.LoadAll<Prop>("Props");
		prefabs = new Dictionary<PropClass, Prop>(list.Length);

		foreach (var item in list)
		{
			if (item.EntityType != EntityType.Prop)
			{
				Log.Error(LogChannel.EntityManager, $"LoadPropPrefabs - prefab '{item.name}' with class {item.PropClass} had invalid entity type: {item.EntityType}");
				continue;
			}

			_propPrefabs.Add(item.PropClass, item);
		}
	}

	private void OnDisconnected(DisconnectedData data)
	{
		DestroyAllEntities();

		Log.Info(LogChannel.EntityManager, $"OnDisconnectedMsg - reason: {data.Reason}");
	}

	private void OnEntityGarbageCollected(EntityGarbageCollectedData data)
	{
		if (Server.IsActive)
		{
			Server.UnregisterEntityAuthority(data.Identity.clientId, data.Identity.entityId);
		}

		// remove from type dicts
		switch (data.EntityType)
		{
			case EntityType.Pawn:
			{
				if (!_activePawns.TryGetValue(data.Identity.entityId, out var pawn))
				{
					return;
				}

				UnregisterPawn(pawn);
			} break;

			case EntityType.Projectile:
			{
				_activeDirectProjectiles.Remove(data.Identity.entityId);
			} break;
		}

		// remove from general dict
		_activeEntities.Remove(data.Identity.entityId);
	}

	private void OnClientDisconnectedMsg(ClientDisconnectedMsg msg)
	{
		foreach (var playerId in msg.PlayerIds)
		{
			DestroyPawnsForPlayer(playerId);
		}

		Log.Info(LogChannel.EntityManager, $"OnClientDisconnectedMsg - id: {msg.ClientId}");
	}

	private void OnServerShuttingDownMsg(ServerShuttingDownMsg msg)
	{
		DestroyAllEntities();

		Log.Info(LogChannel.EntityManager, $"OnServerShuttingDownMsg");
	}

	private void OnCreatePawnMsg(CreatePawnMsg msg)
    {
		CreatePawn(msg.Class, msg.ClientId, msg.PlayerId, msg.EntityId, msg.TeamId, msg.Position, msg.Heading, msg.HealthModifier);
    }

	private void OnPawnStateMsg(PawnStateMsg msg)
	{
		if (!_activePawns.TryGetValue(msg.EntityId, out var pawn))
		{
			return;
		}

		pawn.SyncState(msg);
	}

	private void OnCreateProjectileMsg(CreateProjectileMsg msg)
	{
		CreateProjectile(msg.Class, msg.Position, msg.Heading, msg.EntityId, msg.OwnerEquipmentType, ownerEntityId: msg.OwnerEntityId);
	}

	private void OnCreatePickupMsg(CreatePickupMsg msg)
	{
		CreatePickup(msg.Class, msg.Position, msg.Impulse, msg.EntityId);
	}

	private void OnCreatePropMsg(CreatePropMsg msg)
	{
		CreateProp(msg.Class, msg.Position, msg.Heading, msg.EntityId);
	}

	private void OnDestroyEntityMsg(DestroyEntityMsg msg)
    {
		if (DestroyPawn(msg.EntityId))
		{
			return;
		}

		if (_activeDirectProjectiles.TryGetValue(msg.EntityId, out var projectile)
			&& projectile != null)
		{
			Destroy(projectile.gameObject);
		}
    }

	private void OnAuthorityChangeMsg(AuthorityChangeMsg msg)
    {
		if (!_activePawns.TryGetValue(msg.EntityId, out var pawn))
		{
			return;
		}

		Log.Info(LogChannel.EntityManager, $"OnAuthorityChangeMsg - {msg.Mode} eid({msg.EntityId}) to cid({msg.EntityId}), pid({msg.PlayerId})");

		pawn.identity.ChangeAuthority(msg.ClientId, msg.PlayerId);

		switch (msg.Mode)
		{
			case AuthorityChangeMsg.ChangeMode.Give:
			{
				SmartControllerInit(pawn);
			} break;

			case AuthorityChangeMsg.ChangeMode.Remove:
			{
				pawn.CleanupController();
			} break;
		}
    }

	private void SmartControllerInit(Pawn pawn)
	{
		if (pawn == null)
		{
			return;
		}

		if (pawn.identity.clientId == ClientId.Invalid
			|| pawn.identity.playerId == PlayerId.Invalid)
		{
			return;
		}

		// init AI control
		if (pawn.identity.playerId == PlayerId.AI
			&& Server.IsActive)
		{
			pawn.InitAIController();
		}
		// init player control
		else if (pawn.identity.clientId == GameClient.ClientId)
		{
			pawn.InitController(_instance._playerPawnControllerPrefab, true);
		}
	}

	private bool DestroyPawn(EntityId id)
	{
		if (!ActivePawns.TryGetValue(id, out var pawn))
		{
			return false;
		}

		if (pawn != null)
		{
			Destroy(pawn.gameObject);

			return true;
		}

		return false;
	}

	private void DestroyPawnsForPlayer(PlayerId playerId)
	{
		List<EntityId> pawnIds = new List<EntityId>();

		// find all pawns associated with connection
		foreach (var pawn in ActivePawns.Values)
		{
			if (pawn.identity.playerId != playerId)
			{
				continue;
			}

			pawnIds.Add(pawn.identity.entityId);
		}

		if (pawnIds.Count == 0)
		{
			return;
		}

		// destroy all the client's pawns
		for (int i = 0; i < pawnIds.Count; ++i)
		{
			DestroyPawn(pawnIds[i]);
		}
	}

	private void RegisterPawn(Pawn pawn)
	{
		if (pawn == null || _activePawns.ContainsKey(pawn.identity.entityId))
		{
			return;
		}

		// base initialization
		pawn.Init();

		// add to active dicts
		_activePawns.Add(pawn.identity.entityId, pawn);
		_activeEntities.Add(pawn.identity.entityId, new EntityData(pawn));

		// ensure team dict exists
		if (!_activePawnsByTeam.TryGetValue(pawn.identity.teamId, out var teamDict))
		{
			teamDict = new Dictionary<EntityId, Pawn>(ACTIVE_PAWNS_INITIAL_CAP);
			_activePawnsByTeam.Add(pawn.identity.teamId, teamDict);
		}

		// add to team dict
		teamDict.Add(pawn.identity.entityId, pawn);

		// inform listeners
		LocalEvents.Invoke(new PawnRegisteredData()
		{
			Pawn = pawn,
		});

		// loadout initialization
		pawn.InitLoadout();

		Log.Info(LogChannel.EntityManager, $"RegisterPawn - {pawn.name}, cid: {pawn.identity.clientId}, eid: {pawn.identity.entityId}, pid: {pawn.identity.playerId}, tid: {pawn.identity.teamId}");
	}

	private void UnregisterPawn(Pawn pawn)
	{
		if (pawn == null || !_activePawns.ContainsKey(pawn.identity.entityId))
		{
			return;
		}

		// remove from active dicts
		_activePawns.Remove(pawn.identity.entityId);
		_activeEntities.Remove(pawn.identity.entityId);

		// look for team dict
		if (_activePawnsByTeam.TryGetValue(pawn.identity.teamId, out var teamDict))
		{
			// remove from team dict
			teamDict.Remove(pawn.identity.entityId);

			// check if dict empty
			if (teamDict.Count == 0)
			{
				_activePawnsByTeam.Remove(pawn.identity.teamId);
			}
		}

		// inform listeners
		LocalEvents.Invoke(new PawnUnregisteredData()
		{
			Pawn = pawn,
		});
	}

	/*
    private void OnDrawGizmos()
    {
        foreach (var elem in _activeEntities.Values)
		{
			for (int i = 0; i < elem.History.Head; ++i)
			{
				elem.History.TryGetState(i, out var state);
				Debug.DrawLine(state.Position, state.Position + Vector3.up, Color.white, 10);
			}
		}
    }
	*/
}
