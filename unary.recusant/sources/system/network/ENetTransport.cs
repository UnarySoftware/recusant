using Godot;

namespace Unary.Recusant
{
    public partial class ENetTransport : BaseTransport
    {
        private ENetConnection _connection = new();

        public override void StartHost(int port)
        {
            _connection.CreateHostBound("127.0.0.1", port, 8, 0, 0, 0);
        }

        public override void StartClient(int port)
        {
            _connection.ConnectToHost("127.0.0.1", port, 0, 0);
        }

        public override void Process()
        {
            return;

            var collection = _connection.Service();

            ENetConnection.EventType type = (ENetConnection.EventType)collection[0].AsInt32();
            ENetPacketPeer peer = collection[1].As<ENetPacketPeer>();

            int channel = collection[3].AsInt32();
        }
    }
}
