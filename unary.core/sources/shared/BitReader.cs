
using System;
using System.Numerics;

namespace Unary.Core
{
    public class BitReader
    {
        public byte[] Bytes
        {
            get
            {
                return field;
            }
            set
            {
                field = value;
                bitOffset = 0;
            }
        }

        private int bitOffset = 0;

        public T Read<T>(int bits) where T : IBinaryInteger<T>
        {
            if (bits <= 0 || bits > 128)
            {
                throw new ArgumentOutOfRangeException(nameof(bits), "Bits must be between 1 and 128.");
            }

            T value = T.Zero;

            for (int i = 0; i < bits; i++)
            {
                int byteIndex = bitOffset / 8;
                int bitIndex = bitOffset % 8;

                if (byteIndex >= Bytes.Length)
                {
                    throw new IndexOutOfRangeException("Attempted to read beyond the end of the byte array.");
                }

                bool bitSet = (Bytes[byteIndex] & (1 << bitIndex)) != 0;
                T bitValue = T.CreateChecked(1) << i;

                if (bitSet)
                {
                    value |= bitValue;
                }

                bitOffset++;
            }
            return value;
        }

        public bool Read() => Read<int>(1) != 0;

        public byte ReadByte()
        {
            int remainingBits = Bytes.Length * 8 - bitOffset;
            if (remainingBits <= 0)
            {
                throw new IndexOutOfRangeException("No more bits to read.");
            }

            int bitsToRead = Math.Min(8, remainingBits);
            byte result = 0;

            for (int i = 0; i < bitsToRead; i++)
            {
                int byteIndex = bitOffset / 8;
                int bitIndex = bitOffset % 8;

                bool bitSet = (Bytes[byteIndex] & (1 << bitIndex)) != 0;
                if (bitSet)
                {
                    result |= (byte)(1 << i);
                }

                bitOffset++;
            }
            return result;
        }
    }
}
