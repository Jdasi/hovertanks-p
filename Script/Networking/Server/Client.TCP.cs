using System;
using System.Net.Sockets;

namespace HoverTanks.Networking
{
    public partial class Client
    {
        public class TCP
        {
            public readonly ClientId Id;

            public TcpClient Socket { get; private set; }

            private NetworkStream _stream;
            private Packet _receiveData;
            private byte[] _receiveBuffer;

            public TCP(ClientId id)
            {
                Id = id;
            }

            public void Connect(TcpClient socket, PlayerId playerId)
            {
                Socket = socket;
                Socket.ReceiveBufferSize = DATA_BUFFER_SIZE;
                Socket.SendBufferSize = DATA_BUFFER_SIZE;

                _stream = Socket.GetStream();
                _receiveData = new Packet();
                _receiveBuffer = new byte[DATA_BUFFER_SIZE];

                BeginStreamRead();

                using (var sendMsg = new WelcomeMsg()
                {
                    AssignedClientId = Id,
                    AssignedPlayerId = playerId,
                    Bulletin = "Welcome to the server!",
                })
			    {
				    ServerSend.ToClient(Id, sendMsg);
			    }
            }

            public void Disconnect()
            {
                Socket?.Close();
                Socket = null;
                _stream = null;
                _receiveData = null;
                _receiveBuffer = null;
            }

            public void SendData(Packet packet)
            {
                try
                {
                    if (Socket != null)
                    {
                        _stream.BeginWrite(packet.ToArray(), 0, packet.Length(), null, null);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(LogChannel.Server, $"Error sending data to client {Id}, {ex}");
                }
            }

            private void ReceiveCallback(IAsyncResult result)
            {
                try
                {
                    int byteLength = _stream.EndRead(result);

                    if (byteLength <= 0)
                    {
                        Server.Clients[Id].Disconnect();
                        return;
                    }

                    byte[] data = new byte[byteLength];
                    Array.Copy(_receiveBuffer, data, byteLength);
                
                    _receiveData.Reset(HandleData(data));

                    BeginStreamRead();
                }
                catch (Exception ex)
                {
                    Log.Error(LogChannel.Server, $"Error receiving data from client {Id}, {ex}");
                    Server.Clients[Id].Disconnect();
                }
            }

            private void BeginStreamRead()
            {
                _stream.BeginRead(_receiveBuffer, 0, DATA_BUFFER_SIZE, ReceiveCallback, null);
            }

            private bool HandleData(byte[] data)
            {
                int packetLength = 0;
        
                _receiveData.SetBytes(data);
        
                // check if we have the start of a new packet
                if (_receiveData.UnreadLength() >= 1)
                {
                    packetLength = _receiveData.ReadByte();
        
                    // nothing left
                    if (packetLength <= 0)
                    {
                        return true;
                    }
                }
        
                // process complete packets in the buffer
                while (packetLength > 0 && packetLength <= _receiveData.UnreadLength())
                {
                    byte[] packetBytes = _receiveData.ReadBytes(packetLength);

                    Packet packet = new Packet(packetBytes);
                    var packetId = (MessageId)packet.ReadByte();

                    switch (packetId)
                    {
                        case MessageId.ClientPong:
                        {
                            var msg = new ClientPongMsg();
                            msg.Read(packet);

                            if (!Server.Clients.TryGetValue(Id, out var client))
                            {
                                break;
                            }

                            client.Latency = (int)((Game.time - msg.ServerTimestamp) * 1000f) - GameClient.FakeLatency;

                            using (var sendMsg = new ServerPongMsg()
                            {
                                Timestamp = msg.ClientTimestamp,
                            })
			                {
				                ServerSend.ToClient(Id, sendMsg);
			                }
                        } break;

                        default:
                        {
                            ThreadManager.Invoke(() =>
                            {
                                Server.PacketHandler.Handle(packetId, new Server.ReceiveData(Id, packet));
                            });
                        } break;
                    }
        
                    packetLength = 0;
        
                    // check if we have the start of a new packet
                    if (_receiveData.UnreadLength() >= 1)
                    {
                        packetLength = _receiveData.ReadByte();
        
                        // nothing left
                        if (packetLength <= 0)
                        {
                            return true;
                        }
                    }
                }

                if (packetLength <= 1)
                {
                    return true;
                }

                return false;
            }
        }
    }
}
