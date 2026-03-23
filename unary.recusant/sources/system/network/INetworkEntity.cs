namespace Unary.Recusant
{
    public interface INetworkEntity
    {
        public int GetComponentBitMaskSize();
        public int GetComponentCount();
        public INetworkComponent GetNetworkComponent(int componentIndex);
    }
}
