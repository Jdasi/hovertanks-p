using HoverTanks.ArcadeRoute;
using HoverTanks.Events;
using HoverTanks.Networking;
using HoverTanks.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HoverTanks.GameState
{
    public interface IArcadePlayerQueries
    {
        int GetPlayerCredits(PlayerId playerId);
        bool TryCreateModdingHandle(PlayerId playerId, out ArcadePlayerInfo.ModdingHandle moddingHandle);
    }

    public class GameStateArcade : GameState, IArcadePlayerQueries
    {
        private enum States
        {
            Invalid = -1,

            Idle,
            Loading,
            WaitingPreInit,
            Initializing,
            WaitingPreIntro,
            Intro,
            Starting,
            Playing,
            Defeat,
            WaitingPreUnload,
            Unloading,
        }

        private const int MAX_LIVES = 5;

        [Header("Parameters")]
        [SerializeField] ArcadeArcDefinition _arc;
        [SerializeField] ArcadeLevelDefinition _modShopScene;

        [Header("References")]
        [SerializeField] GameplayUI _gameplayUI;
        [SerializeField] GamePauseUI _gamePauseUI;
        [SerializeField] GameCamera _gameCamera;

        private ArcadeLevel_Base _arcadeLevel;
        private AsyncOperation _asyncOp;
        private List<ArcadePlayerInfo> _playerInfos;

        private int _levelIndex;
        private string _levelName;
        private TeleportFlags _teleportFlags;
        private ArcadeLevelResult _lastLevelResult;

        private States _state = States.Invalid;

        public int GetPlayerCredits(PlayerId playerId)
        {
            if (!TryGetPlayerInfo(playerId, out var playerInfo))
            {
                return 0;
            }

            return playerInfo.Credits;
        }

        public bool TryCreateModdingHandle(PlayerId playerId, out ArcadePlayerInfo.ModdingHandle handle)
        {
            if (!TryGetPlayerInfo(playerId, out var playerInfo))
            {
                handle = null;
                return false;
            }

            handle = playerInfo.CreateModdingHandle();
            return true;
        }

        protected override void OnStateEnter()
        {
            EntityManager.Flush();

            // init stat tracking
            _playerInfos = new List<ArcadePlayerInfo>(PlayerManager.ActivePlayers.Count);

            foreach (var player in PlayerManager.ActivePlayers)
            {
                _playerInfos.Add(new ArcadePlayerInfo(player.Key));
            }

            // init UI
            _gameplayUI.Init();

            // network events
            NetworkEvents.Subscribe<ArcadeLevelConfigMsg>(OnArcadeLevelInitMsg);
            NetworkEvents.Subscribe<LoadSceneMsg>(OnLoadSceneMsg);
            NetworkEvents.Subscribe<ProceedMsg>(OnProceedMsg);
            NetworkEvents.Subscribe<ClientDisconnectedMsg>(OnClientDisconnected);

            // local events
            LocalEvents.Subscribe<SceneChangeData>(OnSceneChanged);
            LocalEvents.Subscribe<ArcadeLevelClearedData>(OnArcadeLevelCleared);
            LocalEvents.Subscribe<ArcadeLevelOutroStartedData>(OnArcadeLevelOutroStarted);

            SetState(States.Idle);
        }

        private void OnDestroy()
        {
            foreach (var player in _playerInfos)
            {
                player.Cleanup();
            }

            // network events
            NetworkEvents.Unsubscribe<ArcadeLevelConfigMsg>(OnArcadeLevelInitMsg);
            NetworkEvents.Unsubscribe<LoadSceneMsg>(OnLoadSceneMsg);
            NetworkEvents.Unsubscribe<ProceedMsg>(OnProceedMsg);
            NetworkEvents.Unsubscribe<ClientDisconnectedMsg>(OnClientDisconnected);

            // local events
            LocalEvents.Unsubscribe<SceneChangeData>(OnSceneChanged);
            LocalEvents.Unsubscribe<ArcadeLevelClearedData>(OnArcadeLevelCleared);
            LocalEvents.Unsubscribe<ArcadeLevelOutroStartedData>(OnArcadeLevelOutroStarted);
        }

        private void Update()
        {
            switch (_state)
            {
                case States.Initializing: InitializingOnUpdate(); break;
                case States.Playing: PlayingOnUpdate(); break;
                case States.Unloading: UnloadingOnUpdate(); break;
            }
        }

        private void IdleOnEnter()
        {
            if (Server.IsActive)
            {
                StartCoroutine(WaitAndProceedRoutine(_state));
            }

            ClientSend.State((int)_state);
        }

        private void LoadingOnEnter()
        {
            // hide all dynamic decals
            DynamicDecals.SetVisible(false);

            // tell everyone to load the next arcade level
            if (Server.IsActive)
            {
                var currLevel = _lastLevelResult == ArcadeLevelResult.ProgressToModShop
                    ? _modShopScene
                    : _arc.Levels[_levelIndex];

                TeleportFlags teleportFlags;

                // handle teleport-in conditions
                if (_levelIndex == 0
                    || _lastLevelResult == ArcadeLevelResult.Failed
                    || _lastLevelResult == ArcadeLevelResult.ProgressToModShop
                    || _lastLevelResult == ArcadeLevelResult.ProgressFromModShop)
                {
                    teleportFlags = TeleportFlags.TeleportIn;
                }
                else
                {
                    teleportFlags = TeleportFlags.None;
                }

                // handle teleport-out conditions
                if (_levelIndex == _arc.Levels.Length - 1)
                {
                    teleportFlags |= TeleportFlags.TeleportOut;
                }

                using (var sendMsg = new ArcadeLevelConfigMsg()
                {
                    TeleportFlags = teleportFlags,
                })
                {
                    ServerSend.ToAll(sendMsg);
                }

                using (var sendMsg = new LoadSceneMsg()
                {
                    SceneName = currLevel.SceneName,
                    Mode = LoadSceneMode.Additive,
                })
                {
                    ServerSend.ToAll(sendMsg);
                }
            }
        }

        private void WaitingPreInitOnEnter()
        {
            if (Server.IsActive)
            {
                StartCoroutine(WaitAndProceedRoutine(_state));
            }

            ClientSend.State((int)_state);
        }

        private IEnumerator WaitAndProceedRoutine(States state)
        {
            yield return new WaitUntil(() => Server.DoAllClientsHaveState((int)state));

            Log.Info(LogChannel.GameStateArcade, $"WaitingRoutine - all clients at state: {state}");
            Server.DoSyncedProceed();
        }

        private void InitializingOnEnter()
        {
            var currLevel = _lastLevelResult == ArcadeLevelResult.ProgressToModShop
                ? _modShopScene
                : _arc.Levels[_levelIndex];

            _arcadeLevel.Init(this, _gameCamera, currLevel.EntryDir, currLevel.ExitDir, _teleportFlags);
        }

        private void InitializingOnUpdate()
        {
            if (!_arcadeLevel.IsInitialized)
            {
                return;
            }

            SetState(States.WaitingPreIntro);
        }

        private void WaitingPreIntroOnEnter()
        {
            if (Server.IsActive)
            {
                StartCoroutine(WaitAndProceedRoutine(_state));
            }

            ClientSend.State((int)_state);
        }

        private void IntroOnEnter()
        {
            StartCoroutine(IntroRoutine());
        }

        private IEnumerator IntroRoutine()
        {
            // arcade map run intro
            _arcadeLevel.StartIntro();

            // wait for arcade map to finish intro
            yield return new WaitUntil(() => _arcadeLevel.IsReadyToStart);

            if (!(_arcadeLevel is ArcadeLevel_ModShop))
            {
                _gameplayUI.RevealActivePlayerHUDs();
            }

            // wait for gameplay UI to be ready
            yield return new WaitUntil(() => !_gameplayUI.IsAnimatingHUDs);

            // wait for all clients to run intro
            if (Server.IsActive)
            {
                StartCoroutine(WaitAndProceedRoutine(_state));
            }

            ClientSend.State((int)_state);
        }

        private void StartingOnEnter()
        {
            // synced start
            Server.DoSyncedLogic((clientId, playerIds) =>
            {
                foreach (var playerId in playerIds)
                {
                    _arcadeLevel.TryGetPawnForPlayer(playerId, out var pawn);
                    EntityManager.GivePawnAuthority(pawn, clientId, playerId);
                }

                using (var sendMsg = new ProceedMsg())
			    {
				    ServerSend.ToClient(clientId, sendMsg);
			    }
            });
        }

        private void PlayingOnEnter()
        {
            if (Server.IsActive)
            {
                _arcadeLevel.SetAIActive(true);
            }

            _arcadeLevel.StartPlaying();

            // display gameplay UI
            _gameplayUI.Announce("Go!", 2);

            if (_arcadeLevel is ArcadeLevel_Standard)
            {
                _gameplayUI.RevealRemainingEnemiesHUD();
            }
        }

        private void PlayingOnUpdate()
        {
            // wait for outcome
            if (_arcadeLevel.Result == ArcadeLevelResult.Pending)
            {
                return;
            }

            _lastLevelResult = _arcadeLevel.Result;
            _arcadeLevel.SetAIActive(false);
            _gameplayUI.HideAllPlayerHUDs();
            _gameplayUI.HideRemainingEnemiesHUD();

            switch (_arcadeLevel.Result)
            {
                case ArcadeLevelResult.ProgressToNext:
                case ArcadeLevelResult.ProgressToModShop:
                {
                    ++_levelIndex;
                    SetState(States.WaitingPreUnload);
                } break;

                case ArcadeLevelResult.ProgressFromModShop:
                {
                    SetState(States.WaitingPreUnload);
                } break;

                case ArcadeLevelResult.Failed:
                {
                    SetState(States.Defeat);
                } break;
            }
        }

        private void DefeatOnEnter()
        {
            StartCoroutine(DefeatRoutine());
        }

        private IEnumerator DefeatRoutine()
        {
            _gameplayUI.Announce("Defeat", 3);

            // wait for announcement
            yield return new WaitForSeconds(3);

            //_gameplayUI.FadeInSeguePanel(1);

            // wait for fade
            yield return new WaitForSeconds(1);

            // re-position camera and fade back in
            _gameCamera.SetPosition(new Vector3(-50, 0, 0));
            //_gameplayUI.FadeOutSeguePanel(0);

            // re-load the stage
            SetState(States.WaitingPreUnload);
        }

        private void WaitingPreUnloadOnEnter()
        {
            if (Server.IsActive)
            {
                StartCoroutine(WaitAndProceedRoutine(_state));
            }

            ClientSend.State((int)_state);
        }

        private void UnloadingOnEnter()
        {
            EntityManager.DestroyAllEntities();
            DynamicDecals.Flush();

            _asyncOp = GameManager.UnloadSceneAsync(_levelName);
        }

        private void UnloadingOnUpdate()
        {
            if (!_asyncOp.isDone)
            {
                return;
            }

            _asyncOp = null;

            SetState(States.Idle);
        }

        private void SetState(States state)
        {
            if (_state == state)
            {
                return;
            }

            Log.Info(LogChannel.GameStateArcade, $"SetState - changing state: {_state} -> {state}");

            _state = state;

            // enters
            switch (_state)
            {
                case States.Idle: IdleOnEnter(); break;
                case States.Loading: LoadingOnEnter(); break;
                case States.WaitingPreInit: WaitingPreInitOnEnter(); break;
                case States.Initializing: InitializingOnEnter(); break;
                case States.WaitingPreIntro: WaitingPreIntroOnEnter(); break;
                case States.Intro: IntroOnEnter(); break;
                case States.Starting: StartingOnEnter(); break;
                case States.Playing: PlayingOnEnter(); break;
                case States.Defeat: DefeatOnEnter(); break;
                case States.WaitingPreUnload: WaitingPreUnloadOnEnter(); break;
                case States.Unloading: UnloadingOnEnter(); break;
            }
        }

        private bool TryGetPlayerInfo(PlayerId playerId, out ArcadePlayerInfo playerInfo)
        {
            for (int i = 0; i < _playerInfos.Count; ++i)
            {
                var info = _playerInfos[i];

                if (info.PlayerId == playerId)
                {
                    playerInfo = info;
                    return true;
                }
            }

            playerInfo = null;
            return false;
        }

        private void OnArcadeLevelInitMsg(ArcadeLevelConfigMsg msg)
        {
            _teleportFlags = msg.TeleportFlags;
        }

        private void OnLoadSceneMsg(LoadSceneMsg msg)
        {
            if (!msg.SceneName.StartsWith("Arcade_"))
            {
                return;
            }

            Log.Info(LogChannel.GameStateArcade, $"OnLoadSceneMsg - {msg.SceneName}");

            _levelName = msg.SceneName;
        }

        private void OnProceedMsg(ProceedMsg data)
        {
            switch (_state)
            {
                case States.Idle: SetState(States.Loading); break;
                case States.WaitingPreInit: SetState(States.Initializing); break;
                case States.WaitingPreIntro: SetState(States.Intro); break;
                case States.Intro: SetState(States.Starting); break;
                case States.Starting: SetState(States.Playing); break;
                case States.WaitingPreUnload: SetState(States.Unloading); break;
            }
        }

        private void OnClientDisconnected(ClientDisconnectedMsg msg)
        {
            _gameplayUI.HidePlayerHUDs(msg.PlayerIds);

            foreach (var id in msg.PlayerIds)
            {
                for (int i = _playerInfos.Count - 1; i >= 0; --i)
                {
                    var player = _playerInfos[i];

                    if (player.PlayerId != id)
                    {
                        continue;
                    }

                    player.Cleanup();
                    _playerInfos.RemoveAt(i);
                }
            }
        }

        private void OnSceneChanged(SceneChangeData data)
        {
            if (data.Mode != LoadSceneMode.Additive)
            {
                return;
            }

            if (data.SceneName != _levelName)
            {
                return;
            }

            Log.Info(LogChannel.GameStateArcade, $"OnSceneChanged - {data.SceneName}");

            EntityManager.Flush();

            _arcadeLevel = FindFirstObjectByType<ArcadeLevel_Base>();

            if (_arcadeLevel == null)
            {
                Log.Error(LogChannel.GameStateArcade, $"OnSceneChanged - no arcade map found");
                return;
            }

            _gameplayUI.ResetRemainingEnemyCount();
            _arcadeLevel.RegisterPrePlacedEntities();

            // bring all dynamic decals to front
            DynamicDecals.SetVisible(true);

            SetState(States.WaitingPreInit);
        }

        private void OnArcadeLevelCleared(ArcadeLevelClearedData _)
        {
            _gameplayUI.HideRemainingEnemiesHUD();
        }

        private void OnArcadeLevelOutroStarted(ArcadeLevelOutroStartedData _)
        {
            _gameplayUI.HideAllPlayerHUDs();
        }
    }
}
