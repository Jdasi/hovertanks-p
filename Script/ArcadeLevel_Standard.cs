using HoverTanks.Entities;
using HoverTanks.Events;
using HoverTanks.Networking;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

#if UNITY_EDITOR
[CustomEditor(typeof(ArcadeLevel_Standard))]
public class ArcadeLevel_Standard_Editor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var script = (ArcadeLevel_Standard)target;
        if (GUILayout.Button("Enumerate AI"))
        {
            script.Editor_EnumeratePrePlacedEntities();

            EditorUtility.SetDirty(script);
            EditorSceneManager.MarkSceneDirty(script.gameObject.scene);
        }
    }
}
#endif

public partial class ArcadeLevel_Standard : ArcadeLevel_Base
{
    private enum States
    {
        Idle,
        Playing,
        CreatingPortal,
        OpeningDoor,
        WaitForExtract,
        Outro,
        Finished,
    }

    [Header("Enemies")]
    [SerializeField] Pawn[] _prePlacedAI;

    [Header("Entry/Exit")]
    [SerializeField] ArcadeLevelDoor _topExit;
    [SerializeField] ArcadeLevelDoor _rightExit;
    [SerializeField] FloorArrow _exitArrow;

    [Space]
    [SerializeField] ArcadeLevelDoor _bottomEntrance;
    [SerializeField] ArcadeLevelDoor _leftEntrance;

    [Space]
    [SerializeField] Transform[] _customTeleportInLeftPoints;
    [SerializeField] Transform[] _customTeleportInBottomPoints;

    [Space]
    [SerializeField] ArcadeLevelPortal _portalPrefab;
    [SerializeField] Transform[] _portalSpawnPoints;

    protected override Transform[] PlayerSpawns { get; set; }

    private List<Pawn> _aiPawns = new List<Pawn>();
    private ArcadeLevelPortal _portal;
    private States _state;
    private float _customFinishTime;

    public override void RegisterPrePlacedEntities()
    {
        if (_prePlacedAI == null
            || _prePlacedAI.Length == 0)
        {
            return;
        }

        int multiplayerHealthBuff = (PlayerManager.ActivePlayers.Count - 1) * 6;

        foreach (var pawn in _prePlacedAI)
        {
            if (multiplayerHealthBuff > 0)
            {
                pawn.Life.Configure().AdjustMaxHealth(multiplayerHealthBuff);
            }

            EntityManager.AcknowledgePrePlacedEntity(pawn, GameManager.DefaultAITeamId);
            _aiPawns.Add(pawn);
        }
    }

    protected override bool TryInit()
    {
        // assign appropriate entry door
        switch (EntryDir)
        {
            case Direction.Up: EntryDoor = _bottomEntrance; break;
            case Direction.Right: EntryDoor = _leftEntrance; break;

            default:
            {
                Log.Error(LogChannel.ArcadeLevel, $"TryPrepareEntryExit - bad entry dir: {EntryDir}");
                return false;
            }
        }

        // validate teleport-in functionality
        if (Bit.IsSet((int)TeleportFlags, (int)TeleportFlags.TeleportIn))
        {
            // try use custom spawns
            PlayerSpawns = EntryDir == Direction.Up
                ? _customTeleportInBottomPoints
                : _customTeleportInLeftPoints;

            // fall back to door offset
            if (PlayerSpawns.Length < Server.MAX_PLAYERS
                && EntryDoor != null)
            {
                PlayerSpawns = EntryDoor.PlayerSpawns;
                var entryVector = JHelper.DirectionToVector(EntryDir);

                // shift spawns into the level
                foreach (var spawn in PlayerSpawns)
                {
                    spawn.position += entryVector * (ENTRY_TRAVEL_DIST + 1);
                }
            }

            if (PlayerSpawns.Length < Server.MAX_PLAYERS)
            {
                Log.Error(LogChannel.ArcadeLevel, $"TryPrepareEntryExit - not enough entry teleport positions for dir: {EntryDir}");
                return false;
            }

            // set intial camera pos
            var avgSpawnPos = PlayerSpawns.Select(spawn => spawn.position).ToArray().GetAveragePosition();
            GameCamera.SetPosition(avgSpawnPos + EntryDir.DirectionToVector() * 3);
            GameCamera.SetZoomLevel(GameCamera.ZoomLevel.Medium);
        }
        else if (EntryDoor == null)
        {
            Log.Error(LogChannel.ArcadeLevel, $"TryPrepareEntryExit - no entry door for dir: {EntryDir}");
            return false;
        }
        else
        {
            PlayerSpawns = EntryDoor.PlayerSpawns;

            if (PlayerSpawns == null
                || PlayerSpawns.Length < Server.MAX_PLAYERS)
            {
                Log.Error(LogChannel.ArcadeLevel, $"TryPrepareEntryExit - not enough player spawns on door for dir: {EntryDir}");
                return false;
            }

            // set intial camera pos
            float dist = EntryDir == Direction.Left || EntryDir == Direction.Right ? LEFT_RIGHT_CAMERA_PAN_DIST : UP_DOWN_CAMERA_PAN_DIST;
            GameCamera.SetPosition(EntryDoor.Position - EntryDir.DirectionToVector() * dist);
            GameCamera.SetZoomLevel(GameCamera.ZoomLevel.Close);

            // start with door open
            EntryDoor.SetOpen(true);
        }

        // validate teleport-out functionality
        if (Bit.IsSet((int)TeleportFlags, (int)TeleportFlags.TeleportOut))
        {
        }
        else
        {
            // assign appropriate exit door
            switch (ExitDir)
            {
                case Direction.Up: ExitDoor = _topExit; break;
                case Direction.Right: ExitDoor = _rightExit; break;

                default:
                {
                    Log.Error(LogChannel.ArcadeLevel, $"TryPrepareEntryExit - bad exit dir: {ExitDir}");
                    return false;
                }
            }

            if (ExitDoor == null)
            {
                Log.Error(LogChannel.ArcadeLevel, $"TryPrepareEntryExit - no exit door for dir: {ExitDir}");
                return false;
            }

            _exitArrow.transform.position = ExitDoor.Position - (ExitDir.DirectionToVector() * 4) + new Vector3(0, 0.2f);
            _exitArrow.transform.forward = JHelper.FlatDirection(_exitArrow.transform.position, ExitDoor.Position);

            // listen for extraction
            ExitDoor.PlayerEntered += PlayerExtracted;
        }

        _exitArrow.enabled = false;

        return true;
    }

    protected override void OnPawnRegistered(PawnRegisteredData data)
    {
        base.OnPawnRegistered(data);

        switch (data.Pawn.identity.playerId)
        {
            // add ai pawn
            case PlayerId.AI:
            {
                _aiPawns.Add(data.Pawn);
            } break;
        }
    }

    protected override void OnPawnUnregistered(PawnUnregisteredData data)
    {
        base.OnPawnUnregistered(data);

        switch (data.Pawn.identity.playerId)
        {
            case PlayerId.AI:
            {
                // try remove AI ref
                for (int i = 0; i < _aiPawns.Count; ++i)
                {
                    if (_aiPawns[i].identity.entityId != data.Pawn.identity.entityId)
                    {
                        continue;
                    }

                    _aiPawns.RemoveAt(i);
                    break;
                }
            } break;
        }
    }

    public override void SetAIActive(bool active)
    {
        if (!Server.IsActive)
        {
            return;
        }

        foreach (var pawn in _aiPawns)
        {
            if (pawn == null)
            {
                continue;
            }

            if (active)
            {
                EntityManager.GivePawnAuthority(pawn, ClientId.Server, PlayerId.AI);
            }
            else
            {
                EntityManager.RemovePawnAuthority(pawn);
            }
        }
    }

    protected override void OnStartedPlaying()
    {
        _state = States.Playing;
    }

    protected override void RunningOnFixedUpdate()
    {
        if (_state >= States.Playing
            && _state <= States.WaitForExtract)
        {
            // fail if all players dead
            if (PlayerDatas.Count == 0)
            {
                Result = ArcadeLevelResult.Failed;
                _state = States.Finished;

                return;
            }
        }

        // auto move all extracted players
        if (_state >= States.WaitForExtract)
        {
            Vector3 exitMoveDir = ExitDir.DirectionToVector();

            foreach (var data in PlayerDatas.Values)
            {
                if (!data.IsPawnExtracted)
                {
                    continue;
                }

                // stop auto-control eventually
                if (data.TimeSinceExtract >= 5)
                {
                    data.Pawn.ClearMove();
                    continue;
                }

                if (DidExtractViaPortal)
                {
                    exitMoveDir = JHelper.FlatDirection(data.Pawn.Position, _portal.transform.position);
                }

                data.Pawn.Move(exitMoveDir);
            }
        }

        switch (_state)
        {
            case States.Playing:
            {
                if (GameManager.fixedFrameCount % 5 != 0)
                {
                    return;
                }

                // wait for no AI remaining
                if (_aiPawns.Count > 0)
                {
                    return;
                }

                LocalEvents.Invoke(new ArcadeLevelClearedData
                {
                    AlivePlayers = PlayerDatas.Values
                        .Where(elem => elem.IsPawnAlive)
                        .Select(elem => elem.Pawn)
                        .ToArray(),
                });

                /*
                if (_portalSpawnPoints.Length > 0)
                {
                    var pos = _portalSpawnPoints.SelectRandom();
                    _portal = Instantiate(_portalPrefab, pos.position, pos.rotation);
                    _portal.PlayerInteracted += PlayerExtractedViaPortal;
                    _portal.transform.SetParent(transform);

                    _state = States.CreatingPortal;
                }
                else
                */
                {
                    if (ExitDoor != null
                        && !TeleportFlags.HasFlag(TeleportFlags.TeleportOut))
                    {
                        ExitDoor.Open();
                    }

                    _state = States.OpeningDoor;
                }
            } break;

            case States.CreatingPortal:
            {
                if (!_portal.IsFullyFormed())
                {
                    return;
                }

                if (ExitDoor != null
                    && !TeleportFlags.HasFlag(TeleportFlags.TeleportOut))
                {
                    ExitDoor.Open();
                }

                _state = States.OpeningDoor;
            } break;

            case States.OpeningDoor:
            {
                if (ExitDoor == null)
                {
                    return;
                }

                // wait for door to be open
                if (!ExitDoor.IsOpen)
                {
                    return;
                }

                _exitArrow.enabled = true;
                _state = States.WaitForExtract;
            } break;

            case States.WaitForExtract:
            {
                if (GameManager.fixedFrameCount % 5 != 0)
                {
                    return;
                }

                // check extracted states
                foreach (var data in PlayerDatas.Values)
                {
                    if (data.IsPawnAlive
                        && !data.IsPawnExtracted)
                    {
                        return;
                    }
                }

                LocalEvents.Invoke(new ArcadeLevelOutroStartedData());

                if (DidExtractViaPortal)
                {
                    _customFinishTime = Time.time + 3;
                }
                else
                {
                    float dist = EntryDir == Direction.Left || EntryDir == Direction.Right ? LEFT_RIGHT_CAMERA_PAN_DIST : UP_DOWN_CAMERA_PAN_DIST;
                    GameCamera.QueueAction(ExitDoor.Position + ExitDir.DirectionToVector() * dist, GameCamera.ZoomLevel.Close, 20, 3);
                }

                _state = States.Outro;
            } break;

            case States.Outro:
            {
                if (Time.time < _customFinishTime)
                {
                    return;
                }

                if (GameCamera.IsLerping)
                {
                    return;
                }

                Result = DidExtractViaPortal
                    ? ArcadeLevelResult.ProgressToModShop
                    : ArcadeLevelResult.ProgressToNext;

                _state = States.Finished;
            } break;
        }
    }

#if UNITY_EDITOR
    public void Editor_EnumeratePrePlacedEntities()
    {
        _prePlacedAI = FindObjectsByType<Pawn>(FindObjectsInactive.Exclude, FindObjectsSortMode.InstanceID);
    }
#endif
}
