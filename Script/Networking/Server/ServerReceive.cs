using HoverTanks.Events;

namespace HoverTanks.Networking
{
    public static class ServerReceive
    {
        public static void WelcomeReceived(Server.ReceiveData data)
        {
            using (var msg = new WelcomeReceivedMsg())
            {
                msg.Read(data.Packet);

                // try get client data
                if (!Server.Clients.TryGetValue(data.FromClientId, out var client))
                {
                    Log.Error(LogChannel.Server, $"WelcomeReceived - server didn't contain client with id: {data.FromClientId}");
                    return;
                }

                // sanity check client id
                if (data.FromClientId != msg.ClientId)
                {
                    Log.Warning(LogChannel.Server, $"WelcomeReceived - client {data.FromClientId} assumed the wrong client id: {msg.ClientId}");
                    client.Disconnect();
                    return;
                }

                // sanity check player id
                if (!Server.TryGetClientIdForPlayer(msg.PlayerId, out var clientId)
                    || data.FromClientId != clientId)
                {
                    Log.Warning(LogChannel.Server, $"WelcomeReceived - client {data.FromClientId} assumed the wrong player id: {msg.ClientId}");
                    client.Disconnect();
                    return;
                }

                Log.Info(LogChannel.Server, $"{client.tcp.Socket.Client.RemoteEndPoint} connected as cid: {data.FromClientId}, pid: {msg.PlayerId}");

                // update player display name
                if (PlayerManager.GetPlayerInfo(msg.PlayerId, out var playerInfo))
                {
                    playerInfo.DisplayName = msg.Username;
                }

                // send current player register to the new client
                using (var sendMsg = new PlayerRegisterMsg())
			    {
                    ServerSend.ToClient(data.FromClientId, sendMsg);
			    }

                // inform all of the new client
                using (var sendMsg = new ClientConnectedMsg()
                {
                    ClientId = client.Id,
                    PlayerId = msg.PlayerId,
                    Username = msg.Username,
                })
                {
                    ServerSend.ToAll(sendMsg);
                }
            }
        }

        public static void PawnState(Server.ReceiveData data)
        {
            using (var msg = new PawnStateMsg())
            {
                msg.Read(data.Packet);

                if (!Server.DoesClientHaveEntityAuthority(data.FromClientId, msg.EntityId))
                {
                    return;
                }

                NetworkEvents.Invoke(msg);
            }
        }

        public static void PawnEquipmentAction(Server.ReceiveData data)
        {
            using (var msg = new PawnEquipmentActionMsg())
            {
                msg.Read(data.Packet);

                if (!Server.DoesClientHaveEntityAuthority(data.FromClientId, msg.EntityId))
                {
                    return;
                }

                NetworkEvents.Invoke(msg);
            }
        }

        public static void HitRequest(Server.ReceiveData data)
        {
            using (var msg = new HitRequestMsg())
            {
                msg.Read(data.Packet);

                if (!Server.DoesClientHaveEntityAuthority(data.FromClientId, msg.OwnerId))
                {
                    return;
                }

                NetworkEvents.Invoke(msg);
            }
        }

        public static void RamRequest(Server.ReceiveData data)
        {
            using (var msg = new RamRequestMsg())
            {
                msg.Read(data.Packet);

                if (!Server.DoesClientHaveEntityAuthority(data.FromClientId, msg.OwnerId))
                {
                    return;
                }

                NetworkEvents.Invoke(msg);
            }
        }

        public static void PawnClassSelect(Server.ReceiveData data)
        {
            using (var sendMsg = new PawnClassSelectMsg())
            {
                sendMsg.Read(data.Packet);

                // sanity check target player id
                if (!Server.TryGetClientIdForPlayer(sendMsg.PlayerId, out var clientId)
                    || clientId != data.FromClientId)
                {
                    Log.Warning(LogChannel.Server, $"ChangePawnClass - {data.FromClientId} tried to target non-local player id: {sendMsg.PlayerId}");
                    return;
                }

                if (!PlayerManager.ActivePlayers.TryGetValue(sendMsg.PlayerId, out var player))
                {
                    Log.Warning(LogChannel.Server, $"ChangePawnClass - player id didn't exist: {sendMsg.PlayerId}");
                    return;
                }

                // update local register
                player.PawnClass = sendMsg.PawnClass;

                // forward the change
                ServerSend.ToAllExcept(sendMsg, GameClient.ClientId, data.FromClientId);

                // run locally
                NetworkEvents.Invoke(sendMsg);
            }
        }

        public static void State(Server.ReceiveData data)
        {
            using (var msg = new StateMsg())
            {
                msg.Read(data.Packet);

                if (!Server.Clients.TryGetValue(data.FromClientId, out var client))
                {
                    return;
                }

                Log.Info(LogChannel.Server, $"State - cid: {data.FromClientId}, state: {msg.State}");

                client.State = msg.State;
            }
        }

        public static void PawnRamStateChange(Server.ReceiveData data)
        {
            using (var sendMsg = new PawnRamStateChangeMsg())
            {
                sendMsg.Read(data.Packet);

                if (!Server.Clients.TryGetValue(data.FromClientId, out var client))
                {
                    return;
                }

                // forward the change
                ServerSend.ToAllExcept(sendMsg, GameClient.ClientId, data.FromClientId);

                // run locally
                NetworkEvents.Invoke(sendMsg);
            }
        }
    }
}
