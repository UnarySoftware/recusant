#if TOOLS

using Godot;
using System;
using System.Collections.Generic;

namespace Unary.Core.Editor
{
    [Tool]
    public partial class PluginBridgeHost : IPluginSystem
    {
        public const int Port = 60909;
        public const float Timeout = 10.0f;

        private readonly TcpServer _server = new();

        private enum PeerStatus
        {
            Connecting,
            Connected
        }

        private readonly Dictionary<StreamPeerTcp, (PeerStatus status, int port)> _peers = [];

        private static void Dummy(StreamPeerTcp peer) { }

        public static Action<StreamPeerTcp> OnConnected = Dummy;
        public static Action<StreamPeerTcp> OnDisconnected = Dummy;

        private Dictionary<int, IStreamSerializableBase> _packets;

        public static Dictionary<int, IStreamSerializableBase> CollectPackets()
        {
            Dictionary<int, IStreamSerializableBase> result = [];

            Type baseType = typeof(IStreamSerializableBase);
            Type instanceType = typeof(IStreamSerializable<>);

            var types = Types.GetTypesOfBase(baseType);

            foreach (var type in types)
            {
                if (type == baseType || type == instanceType)
                {
                    continue;
                }

                IStreamSerializableBase target = (IStreamSerializableBase)Activator.CreateInstance(type);
                result[target.GetPacketHash()] = target;
            }

            return result;
        }

        bool ISystem.Initialize()
        {
            _packets = CollectPackets();

            Error error = _server.Listen(Port, "127.0.0.1");

            if (error != Error.Ok)
            {
                this.Warning($"Failed to bind a TCP server to a port {Port} due to an error {error}");
            }
            else
            {
                this.Log($"Started a bridge host on a port {Port}");
            }

            return true;
        }

        void ISystem.Process(float delta)
        {
            HashSet<StreamPeerTcp> deleted = [];

            foreach (var connection in _peers)
            {
                Error error = connection.Key.Poll();

                if (error != Error.Ok)
                {
                    this.Error($"Failed to poll a bridge client due to an error {error}");
                    deleted.Add(connection.Key);
                    continue;
                }

                StreamPeerSocket.Status status = connection.Key.GetStatus();

                if (status == StreamPeerSocket.Status.None || status == StreamPeerSocket.Status.Error)
                {
                    deleted.Add(connection.Key);
                    continue;
                }
                else if (connection.Value.status == PeerStatus.Connecting)
                {
                    if (status == StreamPeerSocket.Status.Connected)
                    {
                        _peers[connection.Key] = (PeerStatus.Connected, connection.Key.GetConnectedPort());
                        this.Log($"Bridge client {connection.Key.GetConnectedPort()} joined");
                        OnConnected(connection.Key);
                    }
                }
                else if (connection.Value.status == PeerStatus.Connected)
                {
                    while (connection.Key.GetAvailableBytes() > 0)
                    {
                        int hash = connection.Key.Get32();

                        if (!_packets.TryGetValue(hash, out var serializable))
                        {
                            this.Error($"Recieved a malformed packet from a bridge client {connection.Key.GetConnectedPort()}");
                            break;
                        }

                        serializable.Deserialize(connection.Key);
                        serializable.Dispatch();
                    }
                }
            }

            while (_server.IsConnectionAvailable())
            {
                StreamPeerTcp newPeer = _server.TakeConnection();
                _peers[newPeer] = (PeerStatus.Connecting, newPeer.GetConnectedPort());
                this.Log($"Bridge client {newPeer.GetConnectedPort()} connected");
            }

            foreach (var delete in deleted)
            {
                int port = _peers[delete].port;
                this.Log($"Bridge client {port} left");
                delete.DisconnectFromHost();
                _peers.Remove(delete);
                OnDisconnected(delete);
            }
        }

        void ISystem.Deinitialize()
        {
            _server.Stop();
            _packets.Clear();
        }

        // targets set to null means global broadcast
        public void Send<T>(T data, StreamPeerTcp[] targets) where T : IStreamSerializableBase
        {
            if (_peers.Count == 0)
            {
                return;
            }

            if (targets == null)
            {
                foreach (var peer in _peers)
                {
                    if (peer.Value.status == PeerStatus.Connected)
                    {
                        peer.Key.Put32(data.GetPacketHash());
                        data.Serialize(peer.Key);
                    }
                }
            }
            else
            {
                foreach (var peer in _peers)
                {
                    bool found = false;

                    foreach (var target in targets)
                    {
                        if (target == peer.Key)
                        {
                            found = true;
                            break;
                        }
                    }

                    if (found && peer.Value.status == PeerStatus.Connected)
                    {
                        peer.Key.Put32(data.GetPacketHash());
                        data.Serialize(peer.Key);
                    }
                }
            }
        }
    }
}

#endif
