using System;
using System.IO;
using System.Runtime.InteropServices;

namespace StbImageSharp
{
	internal class DecodingContext
	{
		public int img_x;
		public int img_y;
		public int img_n;
		public int img_out_n;
		public Stream stream;
		private long _initialPosition;
		private byte[] _internalBuffer = new byte[256];

		public DecodingContext(Stream str)
		{
			if (str == null)
			{
				throw new ArgumentNullException("str");
			}

			stream = str;
			_initialPosition = str.Position;
		}

		public byte Get8()
		{
			return (byte)stream.ReadByte();
		}

		public int Get16BigEndian()
		{
			int z = Get8();
			return (z << 8) + Get8();
		}

		public uint Get32BigEndian()
		{
			uint z = (uint)Get16BigEndian();
			return (uint)((z << 16) + Get16BigEndian());
		}

		public int Get16LittleEndian()
		{
			int z = Get8();
			return z + (Get8() << 8);
		}

		public uint Get32LittleEndian()
		{
			uint z = (uint)Get16LittleEndian();
			return (uint)(z + (Get16LittleEndian() << 16));
		}

		public unsafe bool Getn(byte *buffer, int n)
		{
			if (n > _internalBuffer.Length)
			{
				_internalBuffer = new byte[n * 2];
			}

			var cnt = stream.Read(_internalBuffer, 0, n);

			Marshal.Copy(_internalBuffer, 0, new IntPtr(buffer), cnt);

			return cnt == n;
		}

		public void Rewind()
		{
			stream.Seek(_initialPosition, SeekOrigin.Begin);
		}

		public void Skip(int length)
		{
			stream.Seek(length, SeekOrigin.Current);
		}
	}
}
