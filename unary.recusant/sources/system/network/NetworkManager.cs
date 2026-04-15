using Godot;
using Unary.Core;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    public partial class NetworkManager : Node, IModSystem
    {
        public const int MaxPlayerCount = 6;

        private bool _local = true;
        private BaseTransport _transport;
        private int _port = 0;

        private readonly BitReader _reader = new();
        private readonly BitWriter _writer = new();

        public EventFunc<ulong> PlayerJoined = new();
        public EventFunc<ulong> PlayerLeft = new();

        bool ISystem.Initialize()
        {
            if (Steam.Initialized)
            {
                _local = false;
            }

            //if(_local)
            {
                _port = 55555;
                _transport = new ENetTransport();
            }

            if (Launcher.Singleton.HasArgument("host"))
            {
                StartHost();
            }
            else if (Launcher.Singleton.HasArgument("client"))
            {
                StartClient();
            }

            return true;
        }

        public void StartHost()
        {
            this.Log("Starting as host");
            _transport.StartHost(_port);
        }

        public void StartClient()
        {
            this.Log("Starting as client");
            _transport.StartClient(_port);
        }

        void ISystem.Process(float delta)
        {
            _transport.Process();
        }

        public void SendToClient(ulong client, byte[] data, BaseTransport.SendType type)
        {
            _transport.SendToClient(client, data, type);
        }

        public void SendToHost(byte[] data, BaseTransport.SendType type)
        {
            _transport.SendToHost(data, type);
        }
    }
}
