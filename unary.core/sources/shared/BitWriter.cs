using System;
using System.Collections.Generic;
using System.Numerics;

namespace Unary.Core
{
    public class BitWriter
    {
        public List<byte> Bytes
        {
            get
            {
                return field;
            }
            set
            {
                field = value;
            }
        } = [0];

        public void Reset()
        {
            Bytes.Clear();
            bitsInBuffer = 0;
        }

        private int bitsInBuffer = 0;

        public void Write<T>(T value, int bits) where T : IBinaryInteger<T>
        {
            if (bits <= 0 || bits > 64)
            {
                throw new ArgumentOutOfRangeException(nameof(bits), "Bits must be between 1 and 64.");
            }

            for (int i = 0; i < bits; i++)
            {
                int byteIndex = bitsInBuffer / 8;

                if (byteIndex >= Bytes.Count)
                {
                    Bytes.Add(0);
                }

                if ((value & (T.One << i)) != T.Zero)
                {
                    Bytes[byteIndex] |= (byte)(1 << (bitsInBuffer % 8));
                }

                bitsInBuffer++;
            }
        }

        public void Write(bool value) => Write(value ? 1 : 0, 1);

        /// <summary>
        /// Writes a full byte. If there are less than 8 bits remaining in the current byte,
        /// it pads with zeros.
        /// </summary>
        public void WriteByte(byte value)
        {
            int remainingBitsInCurrentByte = 8 - (bitsInBuffer % 8);
            int byteIndex = bitsInBuffer / 8;

            if (remainingBitsInCurrentByte == 8)
            {
                // Start a new byte
                Bytes.Add(value);
                bitsInBuffer += 8;
            }
            else
            {
                // Fill the current byte
                for (int i = 0; i < 8; i++)
                {
                    if ((value & (1 << i)) != 0)
                    {
                        Bytes[byteIndex] |= (byte)(1 << (bitsInBuffer % 8));
                    }
                    bitsInBuffer++;
                    if (bitsInBuffer % 8 == 0)
                    {
                        // Move to next byte if needed
                        Bytes.Add(0);
                        byteIndex++;
                    }
                }
            }
        }
    }
}
