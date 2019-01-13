using System;

namespace StbImageSharp
{
	public unsafe class ZHuffman
	{
		public ushort[] fast = new ushort[1 << 9];
		public ushort[] firstcode = new ushort[16];
		public int[] maxcode = new int[17];
		public ushort[] firstsymbol = new ushort[16];
		public byte[] size = new byte[288];
		public ushort[] value = new ushort[288];

		public int ZbuildHuffman(byte* sizelist, int num)
		{
			int i;
			int k = 0;
			int code;
			int* next_code = stackalloc int[16];
			int* sizes = stackalloc int[17];
			CRuntime.memset(sizes, 0, (ulong)sizeof(int));
			Array.Clear(fast, 0, fast.Length);
			for (i = 0; i < num; ++i)
			{
				++sizes[sizelist[i]];
			}

			sizes[0] = 0;
			for (i = 1; i < 16; ++i)
			{
				if (sizes[i] > (1 << i))
					throw new Exception("bad sizes");
			}

			code = 0;
			for (i = 1; i < 16; ++i)
			{
				next_code[i] = code;
				firstcode[i] = (ushort)code;
				firstsymbol[i] = (ushort)k;
				code = code + sizes[i];
				if (sizes[i] != 0)
					if ((code - 1) >= (1 << i))
						throw new Exception("bad codelengths");
				maxcode[i] = code << (16 - i);
				code <<= 1;
				k += sizes[i];
			}

			maxcode[16] = 0x10000;
			for (i = 0; i < num; ++i)
			{
				int s = sizelist[i];
				if (s != 0)
				{
					int c = next_code[s] - firstcode[s] + firstsymbol[s];
					ushort fastv = (ushort)((s << 9) | i);
					size[c] = (byte)s;
					value[c] = (ushort)i;
					if (s <= 9)
					{
						int j = Utility.stbi__bit_reverse(next_code[s], s);
						while (j < (1 << 9))
						{
							fast[j] = fastv;
							j += 1 << s;
						}
					}

					++next_code[s];
				}
			}

			return 1;
		}
	}
}
