#if TOOLS

using Godot;

namespace Unary.Core.Editor
{
    public interface IStreamSerializableBase
    {
        public void Serialize(StreamPeerTcp peer);
        public void Deserialize(StreamPeerTcp peer);
        public int GetPacketHash();
        public void Dispatch();
    }
}

#endif
