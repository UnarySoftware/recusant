#if TOOLS

using Godot;
using System.Text;

namespace Unary.Core.Editor
{
    public struct EditorVariablePacket : IStreamSerializable<EditorVariablePacket>
    {
        public int VariableHash;
        public Variant Value;

        public readonly void Serialize(StreamPeerTcp peer)
        {
            peer.Put32(VariableHash);
            string value = GD.VarToStr(Value);
            peer.Put32(value.Length);
            peer.PutData(Encoding.UTF8.GetBytes(value));
        }

        public void Deserialize(StreamPeerTcp peer)
        {
            VariableHash = peer.Get32();
            int count = peer.Get32();
            Godot.Collections.Array data = peer.GetData(count);
            byte[] byteData = data[1].As<byte[]>();
            Value = GD.StrToVar(Encoding.UTF8.GetString(byteData));
        }
    }
}

#endif
