using Godot;
using System.Collections.Generic;
using System.Text;
using Unary.Core;

namespace Unary.Recusant
{
    public partial class ENetTransport : BaseTransport
    {
        private ENetConnection _connection = new();
        private ENetPacketPeer _host;

        private Dictionary<ulong, ENetPacketPeer> _idToClient = [];
        private Dictionary<ENetPacketPeer, ulong> _clientToId = [];

        private ulong id = 1;
        private bool _active = false;
        private bool _isHost = false;

        public override void StartHost(int port)
        {
            Error error = _connection.CreateHostBound("127.0.0.1", port, NetworkManager.MaxPlayerCount, 0, 0, 0);

            if (error != Error.Ok)
            {
                RuntimeLogger.Error(this, $"Failed to create a host at port {port} due to an error \"{error}\"");
                return;
            }

            _active = true;
            _isHost = true;
        }

        public override void StartClient(int port)
        {
            Error error = _connection.CreateHost(1);

            if (error != Error.Ok)
            {
                RuntimeLogger.Error(this, $"Failed to create an outgoing host due to an error \"{error}\"");
                return;
            }

            _host = _connection.ConnectToHost("127.0.0.1", port, 0, 0);

            if (_host == null)
            {
                RuntimeLogger.Error(this, $"Failed to create a client at port {port}");
                return;
            }

            _active = true;
            _isHost = false;
        }

        public override void Process()
        {
            if (!_active)
            {
                return;
            }

            while (true)
            {
                Godot.Collections.Array collection = _connection.Service();

                if (collection.Count == 0)
                {
                    break;
                }

                ENetConnection.EventType type = (ENetConnection.EventType)collection[0].AsInt32();
                ENetPacketPeer peer = collection[1].As<ENetPacketPeer>();

                switch (type)
                {
                    case ENetConnection.EventType.None:
                        {
                            return;
                        }
                    case ENetConnection.EventType.Connect:
                        {
                            if (_isHost)
                            {
                                _idToClient[id] = peer;
                                _clientToId[peer] = id;
                                NetworkManager.Singleton.PlayerJoined.Publish(id);
                                id++;
                            }
                            else
                            {
                                SendToHost([(byte)'T', (byte)'E', (byte)'S', (byte)'T'], SendType.Reliable);
                            }
                            break;
                        }
                    case ENetConnection.EventType.Disconnect:
                        {
                            if (_isHost)
                            {
                                ulong id = _clientToId[peer];
                                _clientToId.Remove(peer);
                                _idToClient.Remove(id);
                                NetworkManager.Singleton.PlayerLeft.Publish(id);
                            }
                            break;
                        }
                    case ENetConnection.EventType.Receive:
                        {
                            RuntimeLogger.Log(this, "RECIEVE!: " + Encoding.UTF8.GetString(peer.GetPacket()));
                            break;
                        }
                }
            }
        }

        public override void SendToClient(ulong client, byte[] data, SendType type)
        {
            if (!_active)
            {
                return;
            }

            int sendType;

            if (type == SendType.Unreliable)
            {
                sendType = (int)ENetPacketPeer.FlagUnreliableFragment | (int)ENetPacketPeer.FlagUnsequenced;
            }
            else
            {
                sendType = (int)ENetPacketPeer.FlagReliable;
            }

            Error error = _idToClient[client].Send(0, data, sendType);

            if (error != Error.Ok)
            {
                RuntimeLogger.Error(this, $"Failed to send a packet of type \"{type}\" to {client} for a reason \"{error}\"");
            }
        }

        public override void SendToHost(byte[] data, SendType type)
        {
            if (!_active)
            {
                return;
            }

            int sendType;

            if (type == SendType.Unreliable)
            {
                sendType = (int)ENetPacketPeer.FlagUnreliableFragment | (int)ENetPacketPeer.FlagUnsequenced;
            }
            else
            {
                sendType = (int)ENetPacketPeer.FlagReliable;
            }

            Error error = _host.Send(0, data, sendType);

            if (error != Error.Ok)
            {
                RuntimeLogger.Error(this, $"Failed to send a packet of type \"{type}\" to the host for a reason \"{error}\"");
            }
        }
    }
}
