using System;
using System.Runtime.InteropServices;

namespace StbImageSharp
{
	public unsafe class PngDecoder : BaseDecoder
	{
		public const int STBI__F_none = 0;
		public const int STBI__F_sub = 1;
		public const int STBI__F_up = 2;
		public const int STBI__F_avg = 3;
		public const int STBI__F_paeth = 4;
		public const int STBI__F_avg_first = 5;
		public const int STBI__F_paeth_first = 6;

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

		public byte[] png_sig = { 137, 80, 78, 71, 13, 10, 26, 10 };

		public byte[] first_row_filter =
		{
			STBI__F_none, STBI__F_sub, STBI__F_none, STBI__F_avg_first,
			STBI__F_paeth_first
		};

		public byte[] stbi__depth_scale_table = { 0, 0xff, 0x55, 0, 0x11, 0, 0, 0, 0x01 };
		public int stbi__unpremultiply_on_load = 0;
		public int stbi__de_iphone_flag = 0;

		[StructLayout(LayoutKind.Sequential)]
		public struct stbi__zhuffman
		{
			public fixed ushort fast[1 << 9];
			public fixed ushort firstcode[16];
			public fixed int maxcode[17];
			public fixed ushort firstsymbol[16];
			public fixed byte size[288];
			public fixed ushort value[288];
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct stbi__zbuf
		{
			public byte* zbuffer;
			public byte* zbuffer_end;
			public int num_bits;
			public uint code_buffer;
			public sbyte* zout;
			public sbyte* zout_start;
			public sbyte* zout_end;
			public int z_expandable;
			public stbi__zhuffman z_length;
			public stbi__zhuffman z_distance;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct stbi__pngchunk
		{
			public uint length;
			public uint type;
		}

		public byte* idata;
		public byte* expanded;
		public byte* _out_;
		public int depth;

		public int ZbuildHuffman(stbi__zhuffman* z, byte* sizelist, int num)
		{
			int i;
			int k = 0;
			int code;
			int* next_code = stackalloc int[16];
			int* sizes = stackalloc int[17];
			CRuntime.memset(sizes, 0, (ulong)sizeof(int));
			CRuntime.memset(z->fast, 0, (ulong)((1 << 9) * sizeof(ushort)));
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
				z->firstcode[i] = (ushort)code;
				z->firstsymbol[i] = (ushort)k;
				code = code + sizes[i];
				if (sizes[i] != 0)
					if ((code - 1) >= (1 << i))
						throw new Exception("bad codelengths");
				z->maxcode[i] = code << (16 - i);
				code <<= 1;
				k += sizes[i];
			}

			z->maxcode[16] = 0x10000;
			for (i = 0; i < num; ++i)
			{
				int s = sizelist[i];
				if (s != 0)
				{
					int c = next_code[s] - z->firstcode[s] + z->firstsymbol[s];
					ushort fastv = (ushort)((s << 9) | i);
					z->size[c] = (byte)s;
					z->value[c] = (ushort)i;
					if (s <= 9)
					{
						int j = Utility.stbi__bit_reverse(next_code[s], s);
						while (j < (1 << 9))
						{
							z->fast[j] = fastv;
							j += 1 << s;
						}
					}

					++next_code[s];
				}
			}

			return 1;
		}

		public byte stbi__zget8(stbi__zbuf* z)
		{
			if (z->zbuffer >= z->zbuffer_end)
				return 0;
			return *z->zbuffer++;
		}

		public void stbi__fill_bits(stbi__zbuf* z)
		{
			do
			{
				z->code_buffer |= (uint)stbi__zget8(z) << z->num_bits;
				z->num_bits += 8;
			} while (z->num_bits <= 24);
		}

		public uint stbi__zreceive(stbi__zbuf* z, int n)
		{
			uint k;
			if (z->num_bits < n)
				stbi__fill_bits(z);
			k = (uint)(z->code_buffer & ((1 << n) - 1));
			z->code_buffer >>= n;
			z->num_bits -= n;
			return k;
		}

		public int stbi__zhuffman_decode_slowpath(stbi__zbuf* a, stbi__zhuffman* z)
		{
			int b;
			int s;
			int k;
			k = Utility.stbi__bit_reverse((int)a->code_buffer, 16);
			for (s = 9 + 1; ; ++s)
			{
				if (k < z->maxcode[s])
					break;
			}

			if (s == 16)
				return -1;
			b = (k >> (16 - s)) - z->firstcode[s] + z->firstsymbol[s];
			a->code_buffer >>= s;
			a->num_bits -= s;
			return z->value[b];
		}

		public int stbi__zhuffman_decode(stbi__zbuf* a, stbi__zhuffman* z)
		{
			int b;
			int s;
			if (a->num_bits < 16)
				stbi__fill_bits(a);
			b = z->fast[a->code_buffer & ((1 << 9) - 1)];
			if (b != 0)
			{
				s = b >> 9;
				a->code_buffer >>= s;
				a->num_bits -= s;
				return b & 511;
			}

			return stbi__zhuffman_decode_slowpath(a, z);
		}

		public int stbi__zexpand(stbi__zbuf* z, sbyte* zout, int n)
		{
			sbyte* q;
			int cur;
			int limit;
			int old_limit;
			z->zout = zout;
			if (z->z_expandable == 0)
				throw new Exception("output buffer limit");
			cur = (int)(z->zout - z->zout_start);
			limit = old_limit = (int)(z->zout_end - z->zout_start);
			while ((cur + n) > limit)
			{
				limit *= 2;
			}

			q = (sbyte*)CRuntime.realloc(z->zout_start, (ulong)limit);
			if (q == null)
				throw new Exception("outofmem");
			z->zout_start = q;
			z->zout = q + cur;
			z->zout_end = q + limit;
			return 1;
		}

		public int stbi__parse_huffman_block(stbi__zbuf* a)
		{
			sbyte* zout = a->zout;
			for (; ; )
			{
				int z = stbi__zhuffman_decode(a, &a->z_length);
				if (z < 256)
				{
					if (z < 0)
						throw new Exception("bad huffman code");
					if (zout >= a->zout_end)
					{
						if (stbi__zexpand(a, zout, 1) == 0)
							return 0;
						zout = a->zout;
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
						a->zout = zout;
						return 1;
					}

					z -= 257;
					len = stbi__zlength_base[z];
					if (stbi__zlength_extra[z] != 0)
						len += (int)stbi__zreceive(a, stbi__zlength_extra[z]);
					z = stbi__zhuffman_decode(a, &a->z_distance);
					if (z < 0)
						throw new Exception("bad huffman code");
					dist = stbi__zdist_base[z];
					if (stbi__zdist_extra[z] != 0)
						dist += (int)stbi__zreceive(a, stbi__zdist_extra[z]);
					if ((zout - a->zout_start) < dist)
						throw new Exception("bad dist");
					if ((zout + len) > a->zout_end)
					{
						if (stbi__zexpand(a, zout, len) == 0)
							return 0;
						zout = a->zout;
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

		public int stbi__compute_huffman_codes(stbi__zbuf* a)
		{
			stbi__zhuffman z_codelength = new stbi__zhuffman();
			byte* lencodes = stackalloc byte[286 + 32 + 137];
			byte* codelength_sizes = stackalloc byte[19];
			int i;
			int n;
			int hlit = (int)(stbi__zreceive(a, 5) + 257);
			int hdist = (int)(stbi__zreceive(a, 5) + 1);
			int hclen = (int)(stbi__zreceive(a, 4) + 4);
			int ntot = hlit + hdist;
			CRuntime.memset(codelength_sizes, 0, (ulong)(19 * sizeof(byte)));
			for (i = 0; i < hclen; ++i)
			{
				int s = (int)stbi__zreceive(a, 3);
				codelength_sizes[length_dezigzag[i]] = (byte)s;
			}

			if (ZbuildHuffman(&z_codelength, codelength_sizes, 19) == 0)
				return 0;
			n = 0;
			while (n < ntot)
			{
				int c = stbi__zhuffman_decode(a, &z_codelength);
				if ((c < 0) || (c >= 19))
					throw new Exception("bad codelengths");
				if (c < 16)
					lencodes[n++] = (byte)c;
				else
				{
					byte fill = 0;
					if (c == 16)
					{
						c = (int)(stbi__zreceive(a, 2) + 3);
						if (n == 0)
							throw new Exception("bad codelengths");
						fill = lencodes[n - 1];
					}
					else if (c == 17)
						c = (int)(stbi__zreceive(a, 3) + 3);
					else
					{
						c = (int)(stbi__zreceive(a, 7) + 11);
					}

					if ((ntot - n) < c)
						throw new Exception("bad codelengths");
					CRuntime.memset(lencodes + n, fill, (ulong)c);
					n += c;
				}
			}

			if (n != ntot)
				throw new Exception("bad codelengths");
			if (ZbuildHuffman(&a->z_length, lencodes, hlit) == 0)
				return 0;
			if (ZbuildHuffman(&a->z_distance, lencodes + hlit, hdist) == 0)
				return 0;
			return 1;
		}

		public int stbi__parse_uncompressed_block(stbi__zbuf* a)
		{
			byte* header = stackalloc byte[4];
			int len;
			int nlen;
			int k;
			if ((a->num_bits & 7) != 0)
				stbi__zreceive(a, a->num_bits & 7);
			k = 0;
			while (a->num_bits > 0)
			{
				header[k++] = (byte)(a->code_buffer & 255);
				a->code_buffer >>= 8;
				a->num_bits -= 8;
			}

			while (k < 4)
			{
				header[k++] = stbi__zget8(a);
			}

			len = header[1] * 256 + header[0];
			nlen = header[3] * 256 + header[2];
			if (nlen != (len ^ 0xffff))
				throw new Exception("zlib corrupt");
			if ((a->zbuffer + len) > a->zbuffer_end)
				throw new Exception("read past buffer");
			if ((a->zout + len) > a->zout_end)
				if (stbi__zexpand(a, a->zout, len) == 0)
					return 0;
			CRuntime.memcpy(a->zout, a->zbuffer, (ulong)len);
			a->zbuffer += len;
			a->zout += len;
			return 1;
		}

		public int stbi__parse_zlib_header(stbi__zbuf* a)
		{
			int cmf = stbi__zget8(a);
			int cm = cmf & 15;
			int flg = stbi__zget8(a);
			if ((cmf * 256 + flg) % 31 != 0)
				throw new Exception("bad zlib header");
			if ((flg & 32) != 0)
				throw new Exception("no preset dict");
			if (cm != 8)
				throw new Exception("bad compression");
			return 1;
		}

		public int stbi__parse_zlib(stbi__zbuf* a, int parse_header)
		{
			int final;
			int type;
			if (parse_header != 0)
				if (stbi__parse_zlib_header(a) == 0)
					return 0;
			a->num_bits = 0;
			a->code_buffer = 0;
			do
			{
				final = (int)stbi__zreceive(a, 1);
				type = (int)stbi__zreceive(a, 2);
				if (type == 0)
				{
					if (stbi__parse_uncompressed_block(a) == 0)
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
							if (ZbuildHuffman(&a->z_length, b, 288) == 0)
								return 0;
						}

						fixed (byte* b = stbi__zdefault_distance)
						{
							if (ZbuildHuffman(&a->z_distance, b, 32) == 0)
								return 0;
						}
					}
					else
					{
						if (stbi__compute_huffman_codes(a) == 0)
							return 0;
					}

					if (stbi__parse_huffman_block(a) == 0)
						return 0;
				}
			} while (final == 0);

			return 1;
		}

		public int stbi__do_zlib(stbi__zbuf* a, sbyte* obuf, int olen, int exp, int parse_header)
		{
			a->zout_start = obuf;
			a->zout = obuf;
			a->zout_end = obuf + olen;
			a->z_expandable = exp;
			return stbi__parse_zlib(a, parse_header);
		}

		public sbyte* stbi_zlib_decode_malloc_guesssize(sbyte* buffer, int len, int initial_size, int* outlen)
		{
			stbi__zbuf a = new stbi__zbuf();
			sbyte* p = (sbyte*)CRuntime.malloc((ulong)initial_size);
			if (p == null)
				return null;
			a.zbuffer = (byte*)buffer;
			a.zbuffer_end = (byte*)buffer + len;
			if (stbi__do_zlib(&a, p, initial_size, 1, 1) != 0)
			{
				if (outlen != null)
					*outlen = (int)(a.zout - a.zout_start);
				return a.zout_start;
			}
			else
			{
				CRuntime.free(a.zout_start);
				return null;
			}

		}

		public sbyte* stbi_zlib_decode_malloc(sbyte* buffer, int len, int* outlen)
		{
			return stbi_zlib_decode_malloc_guesssize(buffer, len, 16384, outlen);
		}

		public sbyte* stbi_zlib_decode_malloc_guesssize_headerflag(sbyte* buffer, int len, int initial_size,
			int* outlen, int parse_header)
		{
			stbi__zbuf a = new stbi__zbuf();
			sbyte* p = (sbyte*)CRuntime.malloc((ulong)initial_size);
			if (p == null)
				return null;
			a.zbuffer = (byte*)buffer;
			a.zbuffer_end = (byte*)buffer + len;
			if (stbi__do_zlib(&a, p, initial_size, 1, parse_header) != 0)
			{
				if (outlen != null)
					*outlen = (int)(a.zout - a.zout_start);
				return a.zout_start;
			}
			else
			{
				CRuntime.free(a.zout_start);
				return null;
			}

		}

		public int stbi_zlib_decode_buffer(sbyte* obuffer, int olen, sbyte* ibuffer, int ilen)
		{
			stbi__zbuf a = new stbi__zbuf();
			a.zbuffer = (byte*)ibuffer;
			a.zbuffer_end = (byte*)ibuffer + ilen;
			if (stbi__do_zlib(&a, obuffer, olen, 0, 1) != 0)
				return (int)(a.zout - a.zout_start);
			else
				return -1;
		}

		public sbyte* stbi_zlib_decode_noheader_malloc(sbyte* buffer, int len, int* outlen)
		{
			stbi__zbuf a = new stbi__zbuf();
			sbyte* p = (sbyte*)CRuntime.malloc((ulong)16384);
			if (p == null)
				return null;
			a.zbuffer = (byte*)buffer;
			a.zbuffer_end = (byte*)buffer + len;
			if (stbi__do_zlib(&a, p, 16384, 1, 0) != 0)
			{
				if (outlen != null)
					*outlen = (int)(a.zout - a.zout_start);
				return a.zout_start;
			}
			else
			{
				CRuntime.free(a.zout_start);
				return null;
			}

		}

		public int stbi_zlib_decode_noheader_buffer(sbyte* obuffer, int olen, sbyte* ibuffer, int ilen)
		{
			stbi__zbuf a = new stbi__zbuf();
			a.zbuffer = (byte*)ibuffer;
			a.zbuffer_end = (byte*)ibuffer + ilen;
			if (stbi__do_zlib(&a, obuffer, olen, 0, 0) != 0)
				return (int)(a.zout - a.zout_start);
			else
				return -1;
		}

		public stbi__pngchunk Get_chunk_header()
		{
			stbi__pngchunk c = new stbi__pngchunk();
			c.length = Context.Get32BigEndian();
			c.type = Context.Get32BigEndian();
			return c;
		}

		public int stbi__check_png_header()
		{
			int i;
			for (i = 0; i < 8; ++i)
			{
				if (Context.Get8() != png_sig[i])
					throw new Exception("bad png sig");
			}

			return 1;
		}

		public int stbi__paeth(int a, int b, int c)
		{
			int p = a + b - c;
			int pa = CRuntime.abs(p - a);
			int pb = CRuntime.abs(p - b);
			int pc = CRuntime.abs(p - c);
			if ((pa <= pb) && (pa <= pc))
				return a;
			if (pb <= pc)
				return b;
			return c;
		}

		public int stbi__create_png_image_raw(byte* raw, uint raw_len, int out_n, uint x, uint y, int depth, int color)
		{
			int bytes = depth == 16 ? 2 : 1;
			uint i;
			uint j;
			uint stride = (uint)(x * out_n * bytes);
			uint img_len;
			uint img_width_bytes;
			int k;
			int img_n = Context.img_n;
			int output_bytes = out_n * bytes;
			int filter_bytes = img_n * bytes;
			int width = (int)x;
			_out_ = (byte*)Utility.MallocMad3((int)x, (int)y, output_bytes, 0);
			if (_out_ == null)
				throw new Exception("outofmem");
			img_width_bytes = (uint)(((img_n * x * depth) + 7) >> 3);
			img_len = (img_width_bytes + 1) * y;
			if (raw_len < img_len)
				throw new Exception("not enough pixels");
			for (j = 0; j < y; ++j)
			{
				byte* cur = _out_ + stride * j;
				byte* prior;
				int filter = *raw++;
				if (filter > 4)
					throw new Exception("invalid filter");
				if (depth < 8)
				{
					cur += x * out_n - img_width_bytes;
					filter_bytes = 1;
					width = (int)img_width_bytes;
				}

				prior = cur - stride;
				if (j == 0)
					filter = first_row_filter[filter];
				for (k = 0; k < filter_bytes; ++k)
				{
					switch (filter)
					{
						case STBI__F_none:
							cur[k] = raw[k];
							break;
						case STBI__F_sub:
							cur[k] = raw[k];
							break;
						case STBI__F_up:
							cur[k] = (byte)((raw[k] + prior[k]) & 255);
							break;
						case STBI__F_avg:
							cur[k] = (byte)((raw[k] + (prior[k] >> 1)) & 255);
							break;
						case STBI__F_paeth:
							cur[k] = (byte)((raw[k] + stbi__paeth(0, prior[k], 0)) & 255);
							break;
						case STBI__F_avg_first:
							cur[k] = raw[k];
							break;
						case STBI__F_paeth_first:
							cur[k] = raw[k];
							break;
					}
				}

				if (depth == 8)
				{
					if (img_n != out_n)
						cur[img_n] = 255;
					raw += img_n;
					cur += out_n;
					prior += out_n;
				}
				else if (depth == 16)
				{
					if (img_n != out_n)
					{
						cur[filter_bytes] = 255;
						cur[filter_bytes + 1] = 255;
					}

					raw += filter_bytes;
					cur += output_bytes;
					prior += output_bytes;
				}
				else
				{
					raw += 1;
					cur += 1;
					prior += 1;
				}

				if ((depth < 8) || (img_n == out_n))
				{
					int nk = (width - 1) * filter_bytes;
					switch (filter)
					{
						case STBI__F_none:
							CRuntime.memcpy(cur, raw, (ulong)nk);
							break;
						case STBI__F_sub:
							for (k = 0; k < nk; ++k)
							{
								cur[k] = (byte)((raw[k] + cur[k - filter_bytes]) & 255);
							}

							break;
						case STBI__F_up:
							for (k = 0; k < nk; ++k)
							{
								cur[k] = (byte)((raw[k] + prior[k]) & 255);
							}

							break;
						case STBI__F_avg:
							for (k = 0; k < nk; ++k)
							{
								cur[k] = (byte)((raw[k] + ((prior[k] + cur[k - filter_bytes]) >> 1)) & 255);
							}

							break;
						case STBI__F_paeth:
							for (k = 0; k < nk; ++k)
							{
								cur[k] =
									(byte)
										((raw[k] + stbi__paeth(cur[k - filter_bytes], prior[k],
											  prior[k - filter_bytes])) &
										 255);
							}

							break;
						case STBI__F_avg_first:
							for (k = 0; k < nk; ++k)
							{
								cur[k] = (byte)((raw[k] + (cur[k - filter_bytes] >> 1)) & 255);
							}

							break;
						case STBI__F_paeth_first:
							for (k = 0; k < nk; ++k)
							{
								cur[k] = (byte)((raw[k] + stbi__paeth(cur[k - filter_bytes], 0,
													   0)) & 255);
							}

							break;
					}

					raw += nk;
				}
				else
				{
					switch (filter)
					{
						case STBI__F_none:
							for (i = x - 1;
								i >= 1;
								--i, cur[filter_bytes] = 255, raw += filter_bytes, cur += output_bytes,
								prior += output_bytes)
							{
								for (k = 0; k < filter_bytes; ++k)
								{
									cur[k] = raw[k];
								}
							}

							break;
						case STBI__F_sub:
							for (i = x - 1;
								i >= 1;
								--i, cur[filter_bytes] = 255, raw += filter_bytes, cur += output_bytes,
								prior += output_bytes)
							{
								for (k = 0; k < filter_bytes; ++k)
								{
									cur[k] = (byte)((raw[k] + cur[k - output_bytes]) & 255);
								}
							}

							break;
						case STBI__F_up:
							for (i = x - 1;
								i >= 1;
								--i, cur[filter_bytes] = 255, raw += filter_bytes, cur += output_bytes,
								prior += output_bytes)
							{
								for (k = 0; k < filter_bytes; ++k)
								{
									cur[k] = (byte)((raw[k] + prior[k]) & 255);
								}
							}

							break;
						case STBI__F_avg:
							for (i = x - 1;
								i >= 1;
								--i, cur[filter_bytes] = 255, raw += filter_bytes, cur += output_bytes,
								prior += output_bytes)
							{
								for (k = 0; k < filter_bytes; ++k)
								{
									cur[k] = (byte)((raw[k] + ((prior[k] + cur[k - output_bytes]) >> 1)) & 255);
								}
							}

							break;
						case STBI__F_paeth:
							for (i = x - 1;
								i >= 1;
								--i, cur[filter_bytes] = 255, raw += filter_bytes, cur += output_bytes,
								prior += output_bytes)
							{
								for (k = 0; k < filter_bytes; ++k)
								{
									cur[k] =
										(byte)
											((raw[k] + stbi__paeth(cur[k - output_bytes], prior[k],
												  prior[k - output_bytes])) &
											 255);
								}
							}

							break;
						case STBI__F_avg_first:
							for (i = x - 1;
								i >= 1;
								--i, cur[filter_bytes] = 255, raw += filter_bytes, cur += output_bytes,
								prior += output_bytes)
							{
								for (k = 0; k < filter_bytes; ++k)
								{
									cur[k] = (byte)((raw[k] + (cur[k - output_bytes] >> 1)) & 255);
								}
							}

							break;
						case STBI__F_paeth_first:
							for (i = x - 1;
								i >= 1;
								--i, cur[filter_bytes] = 255, raw += filter_bytes, cur += output_bytes,
								prior += output_bytes)
							{
								for (k = 0; k < filter_bytes; ++k)
								{
									cur[k] = (byte)((raw[k] + stbi__paeth(cur[k - output_bytes], 0,
														   0)) & 255);
								}
							}

							break;
					}

					if (depth == 16)
					{
						cur = _out_ + stride * j;
						for (i = 0; i < x; ++i, cur += output_bytes)
						{
							cur[filter_bytes + 1] = 255;
						}
					}
				}
			}

			if (depth < 8)
			{
				for (j = 0; j < y; ++j)
				{
					byte* cur = _out_ + stride * j;
					byte* _in_ = _out_ + stride * j + x * out_n - img_width_bytes;
					byte scale = (byte)((color == 0) ? stbi__depth_scale_table[depth] : 1);
					if (depth == 4)
					{
						for (k = (int)(x * img_n); k >= 2; k -= 2, ++_in_)
						{
							*cur++ = (byte)(scale * (*_in_ >> 4));
							*cur++ = (byte)(scale * ((*_in_) & 0x0f));
						}

						if (k > 0)
							*cur++ = (byte)(scale * (*_in_ >> 4));
					}
					else if (depth == 2)
					{
						for (k = (int)(x * img_n); k >= 4; k -= 4, ++_in_)
						{
							*cur++ = (byte)(scale * (*_in_ >> 6));
							*cur++ = (byte)(scale * ((*_in_ >> 4) & 0x03));
							*cur++ = (byte)(scale * ((*_in_ >> 2) & 0x03));
							*cur++ = (byte)(scale * ((*_in_) & 0x03));
						}

						if (k > 0)
							*cur++ = (byte)(scale * (*_in_ >> 6));
						if (k > 1)
							*cur++ = (byte)(scale * ((*_in_ >> 4) & 0x03));
						if (k > 2)
							*cur++ = (byte)(scale * ((*_in_ >> 2) & 0x03));
					}
					else if (depth == 1)
					{
						for (k = (int)(x * img_n); k >= 8; k -= 8, ++_in_)
						{
							*cur++ = (byte)(scale * (*_in_ >> 7));
							*cur++ = (byte)(scale * ((*_in_ >> 6) & 0x01));
							*cur++ = (byte)(scale * ((*_in_ >> 5) & 0x01));
							*cur++ = (byte)(scale * ((*_in_ >> 4) & 0x01));
							*cur++ = (byte)(scale * ((*_in_ >> 3) & 0x01));
							*cur++ = (byte)(scale * ((*_in_ >> 2) & 0x01));
							*cur++ = (byte)(scale * ((*_in_ >> 1) & 0x01));
							*cur++ = (byte)(scale * ((*_in_) & 0x01));
						}

						if (k > 0)
							*cur++ = (byte)(scale * (*_in_ >> 7));
						if (k > 1)
							*cur++ = (byte)(scale * ((*_in_ >> 6) & 0x01));
						if (k > 2)
							*cur++ = (byte)(scale * ((*_in_ >> 5) & 0x01));
						if (k > 3)
							*cur++ = (byte)(scale * ((*_in_ >> 4) & 0x01));
						if (k > 4)
							*cur++ = (byte)(scale * ((*_in_ >> 3) & 0x01));
						if (k > 5)
							*cur++ = (byte)(scale * ((*_in_ >> 2) & 0x01));
						if (k > 6)
							*cur++ = (byte)(scale * ((*_in_ >> 1) & 0x01));
					}

					if (img_n != out_n)
					{
						int q;
						cur = _out_ + stride * j;
						if (img_n == 1)
						{
							for (q = (int)(x - 1); q >= 0; --q)
							{
								cur[q * 2 + 1] = 255;
								cur[q * 2 + 0] = cur[q];
							}
						}
						else
						{
							for (q = (int)(x - 1); q >= 0; --q)
							{
								cur[q * 4 + 3] = 255;
								cur[q * 4 + 2] = cur[q * 3 + 2];
								cur[q * 4 + 1] = cur[q * 3 + 1];
								cur[q * 4 + 0] = cur[q * 3 + 0];
							}
						}
					}
				}
			}
			else if (depth == 16)
			{
				byte* cur = _out_;
				ushort* cur16 = (ushort*)cur;
				for (i = 0; i < (x * y * out_n); ++i, cur16++, cur += 2)
				{
					*cur16 = (ushort)((cur[0] << 8) | cur[1]);
				}
			}

			return 1;
		}

		public int stbi__create_png_image(byte* image_data, uint image_data_len, int out_n,
			int depth, int color, int interlaced)
		{
			int bytes = depth == 16 ? 2 : 1;
			int out_bytes = out_n * bytes;
			byte* final;
			int p;
			if (interlaced == 0)
			{
				return stbi__create_png_image_raw(image_data, image_data_len, out_n,
						(uint)Context.img_x,
						(uint)Context.img_y, depth, color);
			}
			final = (byte*)Utility.MallocMad3(Context.img_x, Context.img_y, out_bytes, 0);
			for (p = 0; p < 7; ++p)
			{
				int* xorig = stackalloc int[7];
				xorig[0] = 0;
				xorig[1] = 4;
				xorig[2] = 0;
				xorig[3] = 2;
				xorig[4] = 0;
				xorig[5] = 1;
				xorig[6] = 0;
				int* yorig = stackalloc int[7];
				yorig[0] = 0;
				yorig[1] = 0;
				yorig[2] = 4;
				yorig[3] = 0;
				yorig[4] = 2;
				yorig[5] = 0;
				yorig[6] = 1;
				int* xspc = stackalloc int[7];
				xspc[0] = 8;
				xspc[1] = 8;
				xspc[2] = 4;
				xspc[3] = 4;
				xspc[4] = 2;
				xspc[5] = 2;
				xspc[6] = 1;
				int* yspc = stackalloc int[7];
				yspc[0] = 8;
				yspc[1] = 8;
				yspc[2] = 8;
				yspc[3] = 4;
				yspc[4] = 4;
				yspc[5] = 2;
				yspc[6] = 2;
				int i;
				int j;
				int x;
				int y;
				x = (Context.img_x - xorig[p] + xspc[p] - 1) / xspc[p];
				y = (Context.img_y - yorig[p] + yspc[p] - 1) / yspc[p];
				if ((x != 0) && (y != 0))
				{
					uint img_len = (uint)(((((Context.img_n * x * depth) + 7) >> 3) + 1) * y);
					if (
						stbi__create_png_image_raw(image_data, image_data_len, out_n, (uint)x,
							(uint)y,
							depth, color) == 0)
					{
						CRuntime.free(final);
						return 0;
					}

					for (j = 0; j < y; ++j)
					{
						for (i = 0; i < x; ++i)
						{
							int out_y = j * yspc[p] + yorig[p];
							int out_x = i * xspc[p] + xorig[p];
							CRuntime.memcpy(final + out_y * Context.img_x * out_bytes + out_x * out_bytes,
								_out_ + (j * x + i) * out_bytes,
								(ulong)out_bytes);
						}
					}

					CRuntime.free(_out_);
					image_data += img_len;
					image_data_len -= img_len;
				}
			}

			_out_ = final;
			return 1;
		}

		public int stbi__compute_transparency(byte* tc, int out_n)
		{
			uint i;
			uint pixel_count = (uint)(Context.img_x * Context.img_y);
			byte* p = _out_;
			if (out_n == 2)
			{
				for (i = 0; i < pixel_count; ++i)
				{
					p[1] = (byte)(p[0] == tc[0] ? 0 : 255);
					p += 2;
				}
			}
			else
			{
				for (i = 0; i < pixel_count; ++i)
				{
					if ((p[0] == tc[0]) && (p[1] == tc[1]) && (p[2] == tc[2]))
						p[3] = 0;
					p += 4;
				}
			}

			return 1;
		}

		public int stbi__compute_transparency16(ushort* tc, int out_n)
		{
			uint i;
			uint pixel_count = (uint)(Context.img_x * Context.img_y);
			ushort* p = (ushort*)_out_;
			if (out_n == 2)
			{
				for (i = 0; i < pixel_count; ++i)
				{
					p[1] = (ushort)(p[0] == tc[0] ? 0 : 65535);
					p += 2;
				}
			}
			else
			{
				for (i = 0; i < pixel_count; ++i)
				{
					if ((p[0] == tc[0]) && (p[1] == tc[1]) && (p[2] == tc[2]))
						p[3] = 0;
					p += 4;
				}
			}

			return 1;
		}

		public int stbi__expand_png_palette(byte* palette, int len, int pal_img_n)
		{
			uint i;
			uint pixel_count = (uint)(Context.img_x * Context.img_y);
			byte* p;
			byte* temp_out;
			byte* orig = _out_;
			p = (byte*)Utility.MallocMad2((int)pixel_count, pal_img_n, 0);
			if (p == null)
				throw new Exception("outofmem");
			temp_out = p;
			if (pal_img_n == 3)
			{
				for (i = 0; i < pixel_count; ++i)
				{
					int n = orig[i] * 4;
					p[0] = palette[n];
					p[1] = palette[n + 1];
					p[2] = palette[n + 2];
					p += 3;
				}
			}
			else
			{
				for (i = 0; i < pixel_count; ++i)
				{
					int n = orig[i] * 4;
					p[0] = palette[n];
					p[1] = palette[n + 1];
					p[2] = palette[n + 2];
					p[3] = palette[n + 3];
					p += 4;
				}
			}

			CRuntime.free(_out_);
			_out_ = temp_out;
			return 1;
		}

		public void stbi_set_unpremultiply_on_load(int flag_true_if_should_unpremultiply)
		{
			stbi__unpremultiply_on_load = flag_true_if_should_unpremultiply;
		}

		public void stbi_convert_iphone_png_to_rgb(int flag_true_if_should_convert)
		{
			stbi__de_iphone_flag = flag_true_if_should_convert;
		}

		public void stbi__de_iphone()
		{
			uint i;
			uint pixel_count = (uint)(Context.img_x * Context.img_y);
			byte* p = _out_;
			if (Context.img_out_n == 3)
			{
				for (i = 0; i < pixel_count; ++i)
				{
					byte t = p[0];
					p[0] = p[2];
					p[2] = t;
					p += 3;
				}
			}
			else
			{
				if (stbi__unpremultiply_on_load != 0)
				{
					for (i = 0; i < pixel_count; ++i)
					{
						byte a = p[3];
						byte t = p[0];
						if (a != 0)
						{
							byte half = (byte)(a / 2);
							p[0] = (byte)((p[2] * 255 + half) / a);
							p[1] = (byte)((p[1] * 255 + half) / a);
							p[2] = (byte)((t * 255 + half) / a);
						}
						else
						{
							p[0] = p[2];
							p[2] = t;
						}

						p += 4;
					}
				}
				else
				{
					for (i = 0; i < pixel_count; ++i)
					{
						byte t = p[0];
						p[0] = p[2];
						p[2] = t;
						p += 4;
					}
				}
			}
		}

		public int stbi__parse_png_file(ScanType scan, ColorComponents req_comp)
		{
			byte* palette = stackalloc byte[1024];
			byte pal_img_n = 0;
			byte has_trans = 0;
			byte* tc = stackalloc byte[3];
			ushort* tc16 = stackalloc ushort[3];
			uint ioff = 0;
			uint idata_limit = 0;
			uint i;
			uint pal_len = 0;
			int first = 1;
			int k;
			int interlace = 0;
			int color = 0;
			int is_iphone = 0;
			expanded = null;
			idata = null;
			_out_ = null;
			if (stbi__check_png_header() == 0)
				return 0;
			if (scan == ScanType.Type)
				return 1;
			for (; ; )
			{
				stbi__pngchunk c = Get_chunk_header();
				switch (c.type)
				{
					case (('C') << 24) + (('g') << 16) + (('B') << 8) + 'I':
						is_iphone = 1;
						Context.Skip((int)c.length);
						break;
					case (('I') << 24) + (('H') << 16) + (('D') << 8) + 'R':
					{
						int comp;
						int filter;
						if (first == 0)
							throw new Exception("multiple IHDR");
						first = 0;
						if (c.length != 13)
							throw new Exception("bad IHDR len");
						Context.img_x = (int)Context.Get32BigEndian();
						if (Context.img_x > (1 << 24))
							throw new Exception("too large");
						Context.img_y = (int)Context.Get32BigEndian();
						if (Context.img_y > (1 << 24))
							throw new Exception("too large");
						depth = Context.Get8();
						if ((depth != 1) && (depth != 2) && (depth != 4) && (depth != 8) &&
							(depth != 16))
							throw new Exception("1/2/4/8/16-bit only");
						color = Context.Get8();
						if (color > 6)
							throw new Exception("bad ctype");
						if ((color == 3) && (depth == 16))
							throw new Exception("bad ctype");
						if (color == 3)
							pal_img_n = 3;
						else if ((color & 1) != 0)
							throw new Exception("bad ctype");
						comp = Context.Get8();
						if (comp != 0)
							throw new Exception("bad comp method");
						filter = Context.Get8();
						if (filter != 0)
							throw new Exception("bad filter method");
						interlace = Context.Get8();
						if (interlace > 1)
							throw new Exception("bad interlace method");
						if ((Context.img_x == 0) || (Context.img_y == 0))
							throw new Exception("0-pixel image");
						if (pal_img_n == 0)
						{
							Context.img_n = ((color & 2) != 0 ? 3 : 1) + ((color & 4) != 0 ? 1 : 0);
							if (((1 << 30) / Context.img_x / Context.img_n) < Context.img_y)
								throw new Exception("too large");
							if (scan == ScanType.Header)
								return 1;
						}
						else
						{
							Context.img_n = 1;
							if (((1 << 30) / Context.img_x / 4) < Context.img_y)
								throw new Exception("too large");
						}

						break;
					}
					case (('P') << 24) + (('L') << 16) + (('T') << 8) + 'E':
					{
						if (first != 0)
							throw new Exception("first not IHDR");
						if (c.length > (256 * 3))
							throw new Exception("invalid PLTE");
						pal_len = c.length / 3;
						if (pal_len * 3 != c.length)
							throw new Exception("invalid PLTE");
						for (i = 0; i < pal_len; ++i)
						{
							palette[i * 4 + 0] = (byte)Context.Get8();
							palette[i * 4 + 1] = (byte)Context.Get8();
							palette[i * 4 + 2] = (byte)Context.Get8();
							palette[i * 4 + 3] = 255;
						}

						break;
					}
					case (('t') << 24) + (('R') << 16) + (('N') << 8) + 'S':
					{
						if (first != 0)
							throw new Exception("first not IHDR");
						if (idata != null)
							throw new Exception("tRNS after IDAT");
						if (pal_img_n != 0)
						{
							if (scan == ScanType.Header)
							{
								Context.img_n = 4;
								return 1;
							}

							if (pal_len == 0)
								throw new Exception("tRNS before PLTE");
							if (c.length > pal_len)
								throw new Exception("bad tRNS len");
							pal_img_n = 4;
							for (i = 0; i < c.length; ++i)
							{
								palette[i * 4 + 3] = (byte)Context.Get8();
							}
						}
						else
						{
							if ((Context.img_n & 1) == 0)
								throw new Exception("tRNS with alpha");
							if (c.length != (uint)Context.img_n * 2)
								throw new Exception("bad tRNS len");
							has_trans = 1;
							if (depth == 16)
							{
								for (k = 0; k < Context.img_n; ++k)
								{
									tc16[k] = (ushort)Context.Get16BigEndian();
								}
							}
							else
							{
								for (k = 0; k < Context.img_n; ++k)
								{
									tc[k] = (byte)((byte)(Context.Get16BigEndian() & 255) * stbi__depth_scale_table[depth]);
								}
							}
						}

						break;
					}
					case (('I') << 24) + (('D') << 16) + (('A') << 8) + 'T':
					{
						if (first != 0)
							throw new Exception("first not IHDR");
						if ((pal_img_n != 0) && (pal_len == 0))
							throw new Exception("no PLTE");
						if (scan == ScanType.Header)
						{
							Context.img_n = pal_img_n;
							return 1;
						}

						if (((int)(ioff + c.length)) < ((int)ioff))
							return 0;
						if ((ioff + c.length) > idata_limit)
						{
							uint idata_limit_old = idata_limit;
							byte* p;
							if (idata_limit == 0)
								idata_limit = c.length > 4096 ? c.length : 4096;
							while ((ioff + c.length) > idata_limit)
							{
								idata_limit *= 2;
							}

							p = (byte*)CRuntime.realloc(idata, (ulong)idata_limit);
							if (p == null)
								throw new Exception("outofmem");
							idata = p;
						}

						if (Getn(s, idata + ioff, (int)c.length) == 0)
							throw new Exception("outofdata");
						ioff += c.length;
						break;
					}
					case (('I') << 24) + (('E') << 16) + (('N') << 8) + 'D':
					{
						uint raw_len;
						uint bpl;
						if (first != 0)
							throw new Exception("first not IHDR");
						if (scan != ScanType.Load)
							return 1;
						if (idata == null)
							throw new Exception("no IDAT");
						bpl = (uint)((Context.img_x * depth + 7) / 8);
						raw_len = (uint)(bpl * Context.img_y * Context.img_n + Context.img_y);
						expanded =
							(byte*)
							stbi_zlib_decode_malloc_guesssize_headerflag((sbyte*)idata, (int)ioff,
								(int)raw_len,
								(int*)&raw_len, is_iphone != 0 ? 0 : 1);
						if (expanded == null)
							return 0;
						CRuntime.free(idata);
						idata = null;
						if ((((int)req_comp == (Context.img_n + 1)) && (req_comp != ColorComponents.RedGreenBlue) && (pal_img_n == 0)) ||
							(has_trans != 0))
							Context.img_out_n = Context.img_n + 1;
						else
							Context.img_out_n = Context.img_n;
						if (
							stbi__create_png_image(expanded, (uint)raw_len, (int)Context.img_out_n,
								(int)depth, (int)color,
								(int)interlace) == 0)
							return 0;
						if (has_trans != 0)
						{
							if (depth == 16)
							{
								if (stbi__compute_transparency16(tc16, Context.img_out_n) == 0)
									return 0;
							}
							else
							{
								if (stbi__compute_transparency(tc, Context.img_out_n) == 0)
									return 0;
							}
						}

						if ((is_iphone != 0) && (stbi__de_iphone_flag != 0) && (Context.img_out_n > 2))
							stbi__de_iphone(z);
						if (pal_img_n != 0)
						{
							Context.img_n = pal_img_n;
							Context.img_out_n = pal_img_n;
							if ((int)req_comp >= 3)
								Context.img_out_n = (int)req_comp;
							if (stbi__expand_png_palette(palette, (int)pal_len, (int)Context.img_out_n) == 0)
								return 0;
						}
						else if (has_trans != 0)
						{
							++Context.img_n;
						}

						CRuntime.free(expanded);
						expanded = null;
						return 1;
					}
					default:
						if (first != 0)
							throw new Exception("first not IHDR");
						if ((c.type & (1 << 29)) == 0)
						{
							string invalid_chunk = "XXXX PNG chunk not known";
							throw new Exception(invalid_chunk);
						}

						Context.Skip((int)c.length);
						break;
				}

				Context.Get32BigEndian();
			}
		}

		public byte* stbi__do_png(int* x, int* y, int* n, ColorComponents req_comp)
		{
			byte* result = null;
			if (stbi__parse_png_file(p, (int)ScanType.Load, (int)req_comp) != 0)
			{
				if (p.depth < 8)
					ri->bits_per_channel = 8;
				else
					ri->bits_per_channel = (int)p.depth;
				result = p._out_;
				p._out_ = null;
				if ((req_comp != 0) && (req_comp != Context.img_out_n))
				{
					if (ri->bits_per_channel == 8)
						result = stbi__convert_format((byte*)result, (int)Context.img_out_n, req_comp,
							(uint)Context.img_x,
							(uint)Context.img_y);
					else
						result = stbi__convert_format16((ushort*)result, (int)Context.img_out_n, req_comp,
							(uint)Context.img_x,
							(uint)Context.img_y);
					Context.img_out_n = req_comp;
					if (result == null)
						return result;
				}

				*x = (int)Context.img_x;
				*y = (int)Context.img_y;
				if (n != null)
					*n = (int)Context.img_n;
			}

			CRuntime.free(p._out_);
			p._out_ = null;
			CRuntime.free(p.expanded);
			p.expanded = null;
			CRuntime.free(p.idata);
			p.idata = null;
			return result;
		}

		public void* stbi__png_load(int* x, int* y, ColorComponents* comp, ColorComponents req_comp, stbi__result_info* ri)
		{
			stbi__png p = new stbi__png();
			p.s = s;
			return stbi__do_png(p, x, y, comp, (int)req_comp, ri);
		}

		public int stbi__png_test()
		{
			int r;
			r = stbi__check_png_header();
			stbi__rewind();
			return r;
		}

		public int stbi__png_info_raw(stbi__png p, int* x, int* y, ColorComponents* comp)
		{
			if (stbi__parse_png_file(p, (int)ScanType.Header, 0) == 0)
			{
				stbi__rewind(p.s);
				return 0;
			}

			if (x != null)
				*x = (int)Context.img_x;
			if (y != null)
				*y = (int)Context.img_y;
			if (comp != null)
				*comp = (int)Context.img_n;
			return 1;
		}

		public int stbi__png_info(int* x, int* y, ColorComponents* comp)
		{
			stbi__png p = new stbi__png();
			p.s = s;
			return stbi__png_info_raw(p, x, y, comp);
		}
	}
}