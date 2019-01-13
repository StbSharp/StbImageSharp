using System;
using System.Runtime.InteropServices;

namespace StbImageSharp
{
	public unsafe class PngDecoder : BaseDecoder
	{
		public enum F
		{
			none = 0,
			sub = 1,
			up = 2,
			avg = 3,
			paeth = 4,
			avg_first = 5,
			paeth_first = 6
		}

		public static readonly byte[] PngSignature = { 137, 80, 78, 71, 13, 10, 26, 10 };

		public static readonly F[] FirstRowFilter =
		{
			F.none,
			F.sub,
			F.none,
			F.avg_first,
			F.paeth_first
		};

		public static readonly byte[] DepthScaleTable = { 0, 0xff, 0x55, 0, 0x11, 0, 0, 0, 0x01 };

		public int stbi__unpremultiply_on_load = 0;
		public int stbi__de_iphone_flag = 0;

		[StructLayout(LayoutKind.Sequential)]
		public struct PngChunk
		{
			public uint length;
			public uint type;
		}

		public byte* _idata;
		public byte* _expanded;
		public byte* _out_;
		public int _depth;

		public PngChunk GetChunkHeader()
		{
			PngChunk c = new PngChunk();
			c.length = Context.Get32BigEndian();
			c.type = Context.Get32BigEndian();
			return c;
		}

		public void CheckPngHeader()
		{
			for (var i = 0; i < 8; ++i)
			{
				if (Context.Get8() != PngSignature[i])
					throw new Exception("bad png sig");
			}
		}

		public int Paeth(int a, int b, int c)
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

		public int CreatePngImageRaw(byte* raw, uint raw_len, int out_n, uint x, uint y, int depth, int color)
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
					filter = (int)FirstRowFilter[filter];
				for (k = 0; k < filter_bytes; ++k)
				{
					switch ((F)filter)
					{
						case F.none:
							cur[k] = raw[k];
							break;
						case F.sub:
							cur[k] = raw[k];
							break;
						case F.up:
							cur[k] = (byte)((raw[k] + prior[k]) & 255);
							break;
						case F.avg:
							cur[k] = (byte)((raw[k] + (prior[k] >> 1)) & 255);
							break;
						case F.paeth:
							cur[k] = (byte)((raw[k] + Paeth(0, prior[k], 0)) & 255);
							break;
						case F.avg_first:
							cur[k] = raw[k];
							break;
						case F.paeth_first:
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
					switch ((F)filter)
					{
						case F.none:
							CRuntime.memcpy(cur, raw, (ulong)nk);
							break;
						case F.sub:
							for (k = 0; k < nk; ++k)
							{
								cur[k] = (byte)((raw[k] + cur[k - filter_bytes]) & 255);
							}

							break;
						case F.up:
							for (k = 0; k < nk; ++k)
							{
								cur[k] = (byte)((raw[k] + prior[k]) & 255);
							}

							break;
						case F.avg:
							for (k = 0; k < nk; ++k)
							{
								cur[k] = (byte)((raw[k] + ((prior[k] + cur[k - filter_bytes]) >> 1)) & 255);
							}

							break;
						case F.paeth:
							for (k = 0; k < nk; ++k)
							{
								cur[k] =
									(byte)
										((raw[k] + Paeth(cur[k - filter_bytes], prior[k],
											  prior[k - filter_bytes])) &
										 255);
							}

							break;
						case F.avg_first:
							for (k = 0; k < nk; ++k)
							{
								cur[k] = (byte)((raw[k] + (cur[k - filter_bytes] >> 1)) & 255);
							}

							break;
						case F.paeth_first:
							for (k = 0; k < nk; ++k)
							{
								cur[k] = (byte)((raw[k] + Paeth(cur[k - filter_bytes], 0,
													   0)) & 255);
							}

							break;
					}

					raw += nk;
				}
				else
				{
					switch ((F)filter)
					{
						case F.none:
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
						case F.sub:
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
						case F.up:
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
						case F.avg:
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
						case F.paeth:
							for (i = x - 1;
								i >= 1;
								--i, cur[filter_bytes] = 255, raw += filter_bytes, cur += output_bytes,
								prior += output_bytes)
							{
								for (k = 0; k < filter_bytes; ++k)
								{
									cur[k] =
										(byte)
											((raw[k] + Paeth(cur[k - output_bytes], prior[k],
												  prior[k - output_bytes])) &
											 255);
								}
							}

							break;
						case F.avg_first:
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
						case F.paeth_first:
							for (i = x - 1;
								i >= 1;
								--i, cur[filter_bytes] = 255, raw += filter_bytes, cur += output_bytes,
								prior += output_bytes)
							{
								for (k = 0; k < filter_bytes; ++k)
								{
									cur[k] = (byte)((raw[k] + Paeth(cur[k - output_bytes], 0,
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
					byte scale = (byte)((color == 0) ? DepthScaleTable[depth] : 1);
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

		public int CreatePngImage(byte* image_data, uint image_data_len, int out_n,
			int depth, int color, int interlaced)
		{
			int bytes = depth == 16 ? 2 : 1;
			int out_bytes = out_n * bytes;
			byte* final;
			int p;
			if (interlaced == 0)
			{
				return CreatePngImageRaw(image_data, image_data_len, out_n,
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
					uint img_len = (uint)((((((int)Context.img_n * x * depth) + 7) >> 3) + 1) * y);
					if (
						CreatePngImageRaw(image_data, image_data_len, out_n, (uint)x,
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

		public int ComputeTransparency(byte* tc, int out_n)
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

		public int ComputeTransparency16(ushort* tc, int out_n)
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

		public int ExpandPngPalette(byte* palette, int len, int pal_img_n)
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

		public void DeIphone()
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

		public int ParsePngFile(ScanType scan, ColorComponents req_comp)
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
			_expanded = null;
			_idata = null;
			_out_ = null;
			CheckPngHeader();

			if (scan == ScanType.Type)
				return 1;
			for (; ; )
			{
				PngChunk c = GetChunkHeader();
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
						_depth = Context.Get8();
						if ((_depth != 1) && (_depth != 2) && (_depth != 4) && (_depth != 8) &&
							(_depth != 16))
							throw new Exception("1/2/4/8/16-bit only");
						color = Context.Get8();
						if (color > 6)
							throw new Exception("bad ctype");
						if ((color == 3) && (_depth == 16))
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
						if (_idata != null)
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
							if (_depth == 16)
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
									tc[k] = (byte)((byte)(Context.Get16BigEndian() & 255) * DepthScaleTable[_depth]);
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

							p = (byte*)CRuntime.realloc(_idata, (ulong)idata_limit);
							if (p == null)
								throw new Exception("outofmem");
							_idata = p;
						}

						if (!Context.Getn(_idata + ioff, (int)c.length))
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
						if (_idata == null)
							throw new Exception("no IDAT");
						bpl = (uint)((Context.img_x * _depth + 7) / 8);
						raw_len = (uint)(bpl * Context.img_y * Context.img_n + Context.img_y);
						_expanded =
							(byte*)ZBuffer.ZlibDecodeMallocGuessSizeHeaderFlag((sbyte*)_idata, (int)ioff,
								(int)raw_len,
								(int*)&raw_len, is_iphone != 0 ? 0 : 1);
						if (_expanded == null)
							return 0;
						CRuntime.free(_idata);
						_idata = null;
						if ((((int)req_comp == (Context.img_n + 1)) && (req_comp != ColorComponents.RedGreenBlue) && (pal_img_n == 0)) ||
							(has_trans != 0))
							Context.img_out_n = Context.img_n + 1;
						else
							Context.img_out_n = Context.img_n;
						if (
							CreatePngImage(_expanded, (uint)raw_len, (int)Context.img_out_n,
								(int)_depth, (int)color,
								(int)interlace) == 0)
							return 0;
						if (has_trans != 0)
						{
							if (_depth == 16)
							{
								if (ComputeTransparency16(tc16, Context.img_out_n) == 0)
									return 0;
							}
							else
							{
								if (ComputeTransparency(tc, Context.img_out_n) == 0)
									return 0;
							}
						}

						if ((is_iphone != 0) && (stbi__de_iphone_flag != 0) && (Context.img_out_n > 2))
							DeIphone();
						if (pal_img_n != 0)
						{
							Context.img_n = pal_img_n;
							Context.img_out_n = pal_img_n;
							if ((int)req_comp >= 3)
								Context.img_out_n = (int)req_comp;
							if (ExpandPngPalette(palette, (int)pal_len, (int)Context.img_out_n) == 0)
								return 0;
						}
						else if (has_trans != 0)
						{
							++Context.img_n;
						}

						CRuntime.free(_expanded);
						_expanded = null;
						return 1;
					}
					default:
						if (first != 0)
							throw new Exception("first not IHDR");
						if ((c.type & (1 << 29)) == 0)
						{
							throw new Exception("XXXX PNG chunk not known");
						}

						Context.Skip((int)c.length);
						break;
				}

				Context.Get32BigEndian();
			}
		}

		protected override unsafe byte* InternalLoad(ColorComponents comp, ref int width, ref int height, ref ColorComponents sourceComp, ref int bitsPerChannel)
		{
			byte* result = null;

			if (ParsePngFile((int)ScanType.Load, comp) != 0)
			{
				if (_depth < 8)
					bitsPerChannel = 8;
				else
					bitsPerChannel = _depth;
				result = _out_;
				_out_ = null;
				if ((comp != ColorComponents.Default) && ((int)comp != Context.img_out_n))
				{
					if (bitsPerChannel == 8)
						result = Utility.ConvertFormat(result, Context.img_out_n, (int)comp,
							(uint)Context.img_x,
							(uint)Context.img_y);
					else
						result = (byte *)Utility.ConvertFormat16((ushort*)result, 
							Context.img_out_n, (int)comp,
							(uint)Context.img_x,
							(uint)Context.img_y);
					Context.img_out_n = (int)comp;
					if (result == null)
						return result;
				}

				width = Context.img_x;
				height = Context.img_y;
				sourceComp = (ColorComponents)Context.img_n;
			}

			CRuntime.free(_out_);
			_out_ = null;
			CRuntime.free(_expanded);
			_expanded = null;
			CRuntime.free(_idata);
			_idata = null;
			return result;
		}

		protected override unsafe bool InternalInfo(ref int width, ref int height, ref ColorComponents sourceComp)
		{
			if (ParsePngFile(ScanType.Header, 0) == 0)
			{
				Context.Rewind();
				return false;
			}

			width = Context.img_x;
			height = Context.img_y;
			sourceComp = (ColorComponents)Context.img_n;
			return true;
		}

		protected override bool InternalTest()
		{
			try
			{
				CheckPngHeader();
			}
			catch (Exception)
			{
				return false;
			}

			return true;
		}
	}
}