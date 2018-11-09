using System;
using System.Runtime.InteropServices;

namespace StbImageSharp
{
	public unsafe class JpegDecoder : BaseDecoder
	{
		public const int STBI__ZFAST_BITS = 9;

		public delegate void idct_block_kernel_delegate(byte* output, int out_stride, short* data);
		public delegate void YCbCr_to_RGB_kernel_delegate(byte* output, byte* y, byte* pcb, byte* pcr, int count, int step);
		public delegate byte* Resampler(byte* a, byte* b, byte* c, int d, int e);

		[StructLayout(LayoutKind.Sequential)]
		public struct ImgComp
		{
			public int id;
			public int h, v;
			public int tq;
			public int hd, ha;
			public int dc_pred;

			public int x, y, w2, h2;
			public byte* data;
			public void* raw_data;
			public void* raw_coeff;
			public byte* linebuf;
			public short* coeff; // progressive only
			public int coeff_w, coeff_h; // number of 8x8 coefficient blocks
		}

		public class Huffman
		{
			public byte[] fast = new byte[1 << 9];
			public ushort[] code = new ushort[256];
			public byte[] values = new byte[256];
			public byte[] size = new byte[257];
			public uint[] maxcode = new uint[18];
			public int[] delta = new int[17];
		}

		public class Resample
		{
			public Resampler resample;
			public byte* line0;
			public byte* line1;
			public int hs;
			public int vs;
			public int w_lores;
			public int ystep;
			public int ypos;
		}

		public static readonly uint[] stbi__bmask =
{
			0, 1, 3, 7, 15, 31, 63, 127, 255, 511, 1023, 2047, 4095, 8191, 16383, 32767, 65535
		};

		public static readonly int[] stbi__jbias =
		{
			0, -1, -3, -7, -15, -31, -63, -127, -255, -511, -1023, -2047, -4095, -8191, -16383,
			-32767
		};

		public static readonly byte[] stbi__jpeg_dezigzag =
		{
			0, 1, 8, 16, 9, 2, 3, 10, 17, 24, 32, 25, 18, 11, 4, 5, 12, 19, 26, 33, 40,
			48, 41, 34, 27, 20, 13, 6, 7, 14, 21, 28, 35, 42, 49, 56, 57, 50, 43, 36, 29, 22, 15, 23, 30, 37, 44, 51,
			58, 59, 52,
			45, 38, 31, 39, 46, 53, 60, 61, 54, 47, 55, 62, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63,
			63
		};

		public readonly Huffman[] huff_dc = new Huffman[4];
		public readonly Huffman[] huff_ac = new Huffman[4];
		public readonly ushort[][] dequant;

		public readonly short[][] fast_ac;

		// sizes for components, interleaved MCUs
		public int img_h_max, img_v_max;
		public int img_mcu_x, img_mcu_y;
		public int img_mcu_w, img_mcu_h;

		// definition of jpeg image component
		public ImgComp[] img_comp = new ImgComp[4];

		public uint code_buffer; // jpeg entropy-coded buffer
		public int code_bits; // number of valid bits
		public byte marker; // marker seen while filling entropy buffer
		public int nomore; // flag if we saw a marker so must stop

		public int progressive;
		public int spec_start;
		public int spec_end;
		public int succ_high;
		public int succ_low;
		public int eob_run;
		public int jfif;
		public int app14_color_transform; // Adobe APP14 tag
		public int rgb;

		public int scan_n;
		public int[] order = new int[4];
		public int restart_interval, todo;

		public idct_block_kernel_delegate idct_block_kernel;
		public YCbCr_to_RGB_kernel_delegate YCbCr_to_RGB_kernel;
		public Resampler resampleRow_hv_2_kernel;

		public JpegDecoder()
		{
			for (var i = 0; i < 4; ++i)
			{
				huff_ac[i] = new Huffman();
				huff_dc[i] = new Huffman();
			}

			for (var i = 0; i < img_comp.Length; ++i)
			{
				img_comp[i] = new ImgComp();
			}

			fast_ac = new short[4][];
			for (var i = 0; i < fast_ac.Length; ++i)
			{
				fast_ac[i] = new short[1 << STBI__ZFAST_BITS];
			}

			dequant = new ushort[4][];
			for (var i = 0; i < dequant.Length; ++i)
			{
				dequant[i] = new ushort[64];
			}
		}

		public int BuildHuffman(Huffman h, int* count)
		{
			int i;
			int j;
			int k = 0;
			int code;
			for (i = 0; i < 16; ++i)
			{
				for (j = 0; j < count[i]; ++j)
				{
					h.size[k++] = (byte)(i + 1);
				}
			}

			h.size[k] = 0;
			code = 0;
			k = 0;
			for (j = 1; j <= 16; ++j)
			{
				h.delta[j] = k - code;
				if (h.size[k] == j)
				{
					while (h.size[k] == j)
					{
						h.code[k++] = (ushort)code++;
					}

					if ((code - 1) >= (1 << j))
						throw new Exception("bad code lengths");
				}

				h.maxcode[j] = (uint)(code << (16 - j));
				code <<= 1;
			}

			h.maxcode[j] = 0xffffffff;
			for(i = 0; i < h.fast.Length; ++i)
			{
				h.fast[i] = 255;
			}
			for (i = 0; i < k; ++i)
			{
				int s = h.size[i];
				if (s <= 9)
				{
					int c = h.code[i] << (9 - s);
					int m = 1 << (9 - s);
					for (j = 0; j < m; ++j)
					{
						h.fast[c + j] = (byte)i;
					}
				}
			}

			return 1;
		}

		public void BuildFastAc(short[] fast_ac, Huffman h)
		{
			int i;
			for (i = 0; i < (1 << 9); ++i)
			{
				byte fast = h.fast[i];
				fast_ac[i] = 0;
				if (fast < 255)
				{
					int rs = h.values[fast];
					int run = (rs >> 4) & 15;
					int magbits = rs & 15;
					int len = h.size[fast];
					if ((magbits != 0) && (len + magbits <= 9))
					{
						int k = ((i << len) & ((1 << 9) - 1)) >> (9 - magbits);
						int m = 1 << (magbits - 1);
						if (k < m)
							k += (int)((~0U << magbits) + 1);
						if ((k >= (-128)) && (k <= 127))
							fast_ac[i] = (short)((k << 8) + (run << 4) + len + magbits);
					}
				}
			}
		}

		public void GrowBufferUnsafe()
		{
			do
			{
				int b = nomore != 0 ? 0 : Context.Get8();
				if (b == 0xff)
				{
					int c = Context.Get8();
					while (c == 0xff)
					{
						c = Context.Get8();
					}

					if (c != 0)
					{
						marker = (byte)c;
						nomore = 1;
						return;
					}
				}

				code_buffer |= (uint)(b << (24 - code_bits));
				code_bits += 8;
			} while (code_bits <= 24);
		}

		public int HuffDecode(Huffman h)
		{
			uint temp;
			int c;
			int k;
			if (code_bits < 16)
				GrowBufferUnsafe();
			c = (int)((code_buffer >> (32 - 9)) & ((1 << 9) - 1));
			k = h.fast[c];
			if (k < 255)
			{
				int s = h.size[k];
				if (s > code_bits)
					return -1;
				code_buffer <<= s;
				code_bits -= s;
				return h.values[k];
			}

			temp = code_buffer >> 16;
			for (k = 9 + 1; ; ++k)
			{
				if (temp < h.maxcode[k])
					break;
			}

			if (k == 17)
			{
				code_bits -= 16;
				return -1;
			}

			if (k > code_bits)
				return -1;
			c = (int)(((code_buffer >> (32 - k)) & stbi__bmask[k]) + h.delta[k]);
			code_bits -= k;
			code_buffer <<= k;
			return h.values[c];
		}

		public int ExtendReceive(int n)
		{
			uint k;
			int sgn;
			if (code_bits < n)
				GrowBufferUnsafe();
			sgn = (int)code_buffer >> 31;
			k = CRuntime._lrotl(code_buffer, n);
			code_buffer = k & ~stbi__bmask[n];
			k &= stbi__bmask[n];
			code_bits -= n;
			return (int)(k + (stbi__jbias[n] & ~sgn));
		}

		public int GetBits(int n)
		{
			uint k;
			if (code_bits < n)
				GrowBufferUnsafe();
			k = CRuntime._lrotl(code_buffer, n);
			code_buffer = k & ~stbi__bmask[n];
			k &= stbi__bmask[n];
			code_bits -= n;
			return (int)k;
		}

		public int GetBit()
		{
			uint k;
			if (code_bits < 1)
				GrowBufferUnsafe();
			k = code_buffer;
			code_buffer <<= 1;
			--code_bits;
			return (int)(k & 0x80000000);
		}

		public int DecodeBlock(short* data, Huffman hdc, Huffman hac, short[] fac, int b, ushort[] dequant)
		{
			int diff;
			int dc;
			int k;
			int t;
			if (code_bits < 16)
				GrowBufferUnsafe();
			t = HuffDecode(hdc);
			if (t < 0)
				throw new Exception("bad huffman code");
			CRuntime.memset(data, 0, (ulong)(64 * sizeof(short)));
			diff = t != 0 ? ExtendReceive(t) : 0;
			dc = img_comp[b].dc_pred + diff;
			img_comp[b].dc_pred = dc;
			data[0] = (short)(dc * dequant[0]);
			k = 1;
			do
			{
				uint zig;
				int c;
				int r;
				int s;
				if (code_bits < 16)
					GrowBufferUnsafe();
				c = (int)((code_buffer >> (32 - 9)) & ((1 << 9) - 1));
				r = fac[c];
				if (r != 0)
				{
					k += (r >> 4) & 15;
					s = r & 15;
					code_buffer <<= s;
					code_bits -= s;
					zig = stbi__jpeg_dezigzag[k++];
					data[zig] = (short)((r >> 8) * dequant[zig]);
				}
				else
				{
					int rs = HuffDecode(hac);
					if (rs < 0)
						throw new Exception("bad huffman code");
					s = rs & 15;
					r = rs >> 4;
					if (s == 0)
					{
						if (rs != 0xf0)
							break;
						k += 16;
					}
					else
					{
						k += r;
						zig = stbi__jpeg_dezigzag[k++];
						data[zig] = (short)(ExtendReceive(s) * dequant[zig]);
					}
				}
			} while (k < 64);

			return 1;
		}

		public int DecodeBlockProgDc(short* data, Huffman hdc, int b)
		{
			int diff;
			int dc;
			int t;
			if (spec_end != 0)
				throw new Exception("can't merge dc and ac");
			if (code_bits < 16)
				GrowBufferUnsafe();
			if (succ_high == 0)
			{
				CRuntime.memset(data, 0, (ulong)(64 * sizeof(short)));
				t = HuffDecode(hdc);
				diff = t != 0 ? ExtendReceive(t) : 0;
				dc = img_comp[b].dc_pred + diff;
				img_comp[b].dc_pred = dc;
				data[0] = (short)(dc << succ_low);
			}
			else
			{
				if (GetBit() != 0)
					data[0] += (short)(1 << succ_low);
			}

			return 1;
		}

		public int DecodeBlockProgAc(short* data, Huffman hac, short[] fac)
		{
			int k;
			if (spec_start == 0)
				throw new Exception("can't merge dc and ac");
			if (succ_high == 0)
			{
				int shift = succ_low;
				if (eob_run != 0)
				{
					--eob_run;
					return 1;
				}

				k = spec_start;
				do
				{
					uint zig;
					int c;
					int r;
					int s;
					if (code_bits < 16)
						GrowBufferUnsafe();
					c = (int)((code_buffer >> (32 - 9)) & ((1 << 9) - 1));
					r = fac[c];
					if (r != 0)
					{
						k += (r >> 4) & 15;
						s = r & 15;
						code_buffer <<= s;
						code_bits -= s;
						zig = stbi__jpeg_dezigzag[k++];
						data[zig] = (short)((r >> 8) << shift);
					}
					else
					{
						int rs = HuffDecode(hac);
						if (rs < 0)
							throw new Exception("bad huffman code");
						s = rs & 15;
						r = rs >> 4;
						if (s == 0)
						{
							if (r < 15)
							{
								eob_run = 1 << r;
								if (r != 0)
									eob_run += GetBits(r);
								--eob_run;
								break;
							}

							k += 16;
						}
						else
						{
							k += r;
							zig = stbi__jpeg_dezigzag[k++];
							data[zig] = (short)(ExtendReceive(s) << shift);
						}
					}
				} while (k <= spec_end);
			}
			else
			{
				short bit = (short)(1 << succ_low);
				if (eob_run != 0)
				{
					--eob_run;
					for (k = spec_start; k <= spec_end; ++k)
					{
						short* p = &data[stbi__jpeg_dezigzag[k]];
						if (*p != 0)
							if (GetBit() != 0)
								if ((*p & bit) == 0)
								{
									if ((*p) > 0)
										*p += bit;
									else
										*p -= bit;
								}
					}
				}
				else
				{
					k = spec_start;
					do
					{
						int r;
						int s;
						int rs = HuffDecode(hac);
						if (rs < 0)
							throw new Exception("bad huffman code");
						s = rs & 15;
						r = rs >> 4;
						if (s == 0)
						{
							if (r < 15)
							{
								eob_run = (1 << r) - 1;
								if (r != 0)
									eob_run += GetBits(r);
								r = 64;
							}
							else
							{
							}
						}
						else
						{
							if (s != 1)
								throw new Exception("bad huffman code");
							if (GetBit() != 0)
								s = bit;
							else
								s = -bit;
						}

						while (k <= spec_end)
						{
							short* p = &data[stbi__jpeg_dezigzag[k++]];
							if (*p != 0)
							{
								if (GetBit() != 0)
									if ((*p & bit) == 0)
									{
										if ((*p) > 0)
											*p += bit;
										else
											*p -= bit;
									}
							}
							else
							{
								if (r == 0)
								{
									*p = (short)s;
									break;
								}

								--r;
							}
						}
					} while (k <= spec_end);
				}
			}

			return 1;
		}

		public byte stbi__clamp(int x)
		{
			if (((uint)x) > 255)
			{
				if (x < 0)
					return 0;
				if (x > 255)
					return 255;
			}

			return (byte)x;
		}

		public void stbi__idct_block(byte* _out_, int out_stride, short* data)
		{
			int i;
			int* val = stackalloc int[64];
			int* v = val;
			byte* o;
			short* d = data;
			for (i = 0; i < 8; ++i, ++d, ++v)
			{
				if ((d[8] == 0) && (d[16] == 0) && (d[24] == 0) && (d[32] == 0) &&
					  (d[40] == 0) &&
					 (d[48] == 0) && (d[56] == 0))
				{
					int dcterm = d[0] << 2;
					v[0] =

						v[8] =
							v[16] = v[24] =
								v[32] = v[40] = v[48] = v[56] = dcterm;
				}
				else
				{
					int t0;
					int t1;
					int t2;
					int t3;
					int p1;
					int p2;
					int p3;
					int p4;
					int p5;
					int x0;
					int x1;
					int x2;
					int x3;
					p2 = d[16];
					p3 = d[48];
					p1 = (p2 + p3) * ((int)(0.5411961f * 4096 + 0.5));
					t2 = p1 + p3 * ((int)((-1.847759065f) * 4096 + 0.5));
					t3 = p1 + p2 * ((int)(0.765366865f * 4096 + 0.5));
					p2 = d[0];
					p3 = d[32];
					t0 = (p2 + p3) << 12;
					t1 = (p2 - p3) << 12;
					x0 = t0 + t3;
					x3 = t0 - t3;
					x1 = t1 + t2;
					x2 = t1 - t2;
					t0 = d[56];
					t1 = d[40];
					t2 = d[24];
					t3 = d[8];
					p3 = t0 + t2;
					p4 = t1 + t3;
					p1 = t0 + t3;
					p2 = t1 + t2;
					p5 = (p3 + p4) * ((int)(1.175875602f * 4096 + 0.5));
					t0 = t0 * ((int)(0.298631336f * 4096 + 0.5));
					t1 = t1 * ((int)(2.053119869f * 4096 + 0.5));
					t2 = t2 * ((int)(3.072711026f * 4096 + 0.5));
					t3 = t3 * ((int)(1.501321110f * 4096 + 0.5));
					p1 = p5 + p1 * ((int)((-0.899976223f) * 4096 + 0.5));
					p2 = p5 + p2 * ((int)((-2.562915447f) * 4096 + 0.5));
					p3 = p3 * ((int)((-1.961570560f) * 4096 + 0.5));
					p4 = p4 * ((int)((-0.390180644f) * 4096 + 0.5));
					t3 += p1 + p4;
					t2 += p2 + p3;
					t1 += p2 + p4;
					t0 += p1 + p3;
					x0 += 512;
					x1 += 512;
					x2 += 512;
					x3 += 512;
					v[0] = (x0 + t3) >> 10;
					v[56] = (x0 - t3) >> 10;
					v[8] = (x1 + t2) >> 10;
					v[48] = (x1 - t2) >> 10;
					v[16] = (x2 + t1) >> 10;
					v[40] = (x2 - t1) >> 10;
					v[24] = (x3 + t0) >> 10;
					v[32] = (x3 - t0) >> 10;
				}
			}

			for (i = 0, v = val, o = _out_; i < 8; ++i, v += 8, o += out_stride)
			{
				int t0;
				int t1;
				int t2;
				int t3;
				int p1;
				int p2;
				int p3;
				int p4;
				int p5;
				int x0;
				int x1;
				int x2;
				int x3;
				p2 = v[2];
				p3 = v[6];
				p1 = (p2 + p3) * ((int)(0.5411961f * 4096 + 0.5));
				t2 = p1 + p3 * ((int)((-1.847759065f) * 4096 + 0.5));
				t3 = p1 + p2 * ((int)(0.765366865f * 4096 + 0.5));
				p2 = v[0];
				p3 = v[4];
				t0 = (p2 + p3) << 12;
				t1 = (p2 - p3) << 12;
				x0 = t0 + t3;
				x3 = t0 - t3;
				x1 = t1 + t2;
				x2 = t1 - t2;
				t0 = v[7];
				t1 = v[5];
				t2 = v[3];
				t3 = v[1];
				p3 = t0 + t2;
				p4 = t1 + t3;
				p1 = t0 + t3;
				p2 = t1 + t2;
				p5 = (p3 + p4) * ((int)(1.175875602f * 4096 + 0.5));
				t0 = t0 * ((int)(0.298631336f * 4096 + 0.5));
				t1 = t1 * ((int)(2.053119869f * 4096 + 0.5));
				t2 = t2 * ((int)(3.072711026f * 4096 + 0.5));
				t3 = t3 * ((int)(1.501321110f * 4096 + 0.5));
				p1 = p5 + p1 * ((int)((-0.899976223f) * 4096 + 0.5));
				p2 = p5 + p2 * ((int)((-2.562915447f) * 4096 + 0.5));
				p3 = p3 * ((int)((-1.961570560f) * 4096 + 0.5));
				p4 = p4 * ((int)((-0.390180644f) * 4096 + 0.5));
				t3 += p1 + p4;
				t2 += p2 + p3;
				t1 += p2 + p4;
				t0 += p1 + p3;
				x0 += 65536 + (128 << 17);
				x1 += 65536 + (128 << 17);
				x2 += 65536 + (128 << 17);
				x3 += 65536 + (128 << 17);
				o[0] = stbi__clamp((x0 + t3) >> 17);
				o[7] = stbi__clamp((x0 - t3) >> 17);
				o[1] = stbi__clamp((x1 + t2) >> 17);
				o[6] = stbi__clamp((x1 - t2) >> 17);
				o[2] = stbi__clamp((x2 + t1) >> 17);
				o[5] = stbi__clamp((x2 - t1) >> 17);
				o[3] = stbi__clamp((x3 + t0) >> 17);
				o[4] = stbi__clamp((x3 - t0) >> 17);
			}
		}

		public byte GetMarker()
		{
			byte x;
			if (marker != 0xff)
			{
				x = marker;
				marker = 0xff;
				return x;
			}

			x = (byte)Context.Get8();
			if (x != 0xff)
				return 0xff;
			while (x == 0xff)
			{
				x = (byte)Context.Get8();
			}

			return x;
		}

		public void Reset()
		{
			code_bits = 0;
			code_buffer = 0;
			nomore = 0;
			img_comp[0].dc_pred =
				img_comp[1].dc_pred =
					img_comp[2].dc_pred = img_comp[3].dc_pred = 0;
			marker = 0xff;
			todo = restart_interval != 0 ? restart_interval : 0x7fffffff;
			eob_run = 0;
		}

		public int ParseEnthropyCodedData()
		{
			Reset();
			if (progressive == 0)
			{
				if (scan_n == 1)
				{
					int i;
					int j;
					short* data = stackalloc short[64];
					int n = order[0];
					int w = (img_comp[n].x + 7) >> 3;
					int h = (img_comp[n].y + 7) >> 3;
					for (j = 0; j < h; ++j)
					{
						for (i = 0; i < w; ++i)
						{
							int ha = img_comp[n].ha;
							if (
								DecodeBlock(data, huff_dc[img_comp[n].hd],
									huff_ac[ha],
									fast_ac[ha], n, dequant[img_comp[n].tq]) ==
								0)
								return 0;
							idct_block_kernel(img_comp[n].data + img_comp[n].w2 * j * 8 + i * 8,
								img_comp[n].w2, data);
							if (--todo <= 0)
							{
								if (code_bits < 24)
									GrowBufferUnsafe();
								if (!((marker >= 0xd0) && (marker <= 0xd7)))
									return 1;
								Reset();
							}
						}
					}

					return 1;
				}
				else
				{
					int i;
					int j;
					int k;
					int x;
					int y;
					short* data = stackalloc short[64];
					for (j = 0; j < img_mcu_y; ++j)
					{
						for (i = 0; i < img_mcu_x; ++i)
						{
							for (k = 0; k < scan_n; ++k)
							{
								int n = order[k];
								for (y = 0; y < img_comp[n].v; ++y)
								{
									for (x = 0; x < img_comp[n].h; ++x)
									{
										int x2 = (i * img_comp[n].h + x) * 8;
										int y2 = (j * img_comp[n].v + y) * 8;
										int ha = img_comp[n].ha;
										if (
											DecodeBlock(data,
												huff_dc[img_comp[n].hd],
												huff_ac[ha], fast_ac[ha], n,
												dequant[img_comp[n].tq]) == 0)
											return 0;
										idct_block_kernel(img_comp[n].data + img_comp[n].w2 * y2 + x2,
											img_comp[n].w2, data);
									}
								}
							}

							if (--todo <= 0)
							{
								if (code_bits < 24)
									GrowBufferUnsafe();
								if (!((marker >= 0xd0) && (marker <= 0xd7)))
									return 1;
								Reset();
							}
						}
					}

					return 1;
				}
			}
			else
			{
				if (scan_n == 1)
				{
					int i;
					int j;
					int n = order[0];
					int w = (img_comp[n].x + 7) >> 3;
					int h = (img_comp[n].y + 7) >> 3;
					for (j = 0; j < h; ++j)
					{
						for (i = 0; i < w; ++i)
						{
							short* data = img_comp[n].coeff + 64 * (i + j * img_comp[n].coeff_w);
							if (spec_start == 0)
							{
								if (DecodeBlockProgDc(data, huff_dc[img_comp[n].hd], n) == 0)
									return 0;
							}
							else
							{
								int ha = img_comp[n].ha;
								if (DecodeBlockProgAc(data, huff_ac[ha], fast_ac[ha]) == 0)
									return 0;
							}

							if (--todo <= 0)
							{
								if (code_bits < 24)
									GrowBufferUnsafe();
								if (!((marker >= 0xd0) && (marker <= 0xd7)))
									return 1;
								Reset();
							}
						}
					}

					return 1;
				}
				else
				{
					int i;
					int j;
					int k;
					int x;
					int y;
					for (j = 0; j < img_mcu_y; ++j)
					{
						for (i = 0; i < img_mcu_x; ++i)
						{
							for (k = 0; k < scan_n; ++k)
							{
								int n = order[k];
								for (y = 0; y < img_comp[n].v; ++y)
								{
									for (x = 0; x < img_comp[n].h; ++x)
									{
										int x2 = i * img_comp[n].h + x;
										int y2 = j * img_comp[n].v + y;
										short* data = img_comp[n].coeff + 64 * (x2 + y2 * img_comp[n].coeff_w);
										if (DecodeBlockProgDc(data, huff_dc[img_comp[n].hd], n) == 0)
											return 0;
									}
								}
							}

							if (--todo <= 0)
							{
								if (code_bits < 24)
									GrowBufferUnsafe();
								if (!((marker >= 0xd0) && (marker <= 0xd7)))
									return 1;
								Reset();
							}
						}
					}

					return 1;
				}
			}
		}

		public void Dequantize(short* data, ushort[] dequant)
		{
			int i;
			for (i = 0; i < 64; ++i)
			{
				data[i] *= (short)dequant[i];
			}
		}

		public void Finish()
		{
			if (progressive != 0)
			{
				int i;
				int j;
				int n;
				for (n = 0; n < Context.img_n; ++n)
				{
					int w = (img_comp[n].x + 7) >> 3;
					int h = (img_comp[n].y + 7) >> 3;
					for (j = 0; j < h; ++j)
					{
						for (i = 0; i < w; ++i)
						{
							short* data = img_comp[n].coeff + 64 * (i + j * img_comp[n].coeff_w);
							Dequantize(data, dequant[img_comp[n].tq]);
							idct_block_kernel(img_comp[n].data + img_comp[n].w2 * j * 8 + i * 8,
								img_comp[n].w2, data);
						}
					}
				}
			}
		}

		public int ProcessMarker(int m)
		{
			int L;
			switch (m)
			{
				case 0xff:
					throw new Exception("expected marker");
				case 0xDD:
					if (Context.Get16BigEndian() != 4)
						throw new Exception("bad DRI len");
					restart_interval = Context.Get16BigEndian();
					return 1;
				case 0xDB:
					L = Context.Get16BigEndian() - 2;
					while (L > 0)
					{
						int q = Context.Get8();
						int p = q >> 4;
						int sixteen = (p != 0) ? 1 : 0;
						int t = q & 15;
						int i;
						if ((p != 0) && (p != 1))
							throw new Exception("bad DQT type");
						if (t > 3)
							throw new Exception("bad DQT table");
						for (i = 0; i < 64; ++i)
						{
							dequant[t][stbi__jpeg_dezigzag[i]] =
								(ushort)(sixteen != 0 ? Context.Get16BigEndian() : Context.Get8());
						}

						L -= sixteen != 0 ? 129 : 65;
					}

					return L == 0 ? 1 : 0;
				case 0xC4:
					L = Context.Get16BigEndian() - 2;
					while (L > 0)
					{
						byte[] v;
						int* sizes = stackalloc int[16];
						int i;
						int n = 0;
						int q = Context.Get8();
						int tc = q >> 4;
						int th = q & 15;
						if ((tc > 1) || (th > 3))
							throw new Exception("bad DHT header");
						for (i = 0; i < 16; ++i)
						{
							sizes[i] = Context.Get8();
							n += sizes[i];
						}

						L -= 17;
						if (tc == 0)
						{
							if (BuildHuffman(huff_dc[th], sizes) == 0)
								return 0;
							Huffman h = huff_dc[th];
							v = h.values;
						}
						else
						{
							if (BuildHuffman(huff_ac[th], sizes) == 0)
								return 0;
							Huffman h = huff_ac[th];
							v = h.values;
						}

						for (i = 0; i < n; ++i)
						{
							v[i] = (byte)Context.Get8();
						}

						if (tc != 0)
							BuildFastAc(fast_ac[th], huff_ac[th]);
						L -= n;
					}

					return L == 0 ? 1 : 0;
			}

			if (((m >= 0xE0) && (m <= 0xEF)) || (m == 0xFE))
			{
				L = Context.Get16BigEndian();
				if (L < 2)
				{
					if (m == 0xFE)
						throw new Exception("bad COM len");
					else
						throw new Exception("bad APP len");
				}

				L -= 2;
				if ((m == 0xE0) && (L >= 5))
				{
					byte* tag = stackalloc byte[5];
					tag[0] = (byte)'J';
					tag[1] = (byte)'F';
					tag[2] = (byte)'I';
					tag[3] = (byte)'F';
					tag[4] = (byte)'\0';
					int ok = 1;
					int i;
					for (i = 0; i < 5; ++i)
					{
						if (Context.Get8() != tag[i])
							ok = 0;
					}

					L -= 5;
					if (ok != 0)
						jfif = 1;
				}
				else if ((m == 0xEE) && (L >= 12))
				{
					byte* tag = stackalloc byte[6];
					tag[0] = (byte)'A';
					tag[1] = (byte)'d';
					tag[2] = (byte)'o';
					tag[3] = (byte)'b';
					tag[4] = (byte)'e';
					tag[5] = (byte)'\0';
					int ok = 1;
					int i;
					for (i = 0; i < 6; ++i)
					{
						if (Context.Get8() != tag[i])
							ok = 0;
					}

					L -= 6;
					if (ok != 0)
					{
						Context.Get8();
						Context.Get16BigEndian();
						Context.Get16BigEndian();
						app14_color_transform = Context.Get8();
						L -= 6;
					}
				}

				Context.stream.Seek(L, System.IO.SeekOrigin.Current);
				return 1;
			}

			throw new Exception("unknown marker");
		}

		public int ProcessScanHeader()
		{
			int i;
			int Ls = Context.Get16BigEndian();
			scan_n = Context.Get8();
			if ((scan_n < 1) || (scan_n > 4) || (scan_n > Context.img_n))
				throw new Exception("bad SOS component count");
			if (Ls != 6 + 2 * scan_n)
				throw new Exception("bad SOS len");
			for (i = 0; i < scan_n; ++i)
			{
				int id = Context.Get8();
				int which;
				int q = Context.Get8();
				for (which = 0; which < Context.img_n; ++which)
				{
					if (img_comp[which].id == id)
						break;
				}

				if (which == Context.img_n)
					return 0;
				img_comp[which].hd = q >> 4;
				if (img_comp[which].hd > 3)
					throw new Exception("bad DC huff");
				img_comp[which].ha = q & 15;
				if (img_comp[which].ha > 3)
					throw new Exception("bad AC huff");
				order[i] = which;
			}

			{
				int aa;
				spec_start = Context.Get8();
				spec_end = Context.Get8();
				aa = Context.Get8();
				succ_high = aa >> 4;
				succ_low = aa & 15;
				if (progressive != 0)
				{
					if ((spec_start > 63) || (spec_end > 63) || (spec_start > spec_end) ||
						 (succ_high > 13) || (succ_low > 13))
						throw new Exception("bad SOS");
				}
				else
				{
					if (spec_start != 0)
						throw new Exception("bad SOS");
					if ((succ_high != 0) || (succ_low != 0))
						throw new Exception("bad SOS");
					spec_end = 63;
				}
			}

			return 1;
		}

		public void FreeComponents(int ncomp)
		{
			int i;
			for (i = 0; (i) < (ncomp); ++i)
			{
				if ((img_comp[i].raw_data) != null)
				{
					CRuntime.free(img_comp[i].raw_data);
					img_comp[i].raw_data = (null);
					img_comp[i].data = (null);
				}

				if ((img_comp[i].raw_coeff) != null)
				{
					CRuntime.free(img_comp[i].raw_coeff);
					img_comp[i].raw_coeff = null;
					img_comp[i].coeff = null;
				}

				if ((img_comp[i].linebuf) != null)
				{
					CRuntime.free(img_comp[i].linebuf);
					img_comp[i].linebuf = (null);
				}
			}
		}

		public int ProcessFrameHeader(ScanType scan)
		{
			int Lf;
			int p;
			int i;
			int q;
			int h_max = 1;
			int v_max = 1;
			int c;
			Lf = Context.Get16BigEndian();
			if (Lf < 11)
				throw new Exception("bad SOF len");
			p = Context.Get8();
			if (p != 8)
				throw new Exception("only 8-bit");
			Context.img_y = Context.Get16BigEndian();
			if (Context.img_y == 0)
				throw new Exception("no header height");
			Context.img_x = Context.Get16BigEndian();
			if (Context.img_x == 0)
				throw new Exception("0 width");
			c = Context.Get8();
			if ((c != 3) && (c != 1) && (c != 4))
				throw new Exception("bad component count");
			Context.img_n = c;
			for (i = 0; i < c; ++i)
			{
				img_comp[i].data = null;
				img_comp[i].linebuf = null;
			}

			if (Lf != 8 + 3 * Context.img_n)
				throw new Exception("bad SOF len");
			rgb = 0;
			for (i = 0; i < Context.img_n; ++i)
			{
				byte* rgb = stackalloc byte[3];
				rgb[0] = (byte)'R';
				rgb[1] = (byte)'G';
				rgb[2] = (byte)'B';
				img_comp[i].id = Context.Get8();
				if ((Context.img_n == 3) && (img_comp[i].id == rgb[i]))
					++rgb;
				q = Context.Get8();
				img_comp[i].h = q >> 4;
				if ((img_comp[i].h == 0) || (img_comp[i].h > 4))
					throw new Exception("bad H");
				img_comp[i].v = q & 15;
				if ((img_comp[i].v == 0) || (img_comp[i].v > 4))
					throw new Exception("bad V");
				img_comp[i].tq = Context.Get8();
				if (img_comp[i].tq > 3)
					throw new Exception("bad TQ");
			}

			if (scan != ScanType.Load)
				return 1;
			if (Utility.Mad3SizesValid(Context.img_x, Context.img_y, Context.img_n, 0) == 0)
				throw new Exception("too large");
			for (i = 0; i < Context.img_n; ++i)
			{
				if (img_comp[i].h > h_max)
					h_max = img_comp[i].h;
				if (img_comp[i].v > v_max)
					v_max = img_comp[i].v;
			}

			img_h_max = h_max;
			img_v_max = v_max;
			img_mcu_w = h_max * 8;
			img_mcu_h = v_max * 8;
			img_mcu_x = (Context.img_x + img_mcu_w - 1) / img_mcu_w;
			img_mcu_y = (Context.img_y + img_mcu_h - 1) / img_mcu_h;
			for (i = 0; i < Context.img_n; ++i)
			{
				img_comp[i].x = (Context.img_x * img_comp[i].h + h_max - 1) / h_max;
				img_comp[i].y = (Context.img_y * img_comp[i].v + v_max - 1) / v_max;
				img_comp[i].w2 = img_mcu_x * img_comp[i].h * 8;
				img_comp[i].h2 = img_mcu_y * img_comp[i].v * 8;
				img_comp[i].coeff = null;
				img_comp[i].raw_coeff = null;
				img_comp[i].linebuf = null;
				img_comp[i].raw_data = Utility.MallocMad2(img_comp[i].w2, img_comp[i].h2, 15);
				img_comp[i].data = (byte*)(((long)img_comp[i].raw_data + 15) & ~15);
				if (progressive != 0)
				{
					img_comp[i].coeff_w = img_comp[i].w2 / 8;
					img_comp[i].coeff_h = img_comp[i].h2 / 8;
					img_comp[i].raw_coeff = Utility.MallocMad3(img_comp[i].w2, img_comp[i].h2,
						2,
						15);
					img_comp[i].coeff = (short*)(((long)img_comp[i].raw_coeff + 15) & ~15);
				}
			}

			return 1;
		}

		public bool DecodeHeader(ScanType scan)
		{
			int m;
			jfif = 0;
			app14_color_transform = -1;
			marker = 0xff;
			m = GetMarker();
			if (!(m == 0xd8))
				throw new Exception("no SOI");
			if (scan == ScanType.Type)
				return true;
			m = GetMarker();
			while (!((m == 0xc0) || (m == 0xc1) || (m == 0xc2)))
			{
				if (ProcessMarker(m) == 0)
					return false;
				m = GetMarker();
				while (m == 0xff)
				{
					if (Context.stream.Length == Context.stream.Position)
						throw new Exception("no SOF");
					m = GetMarker();
				}
			}

			progressive = m == 0xc2 ? 1 : 0;
			if (ProcessFrameHeader(scan) == 0)
				return false;
			return true;
		}

		public bool Decode()
		{
			int m;
			for (m = 0; m < 4; m++)
			{
				img_comp[m].raw_data = null;
				img_comp[m].raw_coeff = null;
			}

			restart_interval = 0;
			if (!DecodeHeader(ScanType.Load))
				return false;
			m = GetMarker();
			while (!(m == 0xd9))
			{
				if (m == 0xda)
				{
					if (ProcessScanHeader() == 0)
						return false;
					if (ParseEnthropyCodedData() == 0)
						return false;
					if (marker == 0xff)
					{
						while (Context.stream.Position < Context.stream.Length)
						{
							int x = Context.Get8();
							if (x == 255)
							{
								marker = (byte)Context.Get8();
								break;
							}
						}
					}
				}
				else if (m == 0xdc)
				{
					int Ld = Context.Get16BigEndian();
					uint NL = (uint)Context.Get16BigEndian();
					if (Ld != 4)
						throw new Exception("bad DNL len");
					if (NL != Context.img_y)
						throw new Exception("bad DNL height");
				}
				else
				{
					if (ProcessMarker(m) == 0)
						return false;
				}

				m = GetMarker();
			}

			if (progressive != 0)
				Finish();
			return true;
		}

		public byte* ResampleRow1(byte* _out_, byte* in_near, byte* in_far, int w, int hs)
		{
			return in_near;
		}

		public byte* ResampleRowV2(byte* _out_, byte* in_near, byte* in_far, int w, int hs)
		{
			int i;
			for (i = 0; i < w; ++i)
			{
				_out_[i] = (byte)((3 * in_near[i] + in_far[i] + 2) >> 2);
			}

			return _out_;
		}

		public byte* ResampleRowH2(byte* _out_, byte* in_near, byte* in_far, int w, int hs)
		{
			int i;
			byte* input = in_near;
			if (w == 1)
			{
				_out_[0] = _out_[1] = input[0];
				return _out_;
			}

			_out_[0] = input[0];
			_out_[1] = (byte)((input[0] * 3 + input[1] + 2) >> 2);
			for (i = 1; i < (w - 1); ++i)
			{
				int n = 3 * input[i] + 2;
				_out_[i * 2 + 0] = (byte)((n + input[i - 1]) >> 2);
				_out_[i * 2 + 1] = (byte)((n + input[i + 1]) >> 2);
			}

			_out_[i * 2 + 0] = (byte)((input[w - 2] * 3 + input[w - 1] + 2) >> 2);
			_out_[i * 2 + 1] = input[w - 1];
			return _out_;
		}

		public byte* ResampleRowHV2(byte* _out_, byte* in_near, byte* in_far, int w, int hs)
		{
			int i;
			int t0;
			int t1;
			if (w == 1)
			{
				_out_[0] = _out_[1] = (byte)((3 * in_near[0] + in_far[0] + 2) >> 2);
				return _out_;
			}

			t1 = 3 * in_near[0] + in_far[0];
			_out_[0] = (byte)((t1 + 2) >> 2);
			for (i = 1; i < w; ++i)
			{
				t0 = t1;
				t1 = 3 * in_near[i] + in_far[i];
				_out_[i * 2 - 1] = (byte)((3 * t0 + t1 + 8) >> 4);
				_out_[i * 2] = (byte)((3 * t1 + t0 + 8) >> 4);
			}

			_out_[w * 2 - 1] = (byte)((t1 + 2) >> 2);
			return _out_;
		}

		public byte* ResampleRowGeneric(byte* _out_, byte* in_near, byte* in_far, int w, int hs)
		{
			int i;
			int j;
			for (i = 0; i < w; ++i)
			{
				for (j = 0; j < hs; ++j)
				{
					_out_[i * hs + j] = in_near[i];
				}
			}

			return _out_;
		}

		public void YCbCrToRGBRow(byte* _out_, byte* y, byte* pcb, byte* pcr, int count, int step)
		{
			int i;
			for (i = 0; i < count; ++i)
			{
				int y_fixed = (y[i] << 20) + (1 << 19);
				int r;
				int g;
				int b;
				int cr = pcr[i] - 128;
				int cb = pcb[i] - 128;
				r = y_fixed + cr * (((int)(1.40200f * 4096.0f + 0.5f)) << 8);
				g =
					(int)
					(y_fixed + (cr * -(((int)(0.71414f * 4096.0f + 0.5f)) << 8)) +
					 ((cb * -(((int)(0.34414f * 4096.0f + 0.5f)) << 8)) & 0xffff0000));
				b = y_fixed + cb * (((int)(1.77200f * 4096.0f + 0.5f)) << 8);
				r >>= 20;
				g >>= 20;
				b >>= 20;
				if (((uint)r) > 255)
				{
					if (r < 0)
						r = 0;
					else
						r = 255;
				}

				if (((uint)g) > 255)
				{
					if (g < 0)
						g = 0;
					else
						g = 255;
				}

				if (((uint)b) > 255)
				{
					if (b < 0)
						b = 0;
					else
						b = 255;
				}

				_out_[0] = (byte)r;
				_out_[1] = (byte)g;
				_out_[2] = (byte)b;
				_out_[3] = 255;
				_out_ += step;
			}
		}

		public void Setup()
		{
			idct_block_kernel = stbi__idct_block;
			YCbCr_to_RGB_kernel = YCbCrToRGBRow;
			resampleRow_hv_2_kernel = ResampleRowHV2;
		}

		public void Cleanup()
		{
			FreeComponents(Context.img_n);
		}

		public byte Blinn8x8(byte x, byte y)
		{
			uint t = (uint)(x * y + 128);
			return (byte)((t + (t >> 8)) >> 8);
		}

		public byte* InternalLoad(int* out_x, int* out_y, ColorComponents* comp, ColorComponents req_comp)
		{
			int n;
			int decode_n;
			int is_rgb;
			Context.img_n = 0;
			if (!Decode())
			{
				Cleanup();
				return null;
			}

			n = req_comp != ColorComponents.Default ? (int)req_comp : Context.img_n >= 3 ? 3 : 1;
			is_rgb =
				(Context.img_n == 3) &&
					   ((rgb == 3) || ((app14_color_transform == 0) && (jfif == 0)))
					? 1
					: 0;
			if ((Context.img_n == 3) && (n < 3) && (is_rgb == 0))
				decode_n = 1;
			else
				decode_n = Context.img_n;
			{
				int k;
				uint i;
				uint j;
				byte* output;
				byte** coutput = stackalloc byte*[4];
				var res_comp = new Resample[4];
				for (var kkk = 0; kkk < res_comp.Length; ++kkk)
					res_comp[kkk] = new Resample();
				for (k = 0; k < decode_n; ++k)
				{
					Resample r = res_comp[k];
					img_comp[k].linebuf = (byte*)CRuntime.malloc((ulong)(Context.img_x + 3));
					if (img_comp[k].linebuf == null)
					{
						Cleanup();
						throw new Exception("outofmem");
					}

					r.hs = img_h_max / img_comp[k].h;
					r.vs = img_v_max / img_comp[k].v;
					r.ystep = r.vs >> 1;
					r.w_lores = (Context.img_x + r.hs - 1) / r.hs;
					r.ypos = 0;
					r.line0 = r.line1 = img_comp[k].data;
					if ((r.hs == 1) && (r.vs == 1))
						r.resample = ResampleRow1;
					else if ((r.hs == 1) && (r.vs == 2))
						r.resample = ResampleRowV2;
					else if ((r.hs == 2) && (r.vs == 1))
						r.resample = ResampleRowH2;
					else if ((r.hs == 2) && (r.vs == 2))
						r.resample = resampleRow_hv_2_kernel;
					else
						r.resample = ResampleRowGeneric;
				}

				output = (byte*)Utility.MallocMad3(n, Context.img_x, Context.img_y, 1);
				if (output == null)
				{
					Cleanup();
					throw new Exception("outofmem");
				}

				for (j = 0; j < Context.img_y; ++j)
				{
					byte* _out_ = output + n * Context.img_x * j;
					for (k = 0; k < decode_n; ++k)
					{
						Resample r = res_comp[k];
						int y_bot = r.ystep >= (r.vs >> 1) ? 1 : 0;
						coutput[k] = r.resample(img_comp[k].linebuf, y_bot != 0 ? r.line1 : r.line0,
							y_bot != 0 ? r.line0 : r.line1,
							r.w_lores, r.hs);
						if ((++r.ystep) >= r.vs)
						{
							r.ystep = 0;
							r.line0 = r.line1;
							if ((++r.ypos) < img_comp[k].y)
								r.line1 += img_comp[k].w2;
						}
					}

					if (n >= 3)
					{
						byte* y = coutput[0];
						if (Context.img_n == 3)
						{
							if (is_rgb != 0)
							{
								for (i = 0; i < Context.img_x; ++i)
								{
									_out_[0] = y[i];
									_out_[1] = coutput[1][i];
									_out_[2] = coutput[2][i];
									_out_[3] = 255;
									_out_ += n;
								}
							}
							else
							{
								YCbCr_to_RGB_kernel(_out_, y, coutput[1], coutput[2], Context.img_x, n);
							}
						}
						else if (Context.img_n == 4)
						{
							if (app14_color_transform == 0)
							{
								for (i = 0; i < Context.img_x; ++i)
								{
									byte m = coutput[3][i];
									_out_[0] = Blinn8x8(coutput[0][i], m);
									_out_[1] = Blinn8x8(coutput[1][i], m);
									_out_[2] = Blinn8x8(coutput[2][i], m);
									_out_[3] = 255;
									_out_ += n;
								}
							}
							else if (app14_color_transform == 2)
							{
								YCbCr_to_RGB_kernel(_out_, y, coutput[1], coutput[2], Context.img_x, n);
								for (i = 0; i < Context.img_x; ++i)
								{
									byte m = coutput[3][i];
									_out_[0] = Blinn8x8((byte)(255 - _out_[0]), m);
									_out_[1] = Blinn8x8((byte)(255 - _out_[1]), m);
									_out_[2] = Blinn8x8((byte)(255 - _out_[2]), m);
									_out_ += n;
								}
							}
							else
							{
								YCbCr_to_RGB_kernel(_out_, y, coutput[1], coutput[2], Context.img_x, n);
							}
						}
						else
							for (i = 0; i < Context.img_x; ++i)
							{
								_out_[0] = _out_[1] = _out_[2] = y[i];
								_out_[3] = 255;
								_out_ += n;
							}
					}
					else
					{
						if (is_rgb != 0)
						{
							if (n == 1)
								for (i = 0; i < Context.img_x; ++i)
								{
									*_out_++ = Utility.ComputeY(coutput[0][i], coutput[1][i],
										coutput[2][i]);
								}
							else
							{
								for (i = 0; i < Context.img_x; ++i, _out_ += 2)
								{
									_out_[0] = Utility.ComputeY(coutput[0][i], coutput[1][i],
										coutput[2][i]);
									_out_[1] = 255;
								}
							}
						}
						else if ((Context.img_n == 4) && (app14_color_transform == 0))
						{
							for (i = 0; i < Context.img_x; ++i)
							{
								byte m = coutput[3][i];
								byte r = Blinn8x8(coutput[0][i], m);
								byte g = Blinn8x8(coutput[1][i], m);
								byte b = Blinn8x8(coutput[2][i], m);
								_out_[0] = Utility.ComputeY(r, g, b);
								_out_[1] = 255;
								_out_ += n;
							}
						}
						else if ((Context.img_n == 4) && (app14_color_transform == 2))
						{
							for (i = 0; i < Context.img_x; ++i)
							{
								_out_[0] =
									Blinn8x8((byte)(255 - coutput[0][i]), coutput[3][i]);
								_out_[1] = 255;
								_out_ += n;
							}
						}
						else
						{
							byte* y = coutput[0];
							if (n == 1)
								for (i = 0; i < Context.img_x; ++i)
								{
									_out_[i] = y[i];
								}
							else
								for (i = 0; i < Context.img_x; ++i)
								{
									*_out_++ = y[i];
									*_out_++ = 255;
								}
						}
					}
				}

				Cleanup();
				*out_x = Context.img_x;
				*out_y = Context.img_y;
				if (comp != null)
				{
					*comp = (ColorComponents)(Context.img_n >= 3 ? 3 : 1);
				}
				return output;
			}
		}

		protected override byte* InternalLoad(ColorComponents comp, int* width, int* height, ColorComponents* sourceComp)
		{
			Setup();

			return InternalLoad(width, height, sourceComp, comp);
		}

		protected override bool InternalTest()
		{
			Setup();
			var r = DecodeHeader(ScanType.Type);

			return r;
		}

		protected override bool InternalInfo(int* width, int* height, ColorComponents* sourceComp)
		{
			if (!DecodeHeader(ScanType.Header))
			{
				Context.Rewind();
				return false;
			}

			if (width != null)
				*width = Context.img_x;
			if (height != null)
				*height = Context.img_y;
			if (sourceComp != null)
				*sourceComp = (ColorComponents)(Context.img_n >= 3 ? 3 : 1);
			return true;
		}
	}
}