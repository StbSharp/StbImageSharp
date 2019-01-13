using System;

namespace StbImageSharp
{
	public unsafe class ZBuffer
	{
		public int[] stbi__zlength_base =
		{
			3, 4, 5, 6, 7, 8, 9, 10, 11, 13, 15, 17, 19, 23, 27, 31, 35, 43, 51, 59, 67,
			83, 99, 115, 131, 163, 195, 227, 258, 0, 0
		};

		public int[] stbi__zlength_extra =
		{
			0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4, 5,
			5, 5, 5, 0, 0, 0
		};

		public int[] stbi__zdist_base =
		{
			1, 2, 3, 4, 5, 7, 9, 13, 17, 25, 33, 49, 65, 97, 129, 193, 257, 385, 513, 769,
			1025, 1537, 2049, 3073, 4097, 6145, 8193, 12289, 16385, 24577, 0, 0
		};

		public int[] stbi__zdist_extra =
		{
			0, 0, 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 8, 8, 9, 9, 10, 10, 11,
			11, 12, 12, 13, 13
		};

		public byte[] length_dezigzag = { 16, 17, 18, 0, 8, 7, 9, 6, 10, 5, 11, 4, 12, 3, 13, 2, 14, 1, 15 };

		public byte[] stbi__zdefault_length =
		{
			8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8,
			8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8,
			8, 8, 8,
			8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8,
			8, 8, 8,
			8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8,
			8, 8, 8,
			8, 8, 8, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9,
			9, 9, 9,
			9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9,
			9, 9, 9,
			9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9,
			9, 7, 7,
			7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 8, 8, 8, 8, 8, 8, 8, 8
		};

		public byte[] stbi__zdefault_distance =
		{
			5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5,
			5, 5, 5, 5, 5, 5, 5, 5
		};

		public byte* _zbuffer;
		public byte* _zbuffer_end;
		public int _num_bits;
		public uint _code_buffer;
		public sbyte* _zout;
		public sbyte* _zout_start;
		public sbyte* _zout_end;
		public int _z_expendable;
		public ZHuffman _z_length;
		public ZHuffman _z_distance;

		public ZBuffer()
		{
			_z_length = new ZHuffman();
			_z_distance = new ZHuffman();
		}

		public byte Get8()
		{
			if (_zbuffer >= _zbuffer_end)
				return 0;
			var result = *_zbuffer;
			_zbuffer++;
			return result;
		}

		public void FillBits()
		{
			do
			{
				_code_buffer |= (uint)Get8() << _num_bits;
				_num_bits += 8;
			} while (_num_bits <= 24);
		}

		public uint Receive(int n)
		{
			uint k;
			if (_num_bits < n)
				FillBits();
			k = (uint)(_code_buffer & ((1 << n) - 1));
			_code_buffer >>= n;
			_num_bits -= n;
			return k;
		}

		public int DecodeSlowpath(ZHuffman z)
		{
			int b;
			int s;
			int k;
			k = Utility.stbi__bit_reverse((int)_code_buffer, 16);
			for (s = 9 + 1; ; ++s)
			{
				if (k < z.maxcode[s])
					break;
			}

			if (s == 16)
				return -1;
			b = (k >> (16 - s)) - z.firstcode[s] + z.firstsymbol[s];
			_code_buffer >>= s;
			_num_bits -= s;
			return z.value[b];
		}

		public int Decode(ZHuffman z)
		{
			int b;
			int s;
			if (_num_bits < 16)
				FillBits();
			b = z.fast[_code_buffer & ((1 << 9) - 1)];
			if (b != 0)
			{
				s = b >> 9;
				_code_buffer >>= s;
				_num_bits -= s;
				return b & 511;
			}

			return DecodeSlowpath(z);
		}

		public int Expand(sbyte* zout, int n)
		{
			sbyte* q;
			int cur;
			int limit;
			int old_limit;
			_zout = zout;
			if (_z_expendable == 0)
				throw new Exception("output buffer limit");
			cur = (int)(_zout - _zout_start);
			limit = old_limit = (int)(_zout_end - _zout_start);
			while ((cur + n) > limit)
			{
				limit *= 2;
			}

			q = (sbyte*)CRuntime.realloc(_zout_start, (ulong)limit);
			if (q == null)
				throw new Exception("outofmem");
			_zout_start = q;
			_zout = q + cur;
			_zout_end = q + limit;
			return 1;
		}

		public int ParseHuffmanBlock()
		{
			sbyte* zout = _zout;
			for (; ; )
			{
				int z = Decode(_z_length);
				if (z < 256)
				{
					if (z < 0)
						throw new Exception("bad huffman code");
					if (zout >= _zout_end)
					{
						if (Expand(zout, 1) == 0)
							return 0;
						zout = _zout;
					}

					*zout++ = (sbyte)z;
				}
				else
				{
					byte* p;
					int len;
					int dist;
					if (z == 256)
					{
						_zout = zout;
						return 1;
					}

					z -= 257;
					len = stbi__zlength_base[z];
					if (stbi__zlength_extra[z] != 0)
						len += (int)Receive(stbi__zlength_extra[z]);
					z = Decode(_z_distance);
					if (z < 0)
						throw new Exception("bad huffman code");
					dist = stbi__zdist_base[z];
					if (stbi__zdist_extra[z] != 0)
						dist += (int)Receive(stbi__zdist_extra[z]);
					if ((zout - _zout_start) < dist)
						throw new Exception("bad dist");
					if ((zout + len) > _zout_end)
					{
						if (Expand(zout, len) == 0)
							return 0;
						zout = _zout;
					}

					p = (byte*)(zout - dist);
					if (dist == 1)
					{
						byte v = *p;
						if (len != 0)
						{
							do
								*zout++ = (sbyte)v;
							while ((--len) != 0);
						}
					}
					else
					{
						if (len != 0)
						{
							do
								*zout++ = (sbyte)*p++;
							while ((--len) != 0);
						}
					}
				}
			}
		}

		public int ComputeHuffmanCodes()
		{
			ZHuffman z_codelength = new ZHuffman();
			byte* lencodes = stackalloc byte[286 + 32 + 137];
			byte* codelength_sizes = stackalloc byte[19];
			int i;
			int n;
			int hlit = (int)(Receive(5) + 257);
			int hdist = (int)(Receive(5) + 1);
			int hclen = (int)(Receive(4) + 4);
			int ntot = hlit + hdist;
			CRuntime.memset(codelength_sizes, 0, (ulong)(19 * sizeof(byte)));
			for (i = 0; i < hclen; ++i)
			{
				int s = (int)Receive(3);
				codelength_sizes[length_dezigzag[i]] = (byte)s;
			}

			if (z_codelength.ZbuildHuffman(codelength_sizes, 19) == 0)
				return 0;
			n = 0;
			while (n < ntot)
			{
				int c = Decode(z_codelength);
				if ((c < 0) || (c >= 19))
					throw new Exception("bad codelengths");
				if (c < 16)
					lencodes[n++] = (byte)c;
				else
				{
					byte fill = 0;
					if (c == 16)
					{
						c = (int)(Receive(2) + 3);
						if (n == 0)
							throw new Exception("bad codelengths");
						fill = lencodes[n - 1];
					}
					else if (c == 17)
						c = (int)(Receive(3) + 3);
					else
					{
						c = (int)(Receive(7) + 11);
					}

					if ((ntot - n) < c)
						throw new Exception("bad codelengths");
					CRuntime.memset(lencodes + n, fill, (ulong)c);
					n += c;
				}
			}

			if (n != ntot)
				throw new Exception("bad codelengths");
			if (_z_length.ZbuildHuffman(lencodes, hlit) == 0)
				return 0;
			if (_z_distance.ZbuildHuffman(lencodes + hlit, hdist) == 0)
				return 0;
			return 1;
		}

		public int ParseUncompressedBlock()
		{
			byte* header = stackalloc byte[4];
			int len;
			int nlen;
			int k;
			if ((_num_bits & 7) != 0)
				Receive(_num_bits & 7);
			k = 0;
			while (_num_bits > 0)
			{
				header[k++] = (byte)(_code_buffer & 255);
				_code_buffer >>= 8;
				_num_bits -= 8;
			}

			while (k < 4)
			{
				header[k++] = Get8();
			}

			len = header[1] * 256 + header[0];
			nlen = header[3] * 256 + header[2];
			if (nlen != (len ^ 0xffff))
				throw new Exception("zlib corrupt");
			if ((_zbuffer + len) > _zbuffer_end)
				throw new Exception("read past buffer");
			if ((_zout + len) > _zout_end)
				if (Expand(_zout, len) == 0)
					return 0;
			CRuntime.memcpy(_zout, _zbuffer, (ulong)len);
			_zbuffer += len;
			_zout += len;
			return 1;
		}

		public int ParseZlibHeader()
		{
			int cmf = Get8();
			int cm = cmf & 15;
			int flg = Get8();
			if ((cmf * 256 + flg) % 31 != 0)
				throw new Exception("bad zlib header");
			if ((flg & 32) != 0)
				throw new Exception("no preset dict");
			if (cm != 8)
				throw new Exception("bad compression");
			return 1;
		}

		public int ParseZlib(int parse_header)
		{
			int final;
			int type;
			if (parse_header != 0)
				if (ParseZlibHeader() == 0)
					return 0;
			_num_bits = 0;
			_code_buffer = 0;
			do
			{
				final = (int)Receive(1);
				type = (int)Receive(2);
				if (type == 0)
				{
					if (ParseUncompressedBlock() == 0)
						return 0;
				}
				else if (type == 3)
				{
					return 0;
				}
				else
				{
					if (type == 1)
					{
						fixed (byte* b = stbi__zdefault_length)
						{
							if (_z_length.ZbuildHuffman(b, 288) == 0)
								return 0;
						}

						fixed (byte* b = stbi__zdefault_distance)
						{
							if (_z_distance.ZbuildHuffman(b, 32) == 0)
								return 0;
						}
					}
					else
					{
						if (ComputeHuffmanCodes() == 0)
							return 0;
					}

					if (ParseHuffmanBlock() == 0)
						return 0;
				}
			} while (final == 0);

			return 1;
		}

		public int DoZlib(sbyte* obuf, int olen, int exp, int parse_header)
		{
			_zout_start = obuf;
			_zout = obuf;
			_zout_end = obuf + olen;
			_z_expendable = exp;
			return ParseZlib(parse_header);
		}

		public static sbyte* ZlibDecodeMallocGuessSize(sbyte* buffer, int len, int initial_size, int* outlen)
		{
			ZBuffer a = new ZBuffer();
			sbyte* p = (sbyte*)CRuntime.malloc((ulong)initial_size);
			if (p == null)
				return null;
			a._zbuffer = (byte*)buffer;
			a._zbuffer_end = (byte*)buffer + len;
			if (a.DoZlib(p, initial_size, 1, 1) != 0)
			{
				if (outlen != null)
					*outlen = (int)(a._zout - a._zout_start);
				return a._zout_start;
			}
			else
			{
				CRuntime.free(a._zout_start);
				return null;
			}

		}

		public static sbyte* ZlibDecodeMalloc(sbyte* buffer, int len, int* outlen)
		{
			return ZlibDecodeMallocGuessSize(buffer, len, 16384, outlen);
		}

		public static sbyte* ZlibDecodeMallocGuessSizeHeaderFlag(sbyte* buffer, int len, int initial_size,
			int* outlen, int parse_header)
		{
			ZBuffer a = new ZBuffer();
			sbyte* p = (sbyte*)CRuntime.malloc((ulong)initial_size);
			if (p == null)
				return null;
			a._zbuffer = (byte*)buffer;
			a._zbuffer_end = (byte*)buffer + len;
			if (a.DoZlib(p, initial_size, 1, parse_header) != 0)
			{
				if (outlen != null)
					*outlen = (int)(a._zout - a._zout_start);
				return a._zout_start;
			}
			else
			{
				CRuntime.free(a._zout_start);
				return null;
			}

		}

		public static int ZlibDecodeBuffer(sbyte* obuffer, int olen, sbyte* ibuffer, int ilen)
		{
			ZBuffer a = new ZBuffer();
			a._zbuffer = (byte*)ibuffer;
			a._zbuffer_end = (byte*)ibuffer + ilen;
			if (a.DoZlib(obuffer, olen, 0, 1) != 0)
				return (int)(a._zout - a._zout_start);
			else
				return -1;
		}

		public static sbyte* ZlibDecodeNoHeaderMalloc(sbyte* buffer, int len, int* outlen)
		{
			ZBuffer a = new ZBuffer();
			sbyte* p = (sbyte*)CRuntime.malloc((ulong)16384);
			if (p == null)
				return null;
			a._zbuffer = (byte*)buffer;
			a._zbuffer_end = (byte*)buffer + len;
			if (a.DoZlib(p, 16384, 1, 0) != 0)
			{
				if (outlen != null)
					*outlen = (int)(a._zout - a._zout_start);
				return a._zout_start;
			}
			else
			{
				CRuntime.free(a._zout_start);
				return null;
			}
		}

		public static int ZlibDecodeNoHeaderBuffer(sbyte* obuffer, int olen, sbyte* ibuffer, int ilen)
		{
			ZBuffer a = new ZBuffer
			{
				_zbuffer = (byte*)ibuffer,
				_zbuffer_end = (byte*)ibuffer + ilen
			};

			if (a.DoZlib(obuffer, olen, 0, 0) != 0)
				return (int)(a._zout - a._zout_start);
			else
				return -1;
		}
	}
}