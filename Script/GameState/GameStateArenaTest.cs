using HoverTanks.Entities;
using HoverTanks.Events;
using HoverTanks.Networking;
using System.Collections.Generic;
using UnityEngine;

namespace HoverTanks.GameState
{
	public class GameStateArenaTest : MonoBehaviour
	{
		private class SpawnData
		{
			public readonly PlayerId PlayerId;

			public bool WaitingOnCreate;
			public float NextRespawnTime;
			public Pawn Pawn;

			public SpawnData(PlayerId playerId)
			{
				PlayerId = playerId;
			}
		}

		[SerializeField] PawnClass _playerClass;
		[SerializeField] PawnClass _aiClass;
		[SerializeField] Transform[] _spawnPoints;
		[SerializeField] float _respawnDelay;

		private Dictionary<ClientId, SpawnData> _spawnDatas;

		private void Start()
		{
			_spawnDatas = new Dictionary<ClientId, SpawnData>();

			NetworkEvents.Subscribe<ClientConnectedMsg>(OnClientConnectedMsg);
			NetworkEvents.Subscribe<ClientDisconnectedMsg>(OnClientDisconnectedMsg);

			LocalEvents.Subscribe<DisconnectedData>(OnDisconnected);
			LocalEvents.Subscribe<PawnRegisteredData>(OnPawnRegistered);
			LocalEvents.Subscribe<ServerPawnKilledData>(OnPawnKilled);
		}

		private void OnDestroy()
		{
			NetworkEvents.Unsubscribe<ClientConnectedMsg>(OnClientConnectedMsg);
			NetworkEvents.Unsubscribe<ClientDisconnectedMsg>(OnClientDisconnectedMsg);

			LocalEvents.Unsubscribe<DisconnectedData>(OnDisconnected);
			LocalEvents.Unsubscribe<PawnRegisteredData>(OnPawnRegistered);
			LocalEvents.Unsubscribe<ServerPawnKilledData>(OnPawnKilled);
		}

		private void OnClientConnectedMsg(ClientConnectedMsg msg)
		{
			if (!Server.IsActive)
			{
				return;
			}

			if (_spawnDatas.ContainsKey(msg.ClientId))
			{
				Log.Error(LogChannel.GameStateTestBed, $"OnClientConnected - already acknowledged id: {msg.ClientId}");
				return;
			}

			Log.Info(LogChannel.GameStateTestBed, $"OnClientConnected - acknowledged cid: {msg.ClientId}, pid: {msg.PlayerId}");

			_spawnDatas.Add(msg.ClientId, new SpawnData(msg.PlayerId));

			// create all existing pawns on this client
            foreach (var pawn in EntityManager.ActivePawns.Values)
            {
				using (var sendMsg = new CreatePawnMsg()
                {
					Class = pawn.PawnClass,
                    ClientId = pawn.identity.clientId,
                    PlayerId = pawn.identity.playerId,
                    EntityId = pawn.identity.entityId,
                    TeamId = pawn.identity.teamId,
                    Position = pawn.transform.position,
                    Heading = JHelper.RotationToHeading(pawn.transform.rotation),
                })
			    {
				    ServerSend.ToClient(msg.ClientId, sendMsg);
			    }
            }
		}

		private void OnClientDisconnectedMsg(ClientDisconnectedMsg msg)
		{
			if (!Server.IsActive)
			{
				return;
			}

			if (!_spawnDatas.ContainsKey(msg.ClientId))
			{
				return;
			}

			_spawnDatas.Remove(msg.ClientId);
		}

		private void Update()
		{
			if (Server.IsActive)
			{
				if (Input.GetKeyDown(KeyCode.P))
				{
					var spawnPoint = GetRandomSpawnPoint();

					ServerSpawn.Pawn(new ServerCreatePawnData()
					{
						PawnClass = _aiClass,
						ClientId = ClientId.Server,
						PlayerId = PlayerId.AI,
						TeamId = GameManager.DefaultAITeamId,
						Position = spawnPoint.position,
						Heading = JHelper.RotationToHeading(spawnPoint.rotation),
					});
				}
				else if (Input.GetKeyDown(KeyCode.Alpha1))
				{
					foreach (var pawn in EntityManager.ActivePawns.Values)
					{
						EntityManager.RemovePawnAuthority(pawn);
					}
				}
				else if (Input.GetKeyDown(KeyCode.Alpha2))
				{
					foreach (var pawn in EntityManager.ActivePawns.Values)
					{
						EntityManager.GivePawnAuthority(pawn, ClientId.Server, PlayerId.One);
					}
				}
				else if (Input.GetKeyDown(KeyCode.Alpha3))
				{
					foreach (var pawn in EntityManager.ActivePawns.Values)
					{
						EntityManager.GivePawnAuthority(pawn, ClientId.Client1, PlayerId.Two);
					}
				}
			}

			if (Server.IsActive)
			{
				ServerUpdate();
			}
		}

		private void ServerUpdate()
		{
			if (_spawnPoints == null
				|| _spawnPoints.Length == 0)
			{
				return;
			}

			// handle respawn
			foreach (var elem in _spawnDatas)
			{
				if (elem.Value.WaitingOnCreate)
				{
					continue;
				}

				// not yet time to respawn
				if (Time.time < elem.Value.NextRespawnTime)
				{
					continue;
				}

				// still alive
				if (elem.Value.Pawn != null)
				{
					continue;
				}

				elem.Value.WaitingOnCreate = true;

				var spawnPoint = GetRandomSpawnPoint();

				ServerSpawn.Pawn(new ServerCreatePawnData()
				{
					PawnClass = _playerClass,
					ClientId = elem.Key,
					PlayerId = elem.Value.PlayerId,
					TeamId = TeamId.None,
					Position = spawnPoint.position,
					Heading = JHelper.RotationToHeading(spawnPoint.rotation),
				});
			}
		}

		private Transform GetRandomSpawnPoint()
		{
			if (_spawnPoints == null || _spawnPoints.Length == 0)
			{
				return null;
			}

			return _spawnPoints[Random.Range(0, _spawnPoints.Length)];
		}

		private void OnDisconnected(DisconnectedData data)
		{
			_spawnDatas?.Clear();
		}

		private void OnPawnRegistered(PawnRegisteredData data)
		{
			if (!Server.IsActive)
			{
				return;
			}

			if (data.Pawn.identity.playerId == PlayerId.Invalid
				|| data.Pawn.identity.playerId == PlayerId.AI)
			{
				return;
			}

			if (!_spawnDatas.TryGetValue(data.Pawn.identity.clientId, out var spawnData))
			{
				return;
			}

			spawnData.Pawn = data.Pawn;
		}

		private void OnPawnKilled(ServerPawnKilledData data)
		{
			if (!Server.IsActive)
			{
				return;
			}

			if (data.Victim.identity.playerId == PlayerId.Invalid
				|| data.Victim.identity.playerId == PlayerId.AI)
			{
				return;
			}

			if (!_spawnDatas.TryGetValue(data.Victim.identity.clientId, out var spawnData))
			{
				return;
			}

			spawnData.Pawn = null;
			spawnData.WaitingOnCreate = false;
			spawnData.NextRespawnTime = Time.time + _respawnDelay;
		}
	}
}
