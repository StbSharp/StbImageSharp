using System;
using System.Runtime.InteropServices;

namespace StbImageSharp
{
	public unsafe class GifDecoder : BaseDecoder
	{
		[StructLayout(LayoutKind.Sequential)]
		public struct stbi__gif_lzw
		{
			public short prefix;
			public byte first;
			public byte suffix;
		}

		public int w;
		public int h;
		public byte* _out_;
		public byte* old_out;
		public int flags;
		public int bgindex;
		public int ratio;
		public int transparent;
		public int eflags;
		public int delay;
		public byte* pal;
		public byte* lpal;
		public stbi__gif_lzw* codes;
		public byte* color_table;
		public int parse;
		public int step;
		public int lflags;
		public int start_x;
		public int start_y;
		public int max_x;
		public int max_y;
		public int cur_x;
		public int cur_y;
		public int line_size;

		public void ParseColorTable(byte* pal, int num_entries, int transp)
		{
			int i;
			for (i = 0; i < num_entries; ++i)
			{
				pal[i * 4 + 2] = Context.Get8();
				pal[i * 4 + 1] = Context.Get8();
				pal[i * 4] = Context.Get8();
				pal[i * 4 + 3] = (byte)(transp == i ? 0 : 255);
			}
		}

		protected override bool InternalTest()
		{
			int sz;
			if ((((Context.Get8() != 'G') || (Context.Get8() != 'I')) || (Context.Get8() != 'F')) ||
				(Context.Get8() != '8'))
				return false;
			sz = (int)(Context.Get8());
			if ((sz != '9') && (sz != '7'))
				return false;
			if (Context.Get8() != 'a')
				return false;
			return true;
		}

		public void ParseHeader(ref ColorComponents comp, int is_info)
		{
			byte version;
			if ((((Context.Get8() != 'G') || (Context.Get8() != 'I')) || (Context.Get8() != 'F')) ||
				(Context.Get8() != '8'))
				throw new Exception("not GIF");
			version = (byte)(Context.Get8());
			if ((version != '7') && (version != '9'))
				throw new Exception("not GIF");
			if (Context.Get8() != 'a')
				throw new Exception("not GIF");
			w = (int)(Context.Get16LittleEndian());
			h = (int)(Context.Get16LittleEndian());
			flags = (int)(Context.Get8());
			bgindex = (int)(Context.Get8());
			ratio = (int)(Context.Get8());
			transparent = (int)(-1);
			comp = ColorComponents.RedGreenBlueAlpha;
			if ((is_info) != 0)
				return;
			if ((flags & 0x80) != 0)
				ParseColorTable(pal, (int)(2 << (flags & 7)), (int)(-1));
		}

		protected override unsafe bool InternalInfo(ref int width, ref int height, ref ColorComponents sourceComp)
		{
			try
			{
				ParseHeader(ref sourceComp, 1);
			}
			catch (Exception)
			{
				Context.Rewind();
				return false;
			}

			width = w;
			height = h;

			return true;
		}

		public void OutCode(ushort code)
		{
			byte* p;
			byte* c;
			if ((codes[code].prefix) >= (0))
				OutCode((ushort)(codes[code].prefix));
			if ((cur_y) >= (max_y))
				return;
			p = &_out_[cur_x + cur_y];
			c = &color_table[codes[code].suffix * 4];
			if ((c[3]) >= (128))
			{
				p[0] = (byte)(c[2]);
				p[1] = (byte)(c[1]);
				p[2] = (byte)(c[0]);
				p[3] = (byte)(c[3]);
			}

			cur_x += (int)(4);
			if ((cur_x) >= (max_x))
			{
				cur_x = (int)(start_x);
				cur_y += (int)(step);
				while (((cur_y) >= (max_y)) && ((parse) > (0)))
				{
					step = (int)((1 << parse) * line_size);
					cur_y = (int)(start_y + (step >> 1));
					--parse;
				}
			}

		}

		public byte* ProcessRaster()
		{
			byte lzw_cs;
			int len;
			int init_code;
			uint first;
			int codesize;
			int codemask;
			int avail;
			int oldcode;
			int bits;
			int valid_bits;
			int clear;
			stbi__gif_lzw* p;
			lzw_cs = (byte)(Context.Get8());
			if ((lzw_cs) > (12))
				return (null);
			clear = (int)(1 << lzw_cs);
			first = (uint)(1);
			codesize = (int)(lzw_cs + 1);
			codemask = (int)((1 << codesize) - 1);
			bits = (int)(0);
			valid_bits = (int)(0);
			for (init_code = (int)(0); (init_code) < (clear); init_code++)
			{
				((stbi__gif_lzw*)(codes))[init_code].prefix = (short)(-1);
				((stbi__gif_lzw*)(codes))[init_code].first = ((byte)(init_code));
				((stbi__gif_lzw*)(codes))[init_code].suffix = ((byte)(init_code));
			}

			avail = (int)(clear + 2);
			oldcode = (int)(-1);
			len = (int)(0);
			for (; ; )
			{
				if ((valid_bits) < (codesize))
				{
					if ((len) == (0))
					{
						len = (int)(Context.Get8());
						if ((len) == (0))
							return _out_;
					}

					--len;
					bits |= (int)((int)(Context.Get8()) << valid_bits);
					valid_bits += (int)(8);
				}
				else
				{
					int code = (int)(bits & codemask);
					bits >>= codesize;
					valid_bits -= (int)(codesize);
					if ((code) == (clear))
					{
						codesize = (int)(lzw_cs + 1);
						codemask = (int)((1 << codesize) - 1);
						avail = (int)(clear + 2);
						oldcode = (int)(-1);
						first = (uint)(0);
					}
					else if ((code) == (clear + 1))
					{
						Context.Skip((int)(len));
						while ((len = (int)(Context.Get8())) > (0))
						{
							Context.Skip((int)(len));
						}

						return _out_;
					}
					else if (code <= avail)
					{
						if ((first) != 0)
							throw new Exception("no clear code");
						if ((oldcode) >= (0))
						{
							p = (stbi__gif_lzw*)codes + avail++;
							if ((avail) > (4096))
								throw new Exception("too many codes");
							p->prefix = ((short)(oldcode));
							p->first = (byte)(codes[oldcode].first);
							p->suffix = (byte)(((code) == (avail)) ? p->first : codes[code].first);
						}
						else if ((code) == (avail))
							throw new Exception("illegal code in raster");

						OutCode((ushort)(code));
						if (((avail & codemask) == (0)) && (avail <= 0x0FFF))
						{
							codesize++;
							codemask = (int)((1 << codesize) - 1);
						}

						oldcode = (int)(code);
					}
					else
					{
						throw new Exception("illegal code in raster");
					}
				}
			}
		}

		public void FillBackground(int x0, int y0, int x1, int y1)
		{
			int x;
			int y;
			byte* c = (byte*)pal + bgindex;
			for (y = (int)(y0); (y) < (y1); y += (int)(4 * w))
			{
				for (x = (int)(x0); (x) < (x1); x += (int)(4))
				{
					byte* p = &_out_[y + x];
					p[0] = (byte)(c[2]);
					p[1] = (byte)(c[1]);
					p[2] = (byte)(c[0]);
					p[3] = (byte)(0);
				}
			}
		}

		public byte* LoadNext(ref ColorComponents comp, ColorComponents req_comp)
		{
			int i;
			byte* prev_out = null;
			ParseHeader(ref comp, (int)(0));

			if (Utility.Mad3SizesValid((int)(w), (int)(h), (int)(4), (int)(0)) == 0)
				throw new Exception("too large");
			prev_out = _out_;
			_out_ = (byte*)(Utility.MallocMad3((int)(4), (int)(w), (int)(h), (int)(0)));
			if ((_out_) == null)
				throw new Exception("outofmem");
			switch ((eflags & 0x1C) >> 2)
			{
				case 0:
					FillBackground((int)(0), (int)(0), (int)(4 * w), (int)(4 * w * h));
					break;
				case 1:
					if ((prev_out) != null)
						CRuntime.memcpy(_out_, prev_out, (ulong)(4 * w * h));
					old_out = prev_out;
					break;
				case 2:
					if ((prev_out) != null)
						CRuntime.memcpy(_out_, prev_out, (ulong)(4 * w * h));
					FillBackground((int)(start_x), (int)(start_y), (int)(max_x),
						(int)(max_y));
					break;
				case 3:
					if ((old_out) != null)
					{
						for (i = (int)(start_y); (i) < (max_y); i += (int)(4 * w))
						{
							CRuntime.memcpy(&_out_[i + start_x], &old_out[i + start_x],
								(ulong)(max_x - start_x));
						}
					}

					break;
			}

			for (; ; )
			{
				switch (Context.Get8())
				{
					case 0x2C:
					{
						int prev_trans = (int)(-1);
						int x;
						int y;
						int w;
						int h;
						byte* o;
						x = (int)(Context.Get16LittleEndian());
						y = (int)(Context.Get16LittleEndian());
						w = (int)(Context.Get16LittleEndian());
						h = (int)(Context.Get16LittleEndian());
						if (((x + w) > (w)) || ((y + h) > (h)))
							throw new Exception("bad Image Descriptor");
						line_size = (int)(w * 4);
						start_x = (int)(x * 4);
						start_y = (int)(y * line_size);
						max_x = (int)(start_x + w * 4);
						max_y = (int)(start_y + h * line_size);
						cur_x = (int)(start_x);
						cur_y = (int)(start_y);
						lflags = (int)(Context.Get8());
						if ((lflags & 0x40) != 0)
						{
							step = (int)(8 * line_size);
							parse = (int)(3);
						}
						else
						{
							step = (int)(line_size);
							parse = (int)(0);
						}

						if ((lflags & 0x80) != 0)
						{
							ParseColorTable(lpal, (int)(2 << (lflags & 7)),
								(int)((eflags & 0x01) != 0 ? transparent : -1));
							color_table = (byte*)(lpal);
						}
						else if ((flags & 0x80) != 0)
						{
							if (((transparent) >= (0)) && (eflags & 0x01) != 0)
							{
								prev_trans = (int)(pal[transparent * 4 + 3]);
								pal[transparent * 4 + 3] = (byte)(0);
							}

							color_table = (byte*)(pal);
						}
						else
							throw new Exception("missing color table");

						o = ProcessRaster();
						if ((o) == (null))
							return (null);
						if (prev_trans != -1)
							pal[transparent * 4 + 3] = ((byte)(prev_trans));
						return o;
					}
					case 0x21:
					{
						int len;
						if ((Context.Get8()) == (0xF9))
						{
							len = (int)(Context.Get8());
							if ((len) == (4))
							{
								eflags = (int)(Context.Get8());
								delay = (int)(Context.Get16LittleEndian());
								transparent = (int)(Context.Get8());
							}
							else
							{
								Context.Skip((int)(len));
								break;
							}
						}

						while ((len = (int)(Context.Get8())) != 0)
						{
							Context.Skip((int)(len));
						}

						break;
					}
					case 0x3B:
						return null;
					default:
						throw new Exception("unknown code");
				}
			}
		}

		protected override unsafe byte* InternalLoad(ColorComponents req_comp, ref int x, ref int y, ref ColorComponents sourceComp, ref int bitsPerChannel)
		{
			byte* u = null;

			u = LoadNext(ref sourceComp, req_comp);
			if ((u) != null)
			{
				x = (int)(w);
				y = (int)(h);
				if (((req_comp) != 0) && (req_comp != ColorComponents.RedGreenBlueAlpha))
					u = Utility.ConvertFormat(u, (int)(4), (int)(req_comp), (uint)(w), (uint)(h));
			}
			else if ((_out_) != null)
				CRuntime.free(_out_);

			return u;
		}
	}
}