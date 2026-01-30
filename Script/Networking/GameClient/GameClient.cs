using HoverTanks.Events;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace HoverTanks.Networking
{
    public partial class GameClient : MonoBehaviour
    {
        public static string IP { get => _instance._ip; set => _instance._ip = value; }
        public static int Port => _instance._port;
        public static int Latency
        {
            get => _instance._latencyHistory.Average;
            set => _instance._latencyHistory.Record(value);
        }
        public static bool IsConnecting { get; private set; }
        public static bool IsConnected
        {
            get
            {
                if (_instance == null)
                {
                    return false;
                }

                if (_instance._tcp == null)
                {
                    return false;
                }

                if (_instance._tcp.Socket == null)
                {
                    return false;
                }

                if (_instance._tcp.Socket.Client == null)
                {
                    return false;
                }

                return _instance._tcp.Socket.Connected;
            }
        }

        public static ClientId ClientId;
        public static List<PlayerId> PlayerIds;
        public static string Username;
        public static PacketHandler<Packet> PacketHandler;
        public static int FakeLatency => _instance._fakeLatency;
        public static float LastPingTimestamp;

        [SerializeField] string _ip = "127.0.0.1";
        [SerializeField] int _port;
        [SerializeField] int _fakeLatency;

        private static GameClient _instance;

        private Server _server;
        private TCP _tcp;
        private LatencyHistory _latencyHistory;

        public static void Host()
        {
            // abort if already hosting
            if (Server.IsActive)
            {
                return;
            }

            // create the server first
            _instance._server = new Server();
            _instance._server.Start();

            if (!Server.IsActive)
            {
                Log.Error(LogChannel.Server, $"Host - something went wrong starting the server");
                _instance._server = null;

                return;
            }

            if (!_instance.IsHeadless())
            {
                Connect();
            }
        }

        public static void Connect()
        {
            if (IsConnecting)
            {
                return;
            }

            _instance._tcp.Connect();
            IsConnecting = true;
        }

        public static void Disconnect(string reason = "User disconnected")
        {
            if (Server.IsActive)
            {
                // send shut down message
                using (var sendMsg = new ServerShuttingDownMsg())
				{
					ServerSend.ToAllExceptHost(sendMsg);
				}

                // stop server
                _instance._server.Stop();
                _instance._server = null;
            }

            if (IsConnected)
            {
                _instance?._tcp?.Close();

                ThreadManager.Invoke(() =>
                {
                    LocalEvents.Invoke(new DisconnectedData()
                    {
                        Reason = reason
                    });
                });
            }

            IsConnecting = false;
        }

        public static void SendTCPData(Packet packet)
        {
            if (!IsConnected)
            {
                return;
            }

            _instance._tcp?.SendData(packet);
        }

        public static bool IsLocalPlayer(PlayerId playerId)
        {
            return PlayerIds.Contains(playerId);
        }

        public static bool HasAuthority(NetworkIdentity identity)
        {
            if (identity == null)
            {
                return false;
            }

            // client authority
            if (identity.clientId == ClientId)
            {
                return true;
            }

            // default server authority
            if (identity.clientId == ClientId.Invalid
                && Server.IsActive)
            {
                return true;
            }

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
                Log.Info(LogChannel.GameClient, $"Duplicate {this} found, destroying instance");
                Destroy(this.gameObject);
            }
        }

        private void InitSingleton()
        {
            _instance = this;

            _tcp = new TCP();
            _latencyHistory = new LatencyHistory();

            PlayerIds = new List<PlayerId>();

            PacketHandler = new PacketHandler<Packet>()
                .AddHandler(MessageId.Welcome, ClientReceive.Welcome)
                .AddHandler(MessageId.ServerShuttingDown, (packet) => ClientReceive.Message<ServerShuttingDownMsg>(packet))
                .AddHandler(MessageId.ClientConnected, (packet) => ClientReceive.Message<ClientConnectedMsg>(packet))
                .AddHandler(MessageId.ClientDisconnected, (packet) => ClientReceive.Message<ClientDisconnectedMsg>(packet))
                .AddHandler(MessageId.CreatePawn, (packet) => ClientReceive.Message<CreatePawnMsg>(packet))
                .AddHandler(MessageId.CreateProjectile, (packet) => ClientReceive.Message<CreateProjectileMsg>(packet))
                .AddHandler(MessageId.PawnState, (packet) => ClientReceive.Message<PawnStateMsg>(packet))
                .AddHandler(MessageId.PawnEquipmentAction, (packet) => ClientReceive.Message<PawnEquipmentActionMsg>(packet))
                .AddHandler(MessageId.EntityDamage, (packet) => ClientReceive.Message<EntityDamageMsg>(packet))
                .AddHandler(MessageId.EntityHeatLevelChanged, (packet) => ClientReceive.Message<EntityHeatLevelChangedMsg>(packet))
                .AddHandler(MessageId.EntityImpulse, (packet) => ClientReceive.Message<EntityImpulseMsg>(packet))
                .AddHandler(MessageId.KillEntity, (packet) => ClientReceive.Message<KillEntityMsg>(packet))
                .AddHandler(MessageId.AccoladeAwarded, (packet) => ClientReceive.Message<AccoladeAwardedMsg>(packet))
                .AddHandler(MessageId.CreatePickup, (packet) => ClientReceive.Message<CreatePickupMsg>(packet))
                .AddHandler(MessageId.PickupCollected, (packet) => ClientReceive.Message<PickupCollectedMsg>(packet))
                .AddHandler(MessageId.DestroyEntity, (packet) => ClientReceive.Message<DestroyEntityMsg>(packet))
                .AddHandler(MessageId.HomingTarget, (packet) => ClientReceive.Message<HomingTargetMsg>(packet))
                .AddHandler(MessageId.EntityHeal, (packet) => ClientReceive.Message<EntityHealMsg>(packet))
                .AddHandler(MessageId.AddStatusEffect, (packet) => ClientReceive.Message<AddStatusEffectMsg>(packet))
                .AddHandler(MessageId.RemoveStatusEffect, (packet) => ClientReceive.Message<RemoveStatusEffectMsg>(packet))
                .AddHandler(MessageId.AuthorityChange, (packet) => ClientReceive.Message<AuthorityChangeMsg>(packet))
                .AddHandler(MessageId.PlayerRegister, (packet) => ClientReceive.Message<PlayerRegisterMsg>(packet))
                .AddHandler(MessageId.PawnClassSelect, (packet) => ClientReceive.Message<PawnClassSelectMsg>(packet))
                .AddHandler(MessageId.Proceed, (packet) => ClientReceive.Message<ProceedMsg>(packet))
                .AddHandler(MessageId.LoadScene, (packet) => ClientReceive.Message<LoadSceneMsg>(packet))
                .AddHandler(MessageId.PawnRamStateChange, (packet) => ClientReceive.Message<PawnRamStateChangeMsg>(packet))
                .AddHandler(MessageId.CreditsAwarded, (packet) => ClientReceive.Message<CreditsAwardedMsg>(packet))
                .AddHandler(MessageId.ArcadeLevelConfig, (packet) => ClientReceive.Message<ArcadeLevelConfigMsg>(packet))
                .AddHandler(MessageId.EntityEvent, (packet) => ClientReceive.Message<EntityEventMsg>(packet))
                .AddHandler(MessageId.CreateProp, (packet) => ClientReceive.Message<CreatePropMsg>(packet));

            NetworkEvents.Subscribe<ServerShuttingDownMsg>(OnServerShuttingDownMsg);

            if (IsHeadless())
            {
                QualitySettings.vSyncCount = 0;

                Host();
            }
        }

        private void Update()
        {
            _server?.Update();
            PacketHandler?.Update();
        }

        private void OnApplicationQuit()
        {
            Disconnect("Quit application");
        }

        private void OnServerShuttingDownMsg(ServerShuttingDownMsg data)
        {
            Disconnect("Server shutting down");
        }

        private bool IsHeadless()
        {
            return SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null;
        }
    }
}
