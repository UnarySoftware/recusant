namespace Unary.Recusant
{
    public interface INetworkComponent
    {
        public int GetFieldBitMaskSize();
        public int GetFieldCount();
        public uint Serialize(ref byte[] buffer);
        public bool Deserialize(ref byte[] buffer);
    }
}
