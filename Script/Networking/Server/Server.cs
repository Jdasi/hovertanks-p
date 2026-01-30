using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace HoverTanks.Networking
{
    public class Server
    {
        public struct ReceiveData
        {
            public ClientId FromClientId;
            public Packet Packet;

            public ReceiveData(ClientId fromClientId, Packet packet)
            {
                FromClientId = fromClientId;
                Packet = packet;
            }
        }

        public const int MAX_PLAYERS = 4;

        public static bool IsActive { get; private set; }
        public static bool AreConnectionsAllowed { get; set; } = true;

        public static Dictionary<ClientId, Client> Clients;
        public static Dictionary<PlayerId, ClientId> PlayerClientLookup;
        public static PacketHandler<ReceiveData> PacketHandler;

        private const float PING_INTERVAL = 2f;

        private static Server _instance;

        private TcpListener _tcpListener;
        private float _nextPingTime;

        public static bool DoAllClientsHaveState(int state)
        {
            if (!IsActive)
            {
                return false;
            }

            foreach (var client in Clients.Values)
            {
                if (client.State != state)
                {
                    return false;
                }
            }

            return true;
        }

        public static void ResetAllClientState()
        {
            if (!IsActive)
            {
                return;
            }

            Log.Info(LogChannel.Server, $"Resetting all client state..");

            foreach (var client in Clients.Values)
            {
                client.State = -1;
            }
        }

        public static void DoSyncedLogic(Action<ClientId, List<PlayerId>> action)
        {
            if (!IsActive)
            {
                return;
            }

            GameManager.Run(StaggeredRoutine(action));
        }

        public static void DoSyncedProceed()
        {
            if (!IsActive)
            {
                return;
            }

            GameManager.Run(StaggeredRoutine((clientId, playerIds) =>
            {
                using (var sendMsg = new ProceedMsg())
			    {
				    ServerSend.ToClient(clientId, sendMsg);
			    }
            }));
        }

        private static IEnumerator StaggeredRoutine(Action<ClientId, List<PlayerId>> action)
        {
            List<Client> clients = new List<Client>(Clients.Count);
            Client hostClient = null;

            foreach (var client in Clients.Values)
            {
                if (client.Id == ClientId.Server)
                {
                    hostClient = client;
                    continue;
                }

                clients.Add(client);
            }

            // sort by latency (descending)
            clients.Sort((a, b) => b.Latency.CompareTo(a.Latency));

            // add host last
            clients.Add(hostClient);

            int lastLatency = 0;
            
            // stagger action based on each client latency
            foreach (var client in clients)
            {
                if (lastLatency > 0)
                {
                    int diff = lastLatency - client.Latency - GameClient.FakeLatency;
                    yield return new WaitForSecondsRealtime(diff / 1000f);
                }

                lastLatency = client.Latency;

                action(client.Id, GetPlayerIdsForClient(client.Id));
            }
        }

        public static bool TryGetClientIdForPlayer(PlayerId playerId, out ClientId clientId)
        {
            clientId = default;

            if (!IsActive)
            {
                return false;
            }

            return PlayerClientLookup.TryGetValue(playerId, out clientId);
        }

        public static List<PlayerId> GetPlayerIdsForClient(ClientId clientId)
        {
            if (!IsActive)
            {
                return new List<PlayerId>();
            }

            List<PlayerId> list = new List<PlayerId>();

            foreach (var lookup in PlayerClientLookup)
            {
                if (lookup.Value != clientId)
                {
                    continue;
                }

                list.Add(lookup.Key);
            }

            return list;
        }

        public static bool DoesClientHaveEntityAuthority(ClientId clientId, EntityId entityId)
        {
            if (!IsActive)
            {
                return false;
            }

            if (Clients == null)
            {
                return false;
            }

            if (clientId == ClientId.Invalid)
            {
                return false;
            }

            if (!Clients.TryGetValue(clientId, out var client))
            {
                return false;
            }

            if (!client.AuthorityEntityIds.Contains(entityId))
            {
                return false;
            }

            return true;
        }

        public static void RegisterEntityAuthority(ClientId clientId, EntityId entityId)
        {
            if (!IsActive)
            {
                return;
            }

            if (Clients == null)
            {
                return;
            }

            if (clientId == ClientId.Invalid)
            {
                return;
            }

            if (!Clients.TryGetValue(clientId, out var client))
            {
                Log.Warning(LogChannel.Server, $"RegisterEntityAuthority - couldn't get client with id: {clientId}");
                return;
            }

            if (client.AuthorityEntityIds.Contains(entityId))
            {
                return;
            }

            client.AuthorityEntityIds.Add(entityId);
        }

        public static void UnregisterEntityAuthority(ClientId clientId, EntityId entityId)
        {
            if (!IsActive)
            {
                return;
            }

            if (Clients == null)
            {
                return;
            }

            if (clientId == ClientId.Invalid)
            {
                return;
            }

            if (!Clients.TryGetValue(clientId, out var client))
            {
                Log.Warning(LogChannel.Server, $"UnregisterEntityAuthority - couldn't get client with id: {clientId}");
                return;
            }

            client.AuthorityEntityIds.Remove(entityId);
        }

        public Server()
        {
            Clients = new Dictionary<ClientId, Client>(MAX_PLAYERS);
            PlayerClientLookup = new Dictionary<PlayerId, ClientId>(MAX_PLAYERS);

            PacketHandler = new PacketHandler<ReceiveData>()
                .AddHandler(MessageId.WelcomeReceived, ServerReceive.WelcomeReceived)
                .AddHandler(MessageId.PawnState, ServerReceive.PawnState)
                .AddHandler(MessageId.PawnEquipmentAction, ServerReceive.PawnEquipmentAction)
                .AddHandler(MessageId.HitRequest, ServerReceive.HitRequest)
                .AddHandler(MessageId.RamRequest, ServerReceive.RamRequest)
                .AddHandler(MessageId.PawnClassSelect, ServerReceive.PawnClassSelect)
                .AddHandler(MessageId.State, ServerReceive.State)
                .AddHandler(MessageId.PawnRamStateChange, ServerReceive.PawnRamStateChange);
        }

        public void Start()
        {
            if (_instance != null)
            {
                Log.Info(LogChannel.Server, "Start - instance already exists, aborting");
                return;
            }

            try
            {
                _instance = this;
                IsActive = true;

                Log.Info(LogChannel.Server, $"Starting..");
                InitServerData();

                _tcpListener = new TcpListener(IPAddress.Any, GameClient.Port);
                _tcpListener.Start();
                ListenForConnections();

                Log.Info(LogChannel.Server, $"Started on port: {GameClient.Port}");
            }
            catch (Exception ex)
            {
                Log.Error(LogChannel.Server, $"Failed to start: {ex}");

                _instance = null;
                IsActive = false;
            }
        }

        public void Stop()
        {
            if (_instance != this)
            {
                return;
            }

            _tcpListener?.Stop();
            _instance = null;

            IsActive = false;
        }

        public void Update()
        {
            PacketHandler.Update();

            if (Time.time >= _nextPingTime)
            {
                _nextPingTime = Time.time + PING_INTERVAL;

                using (var sendMsg = new ServerPingMsg()
                {
                    Timestamp = Time.time,
                })
				{
					ServerSend.ToAllExceptHost(sendMsg);
				}
            }
        }

        private void ListenForConnections()
        {
            _tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);
        }

        private void TCPConnectCallback(IAsyncResult result)
        {
            if (!IsActive)
            {
                return;
            }

            try
            {
                TcpClient socket = _tcpListener.EndAcceptTcpClient(result);
                ListenForConnections();

                Log.Info(LogChannel.Server, $"Incoming connection from: {socket.Client.RemoteEndPoint}");

                if (!HandleConnection(socket, out string failReason))
                {
                    Log.Info(LogChannel.Server, $"{socket.Client.RemoteEndPoint} failed to connect: {failReason}");
                    socket.Close(); // is this the best way to sever the connection at this point?
                }
            }
            catch (Exception ex)
            {
                Log.Error(LogChannel.Server, $"Error receiving TCP data: {ex}");
            }
        }

        private bool HandleConnection(TcpClient socket, out string failReason)
        {
            failReason = "";

            if (PlayerClientLookup.Count >= MAX_PLAYERS)
            {
                failReason = "Server full";
                return false;
            }

            if (!AreConnectionsAllowed)
            {
                failReason = "Not accepting connections";
                return false;
            }

            ThreadManager.Invoke(() =>
            {
                ClientId clientId = ClientId.Server;

                // find a unique client id for this connection
                while (Clients.ContainsKey(clientId))
                {
                    ++clientId;
                }

                PlayerId playerId = PlayerId.One;

                // find a unique player id for this connection
                while (PlayerClientLookup.ContainsKey(playerId))
                {
                    ++playerId;
                }

                // construct the new client
                var client = new Client(clientId);
                client.AddPlayerId(playerId);

                // add & connect
                Clients.Add(clientId, client);
                client.tcp.Connect(socket, playerId);
            });

            return true;
        }

        private void InitServerData()
        {
        }
    }
}
