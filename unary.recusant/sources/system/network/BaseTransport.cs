
namespace Unary.Recusant
{
    public abstract partial class BaseTransport
    {
        public enum SendType
        {
            Reliable,
            Unreliable
        };

        public abstract void StartHost(int port);
        public abstract void StartClient(int port);
        public abstract void Process();
        public abstract void SendToClient(ulong client, byte[] data, SendType type);
        public abstract void SendToHost(byte[] data, SendType type);
    }
}
