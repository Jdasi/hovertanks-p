using System.Linq;

namespace HoverTanks.Networking
{
    public static class ServerSend
    {
        public static void ToClient(ClientId toClient, NetworkMessage msg)
        {
            using (Packet packet = new Packet(msg._MessageId))
            {
                msg.Write(packet);

                SendData(toClient, packet);
            }
        }

        public static void ToAll(NetworkMessage msg)
        {
            if (msg._MessageId == MessageId.LoadScene
                && msg is LoadSceneMsg loadSceneMsg)
            {
                if (loadSceneMsg.Mode == UnityEngine.SceneManagement.LoadSceneMode.Single)
                {
				    Server.ResetAllClientState();
                }
            }

            using (Packet packet = new Packet(msg._MessageId))
            {
                msg.Write(packet);
                packet.WriteLength();

                foreach (var client in Server.Clients.Values)
                {
                    SendData(client.Id, packet, false);
                }
            }
        }

        public static void ToAllExcept(NetworkMessage msg, params ClientId[] exceptClients)
        {
            if (msg._MessageId == MessageId.ServerPing)
            {
                GameClient.LastPingTimestamp = UnityEngine.Time.time;
            }

            using (Packet packet = new Packet(msg._MessageId))
            {
                msg.Write(packet);
                packet.WriteLength();

                foreach (var client in Server.Clients.Values)
                {
                    if (exceptClients?.Contains(client.Id) ?? false)
                    {
                        continue;
                    }

                    SendData(client.Id, packet, false);
                }
            }
        }

        public static void ToAllExceptHost(NetworkMessage msg)
        {
            ToAllExcept(msg, GameClient.ClientId);
        }

        private static void SendData(ClientId toClient, Packet packet, bool writeLength = true)
        {
            if (!Server.Clients.TryGetValue(toClient, out var client))
            {
                return;
            }

            if (writeLength)
            {
                packet.WriteLength();
            }

            client.tcp.SendData(packet);
        }

        public static class Helpers
        {
            public static void EntityEvent(EntityId entityId, byte eventId)
            {
                using (var sendMsg = new EntityEventMsg()
                {
                    EntityId = entityId,
                    EventId = eventId,
                })
                {
                    ToAll(sendMsg);
                }
            }
        }
    }
}
