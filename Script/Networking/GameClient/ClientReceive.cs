using HoverTanks.Events;
using System.Collections.Generic;

namespace HoverTanks.Networking
{
    public static class ClientReceive
    {
        public static void Message<T>(Packet packet) where T : NetworkMessage, new()
        {
            T data = new T();
            data.Read(packet);

            NetworkEvents.Invoke(data);
        }

        public static void Welcome(Packet packet)
        {
            var msg = new WelcomeMsg();
            msg.Read(packet);

            Log.Info(LogChannel.GameClient, $"Welcome - {msg.Bulletin} (cid: {msg.AssignedClientId}, pid: {msg.AssignedPlayerId})");

            GameClient.ClientId = msg.AssignedClientId;
            GameClient.PlayerIds = new List<PlayerId>() { msg.AssignedPlayerId };

            // ensure a valid username is set
            if (string.IsNullOrWhiteSpace(GameClient.Username))
            {
                GameClient.Username = $"Player {msg.AssignedPlayerId}";
            }

            using (var sendMsg = new WelcomeReceivedMsg()
            {
                ClientId = GameClient.ClientId,
                PlayerId = GameClient.PlayerIds[0],
                Username = GameClient.Username,
            })
            {
                ClientSend.Message(sendMsg);
            }
        }
    }
}
