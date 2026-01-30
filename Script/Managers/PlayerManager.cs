using HoverTanks.Events;
using HoverTanks.Networking;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    private static PlayerManager _instance;

    public static IReadOnlyDictionary<PlayerId, PlayerInfo> ActivePlayers => _instance._activePlayers;

    [SerializeField] PlayerInfo[] _playerInfos;

    private Dictionary<PlayerId, PlayerInfo> _activePlayers;

    public static void ChangePawnClass(PlayerId playerId, PawnClass pawnClass)
    {
	    if (!GameClient.PlayerIds.Contains(playerId))
	    {
		    return;
	    }

	    if (!_instance._activePlayers.TryGetValue(playerId, out var player))
	    {
		    return;
	    }

	    player.PawnClass = pawnClass;

        using (var sendMsg = new PawnClassSelectMsg()
        {
            PlayerId = playerId,
            PawnClass = pawnClass,
        })
        {
            ClientSend.Message(sendMsg, ClientSend.Modes.ToOthersIfHost);
        }
    }

    public static bool GetPlayerInfo(PlayerId playerId, out PlayerInfo info)
    {
        info = null;

        if (_instance._playerInfos == null)
        {
            Log.Error(LogChannel.PlayerManager, "GetPlayerInfo - _playerInfos was null");
            return false;
        }

        if (playerId == PlayerId.Invalid
            || playerId == PlayerId.AI)
        {
            return false;
        }

        foreach (var playerInfo in _instance._playerInfos)
        {
            if (playerInfo.PlayerId != playerId)
            {
                continue;
            }

            info = playerInfo;
            return true;
        }

        Log.Error(LogChannel.PlayerManager, $"GetPlayerInfo - failed to get PlayerInfo for {playerId}");
        return false;
    }

    private void Awake()
    {
        if (_instance == null)
        {
            InitSingleton();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitSingleton()
    {
        _instance = this;

        _activePlayers = new Dictionary<PlayerId, PlayerInfo>();

        var infos = _playerInfos;
        _playerInfos = new PlayerInfo[infos.Length];

        // prevent modifying the base object
        for (int i = 0; i < infos.Length; ++i)
        {
            _playerInfos[i] = Instantiate(infos[i]);
        }

        // network events
		NetworkEvents.Subscribe<PlayerRegisterMsg>(OnPlayerRegisterMsg);
		NetworkEvents.Subscribe<ClientConnectedMsg>(OnClientConnectedMsg);
		NetworkEvents.Subscribe<ClientDisconnectedMsg>(OnClientDisconnectedMsg);
		NetworkEvents.Subscribe<ServerShuttingDownMsg>(OnServerShuttingDownMsg);

		// local events
		LocalEvents.Subscribe<DisconnectedData>(OnDisconnected);
    }

    private void OnPlayerRegisterMsg(PlayerRegisterMsg msg)
	{
		foreach (var elem in msg.PlayerInfos)
		{
			_activePlayers.Add(elem.Key, elem.Value);
		}
	}

	private void OnClientConnectedMsg(ClientConnectedMsg msg)
	{
        if (!GetPlayerInfo(msg.PlayerId, out var playerInfo))
        {
            return;
        }

        playerInfo.DisplayName = msg.Username;
		_activePlayers.Add(msg.PlayerId, playerInfo);
	}

	private void OnClientDisconnectedMsg(ClientDisconnectedMsg msg)
	{
		foreach (var playerId in msg.PlayerIds)
		{
            if (!GetPlayerInfo(playerId, out var playerInfo))
            {
                continue;
            }

            playerInfo.Reset();

			_activePlayers.Remove(playerId);
		}
	}

    private void OnServerShuttingDownMsg(ServerShuttingDownMsg msg)
	{
		HandleSelfDisconnect();
	}

    private void OnDisconnected(DisconnectedData data)
	{
		HandleSelfDisconnect();
	}

    private void HandleSelfDisconnect()
	{
        _activePlayers.Clear();

        foreach (var info in _playerInfos)
        {
            info.Reset();
        }
	}
}
