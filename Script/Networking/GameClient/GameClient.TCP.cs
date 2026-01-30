using System;
using System.Net.Sockets;
using UnityEngine;

namespace HoverTanks.Networking
{
    public partial class GameClient
    {
        private class TCP
        {
            public TcpClient Socket { get; private set; }

            private NetworkStream _stream;
            private Packet _receiveData;
            private byte[] _receiveBuffer;

            public void Connect()
            {
                InitClientData();

                Socket = new TcpClient
                {
                    ReceiveBufferSize = Client.DATA_BUFFER_SIZE,
                    SendBufferSize = Client.DATA_BUFFER_SIZE
                };

                _receiveBuffer = new byte[Client.DATA_BUFFER_SIZE];
                Socket.BeginConnect(IP, Port, ConnectCallback, Socket);
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
                    Log.Error(LogChannel.GameClient, $"Error sending data: {ex}");
                }
            }

            public void Close()
            {
                Socket?.Close();
                Socket = null;
            }

            private void ConnectCallback(IAsyncResult result)
            {
                Debug.Log($"[GameClient.TCP] ConnectCallback - {result}");
                Socket.EndConnect(result);

                if (!Socket.Connected)
                {
                    return;
                }

                _stream = Socket.GetStream();
                _receiveData = new Packet();

                BeginStreamRead();
            }

            private void ReceiveCallback(IAsyncResult result)
            {
                try
                {
                    int byteLength = _stream.EndRead(result);

                    if (byteLength <= 0)
                    {
                        GameClient.Disconnect();
                        return;
                    }

                    byte[] data = new byte[byteLength];
                    Array.Copy(_receiveBuffer, data, byteLength);
            
                    _receiveData.Reset(HandleData(data));

                    BeginStreamRead();
                }
                catch (Exception ex)
                {
                    Log.Error(LogChannel.GameClient, $"Error receiving data: {ex}");
                    Disconnect();
                }
            }

            private void BeginStreamRead()
            {
                _stream.BeginRead(_receiveBuffer, 0, Client.DATA_BUFFER_SIZE, ReceiveCallback, null);
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
                        case MessageId.ServerPing:
                        {
                            var msg = new ServerPingMsg();
                            msg.Read(packet);

                            using (var sendMsg = new ClientPongMsg()
                            {
                                ServerTimestamp = msg.Timestamp,
                                ClientTimestamp = Game.time,
                            })
                            {
                                ClientSend.Message(sendMsg);
                            }
                        } break;

                        case MessageId.ServerPong:
                        {
                            var msg = new ServerPongMsg();
                            msg.Read(packet);

                            Latency = (int)((Game.time - msg.Timestamp) * 1000) - FakeLatency;
                        } break;

                        default:
                        {
                            ThreadManager.Invoke(() =>
                            {
                                PacketHandler.Handle(packetId, packet);
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

            private void InitClientData()
            {
            }

            private void Disconnect()
            {
                GameClient.Disconnect();

                _stream = null;
                _receiveData = null;
                _receiveBuffer = null;
                Socket = null;
            }
        }
    }
}
