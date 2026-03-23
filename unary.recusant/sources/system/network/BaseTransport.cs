
namespace Unary.Recusant
{
    public abstract partial class BaseTransport
    {
        public abstract void StartHost(int port);
        public abstract void StartClient(int port);
        public abstract void Process();
    }
}
