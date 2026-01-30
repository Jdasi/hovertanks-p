using HoverTanks.Entities;
using HoverTanks.Events;
using HoverTanks.GameState;
using HoverTanks.Networking;
using System.Collections.Generic;
using UnityEngine;

public abstract partial class ArcadeLevel_Base : MonoBehaviour
{
    private enum IntroStates
    {
        Initializing,
        WaitingForPlayerPawns,
        Idle,

        // teleport in
        TeleportingPlayers,

        // move in
        MovingPlayers,
        ClosingDoor,

        // common
        WaitingForGameCamera,
        WaitingToStart,
        Main,
    }

    public bool IsInitialized => _introState == IntroStates.Idle;
    public bool IsReadyToStart => _introState == IntroStates.WaitingToStart;
    public ArcadeLevelResult Result { get; protected set; }

    [Header("Parameters")]
    [SerializeField] GameObject _teleportPrefab;

    [Header("References")]
    [SerializeField] Transform _unitCatcher;
    [SerializeField] protected ArcadeLevelDoor EntryDoor;
    [SerializeField] protected ArcadeLevelDoor ExitDoor;

    [Header("Debug")]
    [SerializeField] GameObject _tempCamera;

    protected abstract Transform[] PlayerSpawns { get; set; }

    protected IArcadePlayerQueries PlayerQueries { get; private set; }
    protected GameCamera GameCamera { get; private set; }
    protected IReadOnlyDictionary<PlayerId, PlayerData> PlayerDatas => _playerDatas;
    protected Direction EntryDir { get; private set; }
    protected Direction ExitDir { get; private set; }
    protected TeleportFlags TeleportFlags { get; private set; }
    protected bool DidExtractViaPortal { get; private set; }

    protected const float ENTRY_TRAVEL_DIST = 15;
    protected const float LEFT_RIGHT_CAMERA_PAN_DIST = 27f;
    protected const float UP_DOWN_CAMERA_PAN_DIST = 21f;
    protected const float DELAY_BEFORE_TELEPORT = 0.35f;
    protected const float TELEPORT_DELAY_BEFORE_MOVE = 0.8f;
    protected const float TELEPORT_NEXT_OFFSET = 0.5f;
    protected const float WATCH_TELEPORTS_DELAY = 0.4f;

    private Dictionary<PlayerId, PlayerData> _playerDatas;
    private List<TeleportProcess> _teleportProcesses;

    private float _stateProgressCountdown;
    private IntroStates _introState;

    public virtual void RegisterPrePlacedEntities() { }

    public void Init(IArcadePlayerQueries playerQueries, GameCamera gameCamera, Direction entryDir, Direction exitDir, TeleportFlags teleportFlags)
    {
        if (_introState != IntroStates.Initializing)
        {
            return;
        }

        PlayerQueries = playerQueries;
        GameCamera = gameCamera;
        _tempCamera.SetActive(false);
        _playerDatas = new Dictionary<PlayerId, PlayerData>();

        EntryDir = entryDir;
        ExitDir = exitDir;
        TeleportFlags = teleportFlags;

        if (!TryInit())
        {
            return;
        }

        if (Server.IsActive)
        {
            SpawnPlayers(teleportFlags);
        }

        _introState = IntroStates.WaitingForPlayerPawns;
    }

    public bool TryGetPawnForPlayer(PlayerId playerId, out Pawn pawn)
    {
        pawn = null;

        if (!_playerDatas.TryGetValue(playerId, out var data))
        {
            return false;
        }

        pawn = data.Pawn;

        return true;
    }

    public void StartIntro()
    {
        if (_introState != IntroStates.Idle)
        {
            return;
        }

        // teleport in to level
        if (Bit.IsSet((int)TeleportFlags, (int)TeleportFlags.TeleportIn))
        {
            _teleportProcesses = new List<TeleportProcess>(_playerDatas.Count);
            float teleportTime = Time.time + DELAY_BEFORE_TELEPORT;

            foreach (var elem in _playerDatas)
            {
                _teleportProcesses.Add(new TeleportProcess(elem.Value.Pawn, _teleportPrefab, teleportTime));
                teleportTime += TELEPORT_NEXT_OFFSET;
            }

            GameCamera.QueueAction(GameCamera.transform.position, GameCamera.ZoomLevel.Close, customZoomSpeed: 2.5f);
            _introState = IntroStates.TeleportingPlayers;
        }
        // move in to level
        else
        {
            // disable ramming for a short time
            foreach (var data in _playerDatas.Values)
            {
                data.Pawn.StatusEffectManager.LocalAdd(StatusClass.DisableRam, 4);
            }

            // start pan to center
            GameCamera.QueueAction(EntryDoor.Position + (EntryDir.DirectionToVector() * 3), GameCamera.ZoomLevel.Close, 15);
            GameCamera.QueueAction(Vector3.zero, GameCamera.ZoomLevel.Default, 15, 4.5f);

            _introState = IntroStates.MovingPlayers;
        }
    }

    public void StartPlaying()
    {
        if (_introState != IntroStates.WaitingToStart)
        {
            return;
        }

        _introState = IntroStates.Main;
        OnStartedPlaying();
    }

    public virtual void SetAIActive(bool active) { }

    private void Awake()
    {
        LocalEvents.Subscribe<PawnRegisteredData>(OnPawnRegistered);
        LocalEvents.Subscribe<PawnUnregisteredData>(OnPawnUnregistered);
    }

    private void OnDestroy()
    {
        LocalEvents.Unsubscribe<PawnRegisteredData>(OnPawnRegistered);
        LocalEvents.Unsubscribe<PawnUnregisteredData>(OnPawnUnregistered);
    }

    protected virtual void OnPawnRegistered(PawnRegisteredData data)
    {
        var playerId = data.Pawn.identity.playerId;

        switch (playerId)
        {
            // add player data
            case PlayerId.One:
            case PlayerId.Two:
            case PlayerId.Three:
            case PlayerId.Four:
            {
                _playerDatas.Add(playerId, new PlayerData(data.Pawn, EntryDir, ENTRY_TRAVEL_DIST));
            } break;
        }
    }

    protected virtual void OnPawnUnregistered(PawnUnregisteredData data)
    {
        var playerId = data.Pawn.identity.playerId;

        switch (playerId)
        {
            case PlayerId.One:
            case PlayerId.Two:
            case PlayerId.Three:
            case PlayerId.Four:
            {
                // try remove player data ref
                _playerDatas.Remove(playerId);

                // try remove teleport process for player
                if (_teleportProcesses != null)
                {
                    int index = _teleportProcesses.FindIndex(elem => elem.PlayerId == playerId);

                    if (index >= 0)
                    {
                        _teleportProcesses.RemoveAt(index);
                    }
                }
            } break;
        }
    }

    private void SpawnPlayers(TeleportFlags teleportFlags)
    {
        if (!Server.IsActive)
        {
            return;
        }

        var playerPawnInfos = new List<(PlayerId, PawnClass, float)>();

        // get pawn class speeds
        foreach (var player in PlayerManager.ActivePlayers)
        {
            if (!EntityManager.TryGetPawnBasicInfo(player.Value.PawnClass, out var info))
            {
                continue;
            }

            playerPawnInfos.Add((player.Key, player.Value.PawnClass, info.ForwardSpeed));
        }

        // sort by slowest pawn first
        playerPawnInfos.Sort((a, b) => b.Item3.CompareTo(a.Item3));

        // spawn players without authority
        for (int i = 0; i < playerPawnInfos.Count; ++i)
        {
            var player = playerPawnInfos[i];
            var spawnPoint = PlayerSpawns[i];
            var spawnPos = spawnPoint.position;

            if (Bit.IsSet((int)teleportFlags, (int)TeleportFlags.TeleportIn))
            {
                // move under level for now
                spawnPos.y = _unitCatcher.position.y + 2;
            }

            ServerSpawn.Pawn(new ServerCreatePawnData()
            {
                ClientId = ClientId.Invalid,
                PlayerId = player.Item1,
                TeamId = TeamId.Alpha,
                Position = spawnPos,
                Heading = JHelper.RotationToHeading(spawnPoint.rotation),
                PawnClass = player.Item2,
            });
        }
    }

    private void Update()
    {
        switch (_introState)
        {
            case IntroStates.WaitingForPlayerPawns:
            {
                if (GameManager.fixedFrameCount % 5 != 0)
                {
                    return;
                }

                if (_playerDatas.Count < PlayerManager.ActivePlayers.Count)
                {
                    return;
                }

                _introState = IntroStates.Idle;
            } break;

            case IntroStates.TeleportingPlayers:
            {
                bool allPlayersTeleported = true;

                foreach (var process in _teleportProcesses)
                {
                    if (process.IsDone)
                    {
                        continue;
                    }

                    allPlayersTeleported = false;
                    process.Run();
                }

                if (!allPlayersTeleported)
                {
                    return;
                }

                if (_stateProgressCountdown <= 0)
                {
                    _stateProgressCountdown = WATCH_TELEPORTS_DELAY;
                }

                _stateProgressCountdown -= Time.deltaTime;

                if (_stateProgressCountdown > 0)
                {
                    return;
                }

                _teleportProcesses = null;
                GameCamera.QueueAction(Vector3.zero, GameCamera.ZoomLevel.Default, 15, 4.5f);
                _introState = IntroStates.WaitingForGameCamera;
            } break;

            case IntroStates.MovingPlayers:
            {
                Vector3 entryAimDir = EntryDir.DirectionToVector();
                int numEntered = 0;

                foreach (var data in _playerDatas.Values)
                {
                    float dist = JHelper.FlatDistance(data.Pawn.Position, data.TargetIntroPos);

                    // check if entered
                    if (dist <= 2f)
                    {
                        if (Server.IsActive)
                        {
                            data.Pawn.ClearMove();
                            data.Pawn.StopAiming();
                            data.Pawn.StopTurbo();
                        }

                        ++numEntered;
                    }
                    // control inside
                    else
                    {
                        if (Server.IsActive)
                        {
                            data.Pawn.Move(JHelper.FlatDirection(data.Pawn.Position, data.TargetIntroPos));
                            data.Pawn.StartAiming(data.Pawn.Position + entryAimDir);

                            // manage turbo
                            if (dist < 5f)
                            {
                                data.Pawn.StopTurbo();
                            }
                            else
                            {
                                data.Pawn.StartTurbo();
                            }
                        }
                    }
                }

                if (numEntered >= _playerDatas.Count)
                {
                    if (EntryDoor != null)
                    {
                        EntryDoor.Close();
                        _introState = IntroStates.ClosingDoor;
                    }
                    else
                    {
                        _introState = IntroStates.WaitingForGameCamera;
                    }
                }
            } break;

            case IntroStates.ClosingDoor:
            {
                // wait for door to be closed
                if (EntryDoor.IsOpen)
                {
                    return;
                }

                _introState = IntroStates.WaitingForGameCamera;
            } break;

            case IntroStates.WaitingForGameCamera:
            {
                if (GameCamera.IsLerping)
                {
                    return;
                }

                _introState = IntroStates.WaitingToStart;
            } break;

            case IntroStates.Main:
            {
                RunningOnUpdate();
            } break;
        }
    }

    private void FixedUpdate()
    {
        switch (_introState)
        {
            case IntroStates.Main:
            {
                RunningOnFixedUpdate();
            } break;
        }
    }

    protected void PlayerExtracted(PlayerId playerId)
    {
        if (_introState < IntroStates.Main)
        {
            return;
        }

        if (!PlayerDatas.TryGetValue(playerId, out var data))
        {
            return;
        }

        data.IsPawnExtracted = true;
        data.Pawn.CleanupController();

        LocalEvents.Invoke(new ArcadeLevelPlayerExtractedData()
        {
            PlayerId = playerId,
        });
    }

    protected void PlayerExtractedViaPortal(PlayerId playerId)
    {
        if (!Server.IsActive)
        {
            return;
        }

        if (!PlayerDatas.TryGetValue(playerId, out var data))
        {
            return;
        }

        // TODO communicate to all clients
            // need to play a particle effect
            // and delete the pawn

        DidExtractViaPortal = true;
        PlayerExtracted(playerId);
    }

    protected abstract bool TryInit();
    protected abstract void OnStartedPlaying();

    protected virtual void RunningOnUpdate() { }
    protected virtual void RunningOnFixedUpdate() { }
}
