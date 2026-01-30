using System;
using UnityEngine;

namespace HoverTanks.Networking
{
    public static class ClientSend
    {
        public enum Modes
        {
            /// <summary>
            /// The message will be sent to the server.
            /// </summary>
            ToServer,

            /// <summary>
            /// If this client is the host, the message will by sent via <see cref="ServerSend.ToAllExceptHost(NetworkMessage)"/>.
            /// </summary>
            ToOthersIfHost,
        }

        public static void Message(NetworkMessage msg, Modes mode = Modes.ToServer)
        {
            if (mode == Modes.ToOthersIfHost && Server.IsActive)
            {
                ServerSend.ToAllExceptHost(msg);
            }
            else
            {
                if (msg._MessageId == MessageId.ClientPong)
                {
                    GameClient.LastPingTimestamp = Game.time;
                }

                using (Packet packet = new Packet(msg._MessageId))
                {
                    msg.Write(packet);

                    SendTCPData(packet);
                }
            }
        }

        public static void HitRequest(HitRequestType type, EntityId ownerId, EntityId targetId, bool wasGlancingBlow = false)
        {
            if (Server.IsActive)
            {
                Log.Error(LogChannel.GameClient, $"[ClientSend] HitRequest - was server, this shouldn't be called");
                return;
            }

            using (var sendMsg = new HitRequestMsg()
            {
                Type = type,
                OwnerId = ownerId,
                TargetId = targetId,
                WasGlancingBlow = wasGlancingBlow,
            })
            {
                Message(sendMsg);
            }
        }

        public static void RamRequest(EntityId ownerId, EntityId targetId, Vector3 position, Quaternion rotation, float magnitude)
        {
            if (Server.IsActive)
            {
                Log.Error(LogChannel.GameClient, $"[ClientSend] RamRequest - was server, this shouldn't be called");
                return;
            }

            using (var sendMsg = new RamRequestMsg()
            {
                OwnerId = ownerId,
                TargetId = targetId,
                Position = position,
                Heading = JHelper.RotationToHeading(rotation),
                Magnitude = magnitude,
            })
            {
                Message(sendMsg);
            }
        }

        public static void State(int state)
        {
            Log.Info(LogChannel.GameClient, $"[ClientSend] State - {state}");

            using (var sendMsg = new StateMsg()
            {
                State = state,
            })
            {
                Message(sendMsg);
            }
        }

        private static void SendTCPData(Packet packet)
        {
            packet.WriteLength();
            GameClient.SendTCPData(packet);
        }
    }
}
