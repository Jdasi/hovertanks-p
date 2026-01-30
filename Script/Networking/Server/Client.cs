using System.Collections.Generic;

namespace HoverTanks.Networking
{
    public partial class Client
    {
        public const int DATA_BUFFER_SIZE = 4096;

        public ClientId Id => tcp.Id;
        public int State { get; set; } = -1;
        public int Latency
        {
            get => _latencyHistory.Average;
            set => _latencyHistory.Record(value);
        }
        public int FixedFrameCount { get; set; }
        public TCP tcp { get; private set; }
        public List<EntityId> AuthorityEntityIds { get; private set; } = new List<EntityId>();

        private LatencyHistory _latencyHistory;

        public Client(ClientId clientId)
        {
            tcp = new TCP(clientId);

            _latencyHistory = new LatencyHistory();
        }

        public void AddPlayerId(PlayerId playerId)
        {
            if (Server.PlayerClientLookup.ContainsKey(playerId))
            {
                Log.Error(LogChannel.Client, $"AddPlayerId - server already had active player id: {playerId}");
                return;
            }

            Server.PlayerClientLookup.Add(playerId, Id);
        }

        public void Disconnect()
        {
            Log.Info(LogChannel.Server, $"Client {Id} has disconnected");

            tcp?.Disconnect();

            ThreadManager.Invoke(() =>
            {
                Server.Clients.Remove(Id);

                List<PlayerId> playerIds = Server.GetPlayerIdsForClient(Id);

                foreach (var id in playerIds)
                {
                    Server.PlayerClientLookup.Remove(id);
                }

                using (var sendMsg = new ClientDisconnectedMsg()
                {
                    ClientId = Id,
                    PlayerIds = playerIds.ToArray(),
                })
                {
                    ServerSend.ToAll(sendMsg);
                }
            });
        }
    }
}
