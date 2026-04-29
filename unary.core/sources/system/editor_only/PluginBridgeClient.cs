#if TOOLS

using Godot;
using System;
using System.Collections.Generic;
using Unary.Core.Editor;

namespace Unary.Core
{
    [Tool]
    public partial class PluginBridgeClient : Node, ICoreSystem
    {
        private enum Status
        {
            None,
            Connecting,
            Connected
        }

        private Status _status = Status.None;
        private StreamPeerTcp _peer = new();
        private float _timer = 0.0f;

        private static void Dummy(StreamPeerTcp peer) { }

        public static Action<StreamPeerTcp> OnConnected = Dummy;

        private Dictionary<int, IStreamSerializableBase> _packets;

        bool ISystem.Initialize()
        {
            OnConnected = Dummy;

            _packets = PluginBridgeHost.CollectPackets();

            Error error = _peer.ConnectToHost("127.0.0.1", PluginBridgeHost.Port);

            if (error != Error.Ok)
            {
                return this.Critical($"Failed to initiate connection with a TCP server to a port {PluginBridgeHost.Port} due to an error {error}");
            }

            _status = Status.Connecting;

            this.Log($"Connecting to a bridge host at {PluginBridgeHost.Port}");

            return true;
        }

        void ISystem.Deinitialize()
        {
            if (_status != Status.None)
            {
                _peer.DisconnectFromHost();
                _status = Status.None;
            }
        }

        void ISystem.Process(float delta)
        {
            if (_status == Status.None)
            {
                return;
            }

            Error error = _peer.Poll();

            if (error != Error.Ok)
            {
                this.Error($"Failed to poll a bridge host connection due to an error {error}");
                return;
            }

            _timer += delta;

            StreamPeerSocket.Status status = _peer.GetStatus();

            if (status == StreamPeerSocket.Status.None ||
                status == StreamPeerSocket.Status.Error)
            {
                this.Log($"Bridge host at {PluginBridgeHost.Port} got disconnected");
                _peer.DisconnectFromHost();
                _status = Status.None;
            }
            else if (status == StreamPeerSocket.Status.Connecting && _timer >= PluginBridgeHost.Timeout)
            {
                this.Log($"Connection timed out when connect to a bridge host at {PluginBridgeHost.Port}");
                _peer.DisconnectFromHost();
                _status = Status.None;
            }
            else if (_status == Status.Connecting && status == StreamPeerSocket.Status.Connected)
            {
                _status = Status.Connected;
                this.Log($"Connected to a bridge host at {PluginBridgeHost.Port}");
                OnConnected(_peer);
            }

            if (_status == Status.Connected && status == StreamPeerSocket.Status.Connected)
            {
                while (_peer.GetAvailableBytes() > 0)
                {
                    int hash = _peer.Get32();

                    if (!_packets.TryGetValue(hash, out var serializable))
                    {
                        this.Error($"Recieved a malformed packet from a bridge host {PluginBridgeHost.Port} with unknown hash {hash}");
                        break;
                    }

                    serializable.Deserialize(_peer);
                    serializable.Dispatch();
                }

                return;
            }
        }

        public void Send<T>(T data) where T : IStreamSerializableBase
        {
            if (_status != Status.Connected)
            {
                return;
            }

            _peer.Put32(data.GetPacketHash());
            data.Serialize(_peer);
        }
    }
}

#endif
