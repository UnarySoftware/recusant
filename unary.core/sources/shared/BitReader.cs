
using System;
using System.Numerics;

namespace Unary.Core
{
	public class BitReader
	{
		private readonly byte[] bytes;
		private int bitOffset = 0;

		public BitReader(byte[] bytes)
		{
			this.bytes = bytes ?? throw new ArgumentNullException(nameof(bytes));
		}

		public T Read<T>(int bits) where T : IBinaryInteger<T>
		{
			if (bits <= 0 || bits > 128)
				throw new ArgumentOutOfRangeException(nameof(bits), "Bits must be between 1 and 128.");

			T value = T.Zero;

			for (int i = 0; i < bits; i++)
			{
				int byteIndex = bitOffset / 8;
				int bitIndex = bitOffset % 8;

				if (byteIndex >= bytes.Length)
					throw new IndexOutOfRangeException("Attempted to read beyond the end of the byte array.");

				bool bitSet = (bytes[byteIndex] & (1 << bitIndex)) != 0;
				T bitValue = T.CreateChecked(1) << i;

				if (bitSet)
					value |= bitValue;

				bitOffset++;
			}
			return value;
		}

		public bool Read() => Read<int>(1) != 0;

		/// <summary>
		/// Reads a full byte, padding with zeros if bits remaining are less than 8.
		/// </summary>
		public byte ReadByte()
		{
			int remainingBits = bytes.Length * 8 - bitOffset;
			if (remainingBits <= 0)
				throw new IndexOutOfRangeException("No more bits to read.");

			int bitsToRead = Math.Min(8, remainingBits);
			byte result = 0;

			for (int i = 0; i < bitsToRead; i++)
			{
				int byteIndex = bitOffset / 8;
				int bitIndex = bitOffset % 8;

				bool bitSet = (bytes[byteIndex] & (1 << bitIndex)) != 0;
				if (bitSet)
					result |= (byte)(1 << i);

				bitOffset++;
			}
			return result;
		}
	}
}
