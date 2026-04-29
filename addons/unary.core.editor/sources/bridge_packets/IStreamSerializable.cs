#if TOOLS

using System;

namespace Unary.Core.Editor
{
    public interface IStreamSerializable<T> : IStreamSerializableBase
        where T : struct
    {
        private static void Dummy(T packet) { }

        public static Action<T> OnRecieve = Dummy;

        int IStreamSerializableBase.GetPacketHash()
        {
            return typeof(T).FullName.GetDeterministicHashCode();
        }

        void IStreamSerializableBase.Dispatch()
        {
            OnRecieve((T)this);
        }
    }
}

#endif
