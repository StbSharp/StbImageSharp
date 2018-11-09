using System.Runtime.InteropServices;

namespace StbImageSharp
{
	unsafe partial class StbImage
	{
		[StructLayout(LayoutKind.Sequential)]
		public struct stbi__result_info
		{
			public int bits_per_channel;
			public int num_channels;
			public int channel_order;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct stbi__bmp_data
		{
			public int bpp;
			public int offset;
			public int hsz;
			public uint mr;
			public uint mg;
			public uint mb;
			public uint ma;
			public uint all_a;
		}

		public const int STBI_ORDER_RGB = 0;
		public const int STBI_ORDER_BGR = 1;

		public static float stbi__h2l_gamma_i = (float)(1.0f / 2.2f);
		public static float stbi__h2l_scale_i = (float)(1.0f);

		public static void* stbi__load_main(stbi__context s, int* x, int* y, ColorComponents* comp, ColorComponents req_comp,
			stbi__result_info* ri,
			int bpc)
		{
			ri->bits_per_channel = (int)(8);
			ri->channel_order = (int)(STBI_ORDER_RGB);
			ri->num_channels = (int)(0);
			if ((stbi__jpeg_test(s)) != 0)
				return stbi__jpeg_load(s, x, y, comp, (int)(req_comp), ri);
			if ((stbi__png_test(s)) != 0)
				return stbi__png_load(s, x, y, comp, (int)(req_comp), ri);
			if ((stbi__bmp_test(s)) != 0)
				return stbi__bmp_load(s, x, y, comp, (int)(req_comp), ri);
			if ((stbi__gif_test(s)) != 0)
				return stbi__gif_load(s, x, y, comp, (int)(req_comp), ri);
			if ((stbi__psd_test(s)) != 0)
				return stbi__psd_load(s, x, y, comp, (int)(req_comp), ri, (int)(bpc));
			if ((stbi__tga_test(s)) != 0)
				return stbi__tga_load(s, x, y, comp, (int)(req_comp), ri);
			return ((byte*)((ulong)((stbi__err("unknown image type")) != 0 ? ((byte*)null) : (null))));
		}

		public static int stbi__bmp_test_raw(stbi__context s)
		{
			int r;
			int sz;
			if (Context.Get8() != 'B')
				return (int)(0);
			if (Context.Get8() != 'M')
				return (int)(0);
			Get32le(s);
			Get16le(s);
			Get16le(s);
			Get32le(s);
			sz = (int)(Get32le(s));
			r = (int)((((((sz) == (12)) || ((sz) == (40))) || ((sz) == (56))) || ((sz) == (108))) || ((sz) == (124))
				? 1
				: 0);
			return (int)(r);
		}

		public static int stbi__bmp_test(stbi__context s)
		{
			int r = (int)(stbi__bmp_test_raw(s));
			stbi__rewind(s);
			return (int)(r);
		}

		public static int stbi__high_bit(uint z)
		{
			int n = (int)(0);
			if ((z) == (0))
				return (int)(-1);
			if ((z) >= (0x10000))
			{
				n += (int)(16);
				z >>= 16;
			}

			if ((z) >= (0x00100))
			{
				n += (int)(8);
				z >>= 8;
			}

			if ((z) >= (0x00010))
			{
				n += (int)(4);
				z >>= 4;
			}

			if ((z) >= (0x00004))
			{
				n += (int)(2);
				z >>= 2;
			}

			if ((z) >= (0x00002))
			{
				n += (int)(1);
				z >>= 1;
			}

			return (int)(n);
		}

		public static int stbi__bitcount(uint a)
		{
			a = (uint)((a & 0x55555555) + ((a >> 1) & 0x55555555));
			a = (uint)((a & 0x33333333) + ((a >> 2) & 0x33333333));
			a = (uint)((a + (a >> 4)) & 0x0f0f0f0f);
			a = (uint)(a + (a >> 8));
			a = (uint)(a + (a >> 16));
			return (int)(a & 0xff);
		}

		public static int stbi__shiftsigned(int v, int shift, int bits)
		{
			int result;
			int z = (int)(0);
			if ((shift) < (0))
				v <<= -shift;
			else
				v >>= shift;
			result = (int)(v);
			z = (int)(bits);
			while ((z) < (8))
			{
				result += (int)(v >> z);
				z += (int)(bits);
			}

			return (int)(result);
		}

		public static void* stbi__bmp_parse_header(stbi__context s, stbi__bmp_data* info)
		{
			int hsz;
			if ((Context.Get8() != 'B') || (Context.Get8() != 'M'))
				return ((byte*)((ulong)((stbi__err("not BMP")) != 0 ? ((byte*)null) : (null))));
			Get32le(s);
			Get16le(s);
			Get16le(s);
			info->offset = (int)(Get32le(s));
			info->hsz = (int)(hsz = (int)(Get32le(s)));
			info->mr = (uint)(info->mg = (uint)(info->mb = (uint)(info->ma = (uint)(0))));
			if (((((hsz != 12) && (hsz != 40)) && (hsz != 56)) && (hsz != 108)) && (hsz != 124))
				return ((byte*)((ulong)((stbi__err("unknown BMP")) != 0 ? ((byte*)null) : (null))));
			if ((hsz) == (12))
			{
				s.img_x = (uint)(Get16le(s));
				s.img_y = (uint)(Get16le(s));
			}
			else
			{
				s.img_x = (uint)(Get32le(s));
				s.img_y = (uint)(Get32le(s));
			}

			if (Get16le(s) != 1)
				return ((byte*)((ulong)((stbi__err("bad BMP")) != 0 ? ((byte*)null) : (null))));
			info->bpp = (int)(Get16le(s));
			if ((info->bpp) == (1))
				return ((byte*)((ulong)((stbi__err("monochrome")) != 0 ? ((byte*)null) : (null))));
			if (hsz != 12)
			{
				int compress = (int)(Get32le(s));
				if (((compress) == (1)) || ((compress) == (2)))
					return ((byte*)((ulong)((stbi__err("BMP RLE")) != 0 ? ((byte*)null) : (null))));
				Get32le(s);
				Get32le(s);
				Get32le(s);
				Get32le(s);
				Get32le(s);
				if (((hsz) == (40)) || ((hsz) == (56)))
				{
					if ((hsz) == (56))
					{
						Get32le(s);
						Get32le(s);
						Get32le(s);
						Get32le(s);
					}

					if (((info->bpp) == (16)) || ((info->bpp) == (32)))
					{
						if ((compress) == (0))
						{
							if ((info->bpp) == (32))
							{
								info->mr = (uint)(0xffu << 16);
								info->mg = (uint)(0xffu << 8);
								info->mb = (uint)(0xffu << 0);
								info->ma = (uint)(0xffu << 24);
								info->all_a = (uint)(0);
							}
							else
							{
								info->mr = (uint)(31u << 10);
								info->mg = (uint)(31u << 5);
								info->mb = (uint)(31u << 0);
							}
						}
						else if ((compress) == (3))
						{
							info->mr = (uint)(Get32le(s));
							info->mg = (uint)(Get32le(s));
							info->mb = (uint)(Get32le(s));
							if (((info->mr) == (info->mg)) && ((info->mg) == (info->mb)))
							{
								return ((byte*)((ulong)((stbi__err("bad BMP")) != 0 ? ((byte*)null) : (null))));
							}
						}
						else
							return ((byte*)((ulong)((stbi__err("bad BMP")) != 0 ? ((byte*)null) : (null))));
					}
				}
				else
				{
					int i;
					if ((hsz != 108) && (hsz != 124))
						return ((byte*)((ulong)((stbi__err("bad BMP")) != 0 ? ((byte*)null) : (null))));
					info->mr = (uint)(Get32le(s));
					info->mg = (uint)(Get32le(s));
					info->mb = (uint)(Get32le(s));
					info->ma = (uint)(Get32le(s));
					Get32le(s);
					for (i = (int)(0); (i) < (12); ++i)
					{
						Get32le(s);
					}

					if ((hsz) == (124))
					{
						Get32le(s);
						Get32le(s);
						Get32le(s);
						Get32le(s);
					}
				}
			}

			return (void*)(1);
		}

		public static void* stbi__bmp_load(stbi__context s, int* x, int* y, ColorComponents* comp, ColorComponents req_comp,
			stbi__result_info* ri)
		{
			byte* _out_;
			uint mr = (uint)(0);
			uint mg = (uint)(0);
			uint mb = (uint)(0);
			uint ma = (uint)(0);
			uint all_a;
			byte* pal = stackalloc byte[256 * 4];
			int psize = (int)(0);
			int i;
			int j;
			int width;
			int flip_vertically;
			int pad;
			int target;
			stbi__bmp_data info = new stbi__bmp_data();
			info.all_a = (uint)(255);
			if ((stbi__bmp_parse_header(s, &info)) == (null))
				return (null);
			flip_vertically = (int)(((int)(s.img_y)) > (0) ? 1 : 0);
			s.img_y = (uint)(CRuntime.abs((int)(s.img_y)));
			mr = (uint)(info.mr);
			mg = (uint)(info.mg);
			mb = (uint)(info.mb);
			ma = (uint)(info.ma);
			all_a = (uint)(info.all_a);
			if ((info.hsz) == (12))
			{
				if ((info.bpp) < (24))
					psize = (int)((info.offset - 14 - 24) / 3);
			}
			else
			{
				if ((info.bpp) < (16))
					psize = (int)((info.offset - 14 - info.hsz) >> 2);
			}

			s.img_n = (int)((ma) != 0 ? 4 : 3);
			if (((req_comp) != 0) && ((req_comp) >= (3)))
				target = (int)(req_comp);
			else
				target = (int)(s.img_n);
			if (Utility.Mad3SizesValid((int)(target), (int)(s.img_x), (int)(s.img_y), (int)(0)) == 0)
				return ((byte*)((ulong)((stbi__err("too large")) != 0 ? ((byte*)null) : (null))));
			_out_ = (byte*)(Utility.MallocMad3((int)(target), (int)(s.img_x), (int)(s.img_y), (int)(0)));
			if (_out_ == null)
				return ((byte*)((ulong)((stbi__err("outofmem")) != 0 ? ((byte*)null) : (null))));
			if ((info.bpp) < (16))
			{
				int z = (int)(0);
				if (((psize) == (0)) || ((psize) > (256)))
				{
					CRuntime.free(_out_);
					return ((byte*)((ulong)((stbi__err("invalid")) != 0 ? ((byte*)null) : (null))));
				}

				for (i = (int)(0); (i) < (psize); ++i)
				{
					pal[i * 4 + 2] = (byte)(Context.Get8());
					pal[i * 4 + 1] = (byte)(Context.Get8());
					pal[i * 4 + 0] = (byte)(Context.Get8());
					if (info.hsz != 12)
						Context.Get8();
					pal[i * 4 + 3] = (byte)(255);
				}

				stbi__skip(s, (int)(info.offset - 14 - info.hsz - psize * ((info.hsz) == (12) ? 3 : 4)));
				if ((info.bpp) == (4))
					width = (int)((s.img_x + 1) >> 1);
				else if ((info.bpp) == (8))
					width = (int)(s.img_x);
				else
				{
					CRuntime.free(_out_);
					return ((byte*)((ulong)((stbi__err("bad bpp")) != 0 ? ((byte*)null) : (null))));
				}

				pad = (int)((-width) & 3);
				for (j = (int)(0); (j) < ((int)(s.img_y)); ++j)
				{
					for (i = (int)(0); (i) < ((int)(s.img_x)); i += (int)(2))
					{
						int v = (int)(Context.Get8());
						int v2 = (int)(0);
						if ((info.bpp) == (4))
						{
							v2 = (int)(v & 15);
							v >>= 4;
						}

						_out_[z++] = (byte)(pal[v * 4 + 0]);
						_out_[z++] = (byte)(pal[v * 4 + 1]);
						_out_[z++] = (byte)(pal[v * 4 + 2]);
						if ((target) == (4))
							_out_[z++] = (byte)(255);
						if ((i + 1) == ((int)(s.img_x)))
							break;
						v = (int)(((info.bpp) == (8)) ? Context.Get8() : v2);
						_out_[z++] = (byte)(pal[v * 4 + 0]);
						_out_[z++] = (byte)(pal[v * 4 + 1]);
						_out_[z++] = (byte)(pal[v * 4 + 2]);
						if ((target) == (4))
							_out_[z++] = (byte)(255);
					}

					stbi__skip(s, (int)(pad));
				}
			}
			else
			{
				int rshift = (int)(0);
				int gshift = (int)(0);
				int bshift = (int)(0);
				int ashift = (int)(0);
				int rcount = (int)(0);
				int gcount = (int)(0);
				int bcount = (int)(0);
				int acount = (int)(0);
				int z = (int)(0);
				int easy = (int)(0);
				stbi__skip(s, (int)(info.offset - 14 - info.hsz));
				if ((info.bpp) == (24))
					width = (int)(3 * s.img_x);
				else if ((info.bpp) == (16))
					width = (int)(2 * s.img_x);
				else
					width = (int)(0);
				pad = (int)((-width) & 3);
				if ((info.bpp) == (24))
				{
					easy = (int)(1);
				}
				else if ((info.bpp) == (32))
				{
					if (((((mb) == (0xff)) && ((mg) == (0xff00))) && ((mr) == (0x00ff0000))) && ((ma) == (0xff000000)))
						easy = (int)(2);
				}

				if (easy == 0)
				{
					if (((mr == 0) || (mg == 0)) || (mb == 0))
					{
						CRuntime.free(_out_);
						return ((byte*)((ulong)((stbi__err("bad masks")) != 0 ? ((byte*)null) : (null))));
					}

					rshift = (int)(stbi__high_bit((uint)(mr)) - 7);
					rcount = (int)(stbi__bitcount((uint)(mr)));
					gshift = (int)(stbi__high_bit((uint)(mg)) - 7);
					gcount = (int)(stbi__bitcount((uint)(mg)));
					bshift = (int)(stbi__high_bit((uint)(mb)) - 7);
					bcount = (int)(stbi__bitcount((uint)(mb)));
					ashift = (int)(stbi__high_bit((uint)(ma)) - 7);
					acount = (int)(stbi__bitcount((uint)(ma)));
				}

				for (j = (int)(0); (j) < ((int)(s.img_y)); ++j)
				{
					if ((easy) != 0)
					{
						for (i = (int)(0); (i) < ((int)(s.img_x)); ++i)
						{
							byte a;
							_out_[z + 2] = (byte)(Context.Get8());
							_out_[z + 1] = (byte)(Context.Get8());
							_out_[z + 0] = (byte)(Context.Get8());
							z += (int)(3);
							a = (byte)((easy) == (2) ? Context.Get8() : 255);
							all_a |= (uint)(a);
							if ((target) == (4))
								_out_[z++] = (byte)(a);
						}
					}
					else
					{
						int bpp = (int)(info.bpp);
						for (i = (int)(0); (i) < ((int)(s.img_x)); ++i)
						{
							uint v = (uint)((bpp) == (16) ? (uint)(Get16le(s)) : Get32le(s));
							int a;
							_out_[z++] = ((byte)((stbi__shiftsigned((int)(v & mr), (int)(rshift), (int)(rcount))) &
												  255));
							_out_[z++] = ((byte)((stbi__shiftsigned((int)(v & mg), (int)(gshift), (int)(gcount))) &
												  255));
							_out_[z++] = ((byte)((stbi__shiftsigned((int)(v & mb), (int)(bshift), (int)(bcount))) &
												  255));
							a = (int)((ma) != 0
								? stbi__shiftsigned((int)(v & ma), (int)(ashift), (int)(acount))
								: 255);
							all_a |= (uint)(a);
							if ((target) == (4))
								_out_[z++] = ((byte)((a) & 255));
						}
					}

					stbi__skip(s, (int)(pad));
				}
			}

			if (((target) == (4)) && ((all_a) == (0)))
				for (i = (int)(4 * s.img_x * s.img_y - 1); (i) >= (0); i -= (int)(4))
				{
					_out_[i] = (byte)(255);
				}

			if ((flip_vertically) != 0)
			{
				byte t;
				for (j = (int)(0); (j) < ((int)(s.img_y) >> 1); ++j)
				{
					byte* p1 = _out_ + j * s.img_x * target;
					byte* p2 = _out_ + (s.img_y - 1 - j) * s.img_x * target;
					for (i = (int)(0); (i) < ((int)(s.img_x) * target); ++i)
					{
						t = (byte)(p1[i]);
						p1[i] = (byte)(p2[i]);
						p2[i] = (byte)(t);
					}
				}
			}

			if (((req_comp) != 0) && (req_comp != target))
			{
				_out_ = stbi__convert_format(_out_, (int)(target), (int)(req_comp), (uint)(s.img_x),
					(uint)(s.img_y));
				if ((_out_) == (null))
					return _out_;
			}

			*x = (int)(s.img_x);
			*y = (int)(s.img_y);
			if ((comp) != null)
				*comp = (int)(s.img_n);
			return _out_;
		}

		public static int stbi__tga_get_comp(int bits_per_pixel, int is_grey, int* is_rgb16)
		{
			if ((is_rgb16) != null)
				*is_rgb16 = (int)(0);
			switch (bits_per_pixel)
			{
				case 8:
					return (int)(STBI_grey);
				case 15:
				case 16:
					if (((bits_per_pixel) == (16)) && ((is_grey) != 0))
						return (int)(STBI_grey_alpha);
					if ((is_rgb16) != null)
						*is_rgb16 = (int)(1);
					return (int)(STBI_rgb);
				case 24:
				case 32:
					return (int)(bits_per_pixel / 8);
				default:
					return (int)(0);
			}

		}

		public static int stbi__tga_info(stbi__context s, int* x, int* y, ColorComponents* comp)
		{
			int tga_w;
			int tga_h;
			int tga_comp;
			int tga_image_type;
			int tga_bits_per_pixel;
			int tga_colormap_bpp;
			int sz;
			int tga_colormap_type;
			Context.Get8();
			tga_colormap_type = (int)(Context.Get8());
			if ((tga_colormap_type) > (1))
			{
				stbi__rewind(s);
				return (int)(0);
			}

			tga_image_type = (int)(Context.Get8());
			if ((tga_colormap_type) == (1))
			{
				if ((tga_image_type != 1) && (tga_image_type != 9))
				{
					stbi__rewind(s);
					return (int)(0);
				}

				stbi__skip(s, (int)(4));
				sz = (int)(Context.Get8());
				if (((((sz != 8) && (sz != 15)) && (sz != 16)) && (sz != 24)) && (sz != 32))
				{
					stbi__rewind(s);
					return (int)(0);
				}

				stbi__skip(s, (int)(4));
				tga_colormap_bpp = (int)(sz);
			}
			else
			{
				if ((((tga_image_type != 2) && (tga_image_type != 3)) && (tga_image_type != 10)) &&
					(tga_image_type != 11))
				{
					stbi__rewind(s);
					return (int)(0);
				}

				stbi__skip(s, (int)(9));
				tga_colormap_bpp = (int)(0);
			}

			tga_w = (int)(Get16le(s));
			if ((tga_w) < (1))
			{
				stbi__rewind(s);
				return (int)(0);
			}

			tga_h = (int)(Get16le(s));
			if ((tga_h) < (1))
			{
				stbi__rewind(s);
				return (int)(0);
			}

			tga_bits_per_pixel = (int)(Context.Get8());
			Context.Get8();
			if (tga_colormap_bpp != 0)
			{
				if ((tga_bits_per_pixel != 8) && (tga_bits_per_pixel != 16))
				{
					stbi__rewind(s);
					return (int)(0);
				}

				tga_comp = (int)(stbi__tga_get_comp((int)(tga_colormap_bpp), (int)(0), (null)));
			}
			else
			{
				tga_comp =
					(int)
					(stbi__tga_get_comp((int)(tga_bits_per_pixel),
						(((tga_image_type) == (3))) || (((tga_image_type) == (11))) ? 1 : 0, (null)));
			}

			if (tga_comp == 0)
			{
				stbi__rewind(s);
				return (int)(0);
			}

			if ((x) != null)
				*x = (int)(tga_w);
			if ((y) != null)
				*y = (int)(tga_h);
			if ((comp) != null)
				*comp = (int)(tga_comp);
			return (int)(1);
		}

		public static int stbi__tga_test(stbi__context s)
		{
			int res = (int)(0);
			int sz;
			int tga_color_type;
			Context.Get8();
			tga_color_type = (int)(Context.Get8());
			if ((tga_color_type) > (1))
				goto errorEnd;
			sz = (int)(Context.Get8());
			if ((tga_color_type) == (1))
			{
				if ((sz != 1) && (sz != 9))
					goto errorEnd;
				stbi__skip(s, (int)(4));
				sz = (int)(Context.Get8());
				if (((((sz != 8) && (sz != 15)) && (sz != 16)) && (sz != 24)) && (sz != 32))
					goto errorEnd;
				stbi__skip(s, (int)(4));
			}
			else
			{
				if ((((sz != 2) && (sz != 3)) && (sz != 10)) && (sz != 11))
					goto errorEnd;
				stbi__skip(s, (int)(9));
			}

			if ((Get16le(s)) < (1))
				goto errorEnd;
			if ((Get16le(s)) < (1))
				goto errorEnd;
			sz = (int)(Context.Get8());
			if ((((tga_color_type) == (1)) && (sz != 8)) && (sz != 16))
				goto errorEnd;
			if (((((sz != 8) && (sz != 15)) && (sz != 16)) && (sz != 24)) && (sz != 32))
				goto errorEnd;
			res = (int)(1);
			errorEnd:
			;
			stbi__rewind(s);
			return (int)(res);
		}

		public static void stbi__tga_read_rgb16(stbi__context s, byte* _out_)
		{
			ushort px = (ushort)(Get16le(s));
			ushort fiveBitMask = (ushort)(31);
			int r = (int)((px >> 10) & fiveBitMask);
			int g = (int)((px >> 5) & fiveBitMask);
			int b = (int)(px & fiveBitMask);
			_out_[0] = ((byte)((r * 255) / 31));
			_out_[1] = ((byte)((g * 255) / 31));
			_out_[2] = ((byte)((b * 255) / 31));
		}

		public static void* stbi__tga_load(stbi__context s, int* x, int* y, ColorComponents* comp, ColorComponents req_comp,
			stbi__result_info* ri)
		{
			int tga_offset = (int)(Context.Get8());
			int tga_indexed = (int)(Context.Get8());
			int tga_image_type = (int)(Context.Get8());
			int tga_is_RLE = (int)(0);
			int tga_palette_start = (int)(Get16le(s));
			int tga_palette_len = (int)(Get16le(s));
			int tga_palette_bits = (int)(Context.Get8());
			int tga_x_origin = (int)(Get16le(s));
			int tga_y_origin = (int)(Get16le(s));
			int tga_width = (int)(Get16le(s));
			int tga_height = (int)(Get16le(s));
			int tga_bits_per_pixel = (int)(Context.Get8());
			int tga_comp;
			int tga_rgb16 = (int)(0);
			int tga_inverted = (int)(Context.Get8());
			byte* tga_data;
			byte* tga_palette = (null);
			int i;
			int j;
			byte* raw_data = stackalloc byte[4];
			raw_data[0] = (byte)(0);

			int RLE_count = (int)(0);
			int RLE_repeating = (int)(0);
			int read_next_pixel = (int)(1);
			if ((tga_image_type) >= (8))
			{
				tga_image_type -= (int)(8);
				tga_is_RLE = (int)(1);
			}

			tga_inverted = (int)(1 - ((tga_inverted >> 5) & 1));
			if ((tga_indexed) != 0)
				tga_comp = (int)(stbi__tga_get_comp((int)(tga_palette_bits), (int)(0), &tga_rgb16));
			else
				tga_comp = (int)(stbi__tga_get_comp((int)(tga_bits_per_pixel), (tga_image_type) == (3) ? 1 : 0,
					&tga_rgb16));
			if (tga_comp == 0)
				return ((byte*)((ulong)((stbi__err("bad format")) != 0 ? ((byte*)null) : (null))));
			*x = (int)(tga_width);
			*y = (int)(tga_height);
			if ((comp) != null)
				*comp = (int)(tga_comp);
			if (Utility.Mad3SizesValid((int)(tga_width), (int)(tga_height), (int)(tga_comp), (int)(0)) == 0)
				return ((byte*)((ulong)((stbi__err("too large")) != 0 ? ((byte*)null) : (null))));
			tga_data = (byte*)(Utility.MallocMad3((int)(tga_width), (int)(tga_height), (int)(tga_comp), (int)(0)));
			if (tga_data == null)
				return ((byte*)((ulong)((stbi__err("outofmem")) != 0 ? ((byte*)null) : (null))));
			stbi__skip(s, (int)(tga_offset));
			if (((tga_indexed == 0) && (tga_is_RLE == 0)) && (tga_rgb16 == 0))
			{
				for (i = (int)(0); (i) < (tga_height); ++i)
				{
					int row = (int)((tga_inverted) != 0 ? tga_height - i - 1 : i);
					byte* tga_row = tga_data + row * tga_width * tga_comp;
					Getn(s, tga_row, (int)(tga_width * tga_comp));
				}
			}
			else
			{
				if ((tga_indexed) != 0)
				{
					stbi__skip(s, (int)(tga_palette_start));
					tga_palette = (byte*)(Utility.MallocMad2((int)(tga_palette_len), (int)(tga_comp), (int)(0)));
					if (tga_palette == null)
					{
						CRuntime.free(tga_data);
						return ((byte*)((ulong)((stbi__err("outofmem")) != 0 ? ((byte*)null) : (null))));
					}

					if ((tga_rgb16) != 0)
					{
						byte* pal_entry = tga_palette;
						for (i = (int)(0); (i) < (tga_palette_len); ++i)
						{
							stbi__tga_read_rgb16(s, pal_entry);
							pal_entry += tga_comp;
						}
					}
					else if (Getn(s, tga_palette, (int)(tga_palette_len * tga_comp)) == 0)
					{
						CRuntime.free(tga_data);
						CRuntime.free(tga_palette);
						return ((byte*)((ulong)((stbi__err("bad palette")) != 0 ? ((byte*)null) : (null))));
					}
				}

				for (i = (int)(0); (i) < (tga_width * tga_height); ++i)
				{
					if ((tga_is_RLE) != 0)
					{
						if ((RLE_count) == (0))
						{
							int RLE_cmd = (int)(Context.Get8());
							RLE_count = (int)(1 + (RLE_cmd & 127));
							RLE_repeating = (int)(RLE_cmd >> 7);
							read_next_pixel = (int)(1);
						}
						else if (RLE_repeating == 0)
						{
							read_next_pixel = (int)(1);
						}
					}
					else
					{
						read_next_pixel = (int)(1);
					}

					if ((read_next_pixel) != 0)
					{
						if ((tga_indexed) != 0)
						{
							int pal_idx = (int)(((tga_bits_per_pixel) == (8)) ? Context.Get8() : Get16le(s));
							if ((pal_idx) >= (tga_palette_len))
							{
								pal_idx = (int)(0);
							}

							pal_idx *= (int)(tga_comp);
							for (j = (int)(0); (j) < (tga_comp); ++j)
							{
								raw_data[j] = (byte)(tga_palette[pal_idx + j]);
							}
						}
						else if ((tga_rgb16) != 0)
						{
							stbi__tga_read_rgb16(s, raw_data);
						}
						else
						{
							for (j = (int)(0); (j) < (tga_comp); ++j)
							{
								raw_data[j] = (byte)(Context.Get8());
							}
						}

						read_next_pixel = (int)(0);
					}

					for (j = (int)(0); (j) < (tga_comp); ++j)
					{
						tga_data[i * tga_comp + j] = (byte)(raw_data[j]);
					}

					--RLE_count;
				}

				if ((tga_inverted) != 0)
				{
					for (j = (int)(0); (j * 2) < (tga_height); ++j)
					{
						int index1 = (int)(j * tga_width * tga_comp);
						int index2 = (int)((tga_height - 1 - j) * tga_width * tga_comp);
						for (i = (int)(tga_width * tga_comp); (i) > (0); --i)
						{
							byte temp = (byte)(tga_data[index1]);
							tga_data[index1] = (byte)(tga_data[index2]);
							tga_data[index2] = (byte)(temp);
							++index1;
							++index2;
						}
					}
				}

				if (tga_palette != (null))
				{
					CRuntime.free(tga_palette);
				}
			}

			if (((tga_comp) >= (3)) && (tga_rgb16 == 0))
			{
				byte* tga_pixel = tga_data;
				for (i = (int)(0); (i) < (tga_width * tga_height); ++i)
				{
					byte temp = (byte)(tga_pixel[0]);
					tga_pixel[0] = (byte)(tga_pixel[2]);
					tga_pixel[2] = (byte)(temp);
					tga_pixel += tga_comp;
				}
			}

			if (((req_comp) != 0) && (req_comp != tga_comp))
				tga_data = stbi__convert_format(tga_data, (int)(tga_comp), (int)(req_comp), (uint)(tga_width),
					(uint)(tga_height));
			tga_palette_start =
				(int)(tga_palette_len =
					(int)(tga_palette_bits = (int)(tga_x_origin = (int)(tga_y_origin = (int)(0)))));
			return tga_data;
		}

		public static int stbi__psd_test(stbi__context s)
		{
			int r = (((Get32be(s)) == (0x38425053))) ? 1 : 0;
			stbi__rewind(s);
			return (int)(r);
		}

		public static int stbi__psd_decode_rle(stbi__context s, byte* p, int pixelCount)
		{
			int count;
			int nleft;
			int len;
			count = (int)(0);
			while ((nleft = (int)(pixelCount - count)) > (0))
			{
				len = (int)(Context.Get8());
				if ((len) == (128))
				{
				}
				else if ((len) < (128))
				{
					len++;
					if ((len) > (nleft))
						return (int)(0);
					count += (int)(len);
					while ((len) != 0)
					{
						*p = (byte)(Context.Get8());
						p += 4;
						len--;
					}
				}
				else if ((len) > (128))
				{
					byte val;
					len = (int)(257 - len);
					if ((len) > (nleft))
						return (int)(0);
					val = (byte)(Context.Get8());
					count += (int)(len);
					while ((len) != 0)
					{
						*p = (byte)(val);
						p += 4;
						len--;
					}
				}
			}

			return (int)(1);
		}

		public static void* stbi__psd_load(stbi__context s, int* x, int* y, ColorComponents* comp, ColorComponents req_comp,
			stbi__result_info* ri,
			int bpc)
		{
			int pixelCount;
			int channelCount;
			int compression;
			int channel;
			int i;
			int bitdepth;
			int w;
			int h;
			byte* _out_;
			if (Get32be(s) != 0x38425053)
				return ((byte*)((ulong)((stbi__err("not PSD")) != 0 ? ((byte*)null) : (null))));
			if (Get16be(s) != 1)
				return ((byte*)((ulong)((stbi__err("wrong version")) != 0 ? ((byte*)null) : (null))));
			stbi__skip(s, (int)(6));
			channelCount = (int)(Get16be(s));
			if (((channelCount) < (0)) || ((channelCount) > (16)))
				return ((byte*)((ulong)((stbi__err("wrong channel count")) != 0 ? ((byte*)null) : (null))));
			h = (int)(Get32be(s));
			w = (int)(Get32be(s));
			bitdepth = (int)(Get16be(s));
			if ((bitdepth != 8) && (bitdepth != 16))
				return ((byte*)((ulong)((stbi__err("unsupported bit depth")) != 0 ? ((byte*)null) : (null))));
			if (Get16be(s) != 3)
				return ((byte*)((ulong)((stbi__err("wrong color format")) != 0 ? ((byte*)null) : (null))));
			stbi__skip(s, (int)(Get32be(s)));
			stbi__skip(s, (int)(Get32be(s)));
			stbi__skip(s, (int)(Get32be(s)));
			compression = (int)(Get16be(s));
			if ((compression) > (1))
				return ((byte*)((ulong)((stbi__err("bad compression")) != 0 ? ((byte*)null) : (null))));
			if (Utility.Mad3SizesValid((int)(4), (int)(w), (int)(h), (int)(0)) == 0)
				return ((byte*)((ulong)((stbi__err("too large")) != 0 ? ((byte*)null) : (null))));
			if (((compression == 0) && ((bitdepth) == (16))) && ((bpc) == (16)))
			{
				_out_ = (byte*)(Utility.MallocMad3((int)(8), (int)(w), (int)(h), (int)(0)));
				ri->bits_per_channel = (int)(16);
			}
			else
				_out_ = (byte*)(CRuntime.malloc((ulong)(4 * w * h)));

			if (_out_ == null)
				return ((byte*)((ulong)((stbi__err("outofmem")) != 0 ? ((byte*)null) : (null))));
			pixelCount = (int)(w * h);
			if ((compression) != 0)
			{
				stbi__skip(s, (int)(h * channelCount * 2));
				for (channel = (int)(0); (channel) < (4); channel++)
				{
					byte* p;
					p = _out_ + channel;
					if ((channel) >= (channelCount))
					{
						for (i = (int)(0); (i) < (pixelCount); i++, p += 4)
						{
							*p = (byte)((channel) == (3) ? 255 : 0);
						}
					}
					else
					{
						if (stbi__psd_decode_rle(s, p, (int)(pixelCount)) == 0)
						{
							CRuntime.free(_out_);
							return ((byte*)((ulong)((stbi__err("corrupt")) != 0 ? ((byte*)null) : (null))));
						}
					}
				}
			}
			else
			{
				for (channel = (int)(0); (channel) < (4); channel++)
				{
					if ((channel) >= (channelCount))
					{
						if (((bitdepth) == (16)) && ((bpc) == (16)))
						{
							ushort* q = ((ushort*)(_out_)) + channel;
							ushort val = (ushort)((channel) == (3) ? 65535 : 0);
							for (i = (int)(0); (i) < (pixelCount); i++, q += 4)
							{
								*q = (ushort)(val);
							}
						}
						else
						{
							byte* p = _out_ + channel;
							byte val = (byte)((channel) == (3) ? 255 : 0);
							for (i = (int)(0); (i) < (pixelCount); i++, p += 4)
							{
								*p = (byte)(val);
							}
						}
					}
					else
					{
						if ((ri->bits_per_channel) == (16))
						{
							ushort* q = ((ushort*)(_out_)) + channel;
							for (i = (int)(0); (i) < (pixelCount); i++, q += 4)
							{
								*q = ((ushort)(Get16be(s)));
							}
						}
						else
						{
							byte* p = _out_ + channel;
							if ((bitdepth) == (16))
							{
								for (i = (int)(0); (i) < (pixelCount); i++, p += 4)
								{
									*p = ((byte)(Get16be(s) >> 8));
								}
							}
							else
							{
								for (i = (int)(0); (i) < (pixelCount); i++, p += 4)
								{
									*p = (byte)(Context.Get8());
								}
							}
						}
					}
				}
			}

			if ((channelCount) >= (4))
			{
				if ((ri->bits_per_channel) == (16))
				{
					for (i = (int)(0); (i) < (w * h); ++i)
					{
						ushort* pixel = (ushort*)(_out_) + 4 * i;
						if ((pixel[3] != 0) && (pixel[3] != 65535))
						{
							float a = (float)(pixel[3] / 65535.0f);
							float ra = (float)(1.0f / a);
							float inv_a = (float)(65535.0f * (1 - ra));
							pixel[0] = ((ushort)(pixel[0] * ra + inv_a));
							pixel[1] = ((ushort)(pixel[1] * ra + inv_a));
							pixel[2] = ((ushort)(pixel[2] * ra + inv_a));
						}
					}
				}
				else
				{
					for (i = (int)(0); (i) < (w * h); ++i)
					{
						byte* pixel = _out_ + 4 * i;
						if ((pixel[3] != 0) && (pixel[3] != 255))
						{
							float a = (float)(pixel[3] / 255.0f);
							float ra = (float)(1.0f / a);
							float inv_a = (float)(255.0f * (1 - ra));
							pixel[0] = ((byte)(pixel[0] * ra + inv_a));
							pixel[1] = ((byte)(pixel[1] * ra + inv_a));
							pixel[2] = ((byte)(pixel[2] * ra + inv_a));
						}
					}
				}
			}

			if (((req_comp) != 0) && (req_comp != 4))
			{
				if ((ri->bits_per_channel) == (16))
					_out_ = (byte*)(stbi__convert_format16((ushort*)(_out_), (int)(4), (int)(req_comp), (uint)(w),
						(uint)(h)));
				else
					_out_ = stbi__convert_format(_out_, (int)(4), (int)(req_comp), (uint)(w), (uint)(h));
				if ((_out_) == (null))
					return _out_;
			}

			if ((comp) != null)
				*comp = (int)(4);
			*y = (int)(h);
			*x = (int)(w);
			return _out_;
		}

		public static int stbi__gif_test_raw(stbi__context s)
		{
			int sz;
			if ((((Context.Get8() != 'G') || (Context.Get8() != 'I')) || (Context.Get8() != 'F')) ||
				(Context.Get8() != '8'))
				return (int)(0);
			sz = (int)(Context.Get8());
			if ((sz != '9') && (sz != '7'))
				return (int)(0);
			if (Context.Get8() != 'a')
				return (int)(0);
			return (int)(1);
		}

		public static int stbi__gif_test(stbi__context s)
		{
			int r = (int)(stbi__gif_test_raw(s));
			stbi__rewind(s);
			return (int)(r);
		}

		public static int stbi__gif_header(stbi__context s, stbi__gif g, ColorComponents* comp, int is_info)
		{
			byte version;
			if ((((Context.Get8() != 'G') || (Context.Get8() != 'I')) || (Context.Get8() != 'F')) ||
				(Context.Get8() != '8'))
				return (int)(stbi__err("not GIF"));
			version = (byte)(Context.Get8());
			if ((version != '7') && (version != '9'))
				return (int)(stbi__err("not GIF"));
			if (Context.Get8() != 'a')
				return (int)(stbi__err("not GIF"));
			stbi__g_failure_reason = "";
			g.w = (int)(Get16le(s));
			g.h = (int)(Get16le(s));
			g.flags = (int)(Context.Get8());
			g.bgindex = (int)(Context.Get8());
			g.ratio = (int)(Context.Get8());
			g.transparent = (int)(-1);
			if (comp != null)
				*comp = (int)(4);
			if ((is_info) != 0)
				return (int)(1);
			if ((g.flags & 0x80) != 0)
				stbi__gif_parse_colortable(s, g.pal, (int)(2 << (g.flags & 7)), (int)(-1));
			return (int)(1);
		}

		public static int stbi__gif_info_raw(stbi__context s, int* x, int* y, ColorComponents* comp)
		{
			stbi__gif g = new stbi__gif();
			if (stbi__gif_header(s, g, comp, (int)(1)) == 0)
			{
				stbi__rewind(s);
				return (int)(0);
			}

			if ((x) != null)
				*x = (int)(g.w);
			if ((y) != null)
				*y = (int)(g.h);

			return (int)(1);
		}

		public static void stbi__out_gif_code(stbi__gif g, ushort code)
		{
			byte* p;
			byte* c;
			if ((g.codes[code].prefix) >= (0))
				stbi__out_gif_code(g, (ushort)(g.codes[code].prefix));
			if ((g.cur_y) >= (g.max_y))
				return;
			p = &g._out_[g.cur_x + g.cur_y];
			c = &g.color_table[g.codes[code].suffix * 4];
			if ((c[3]) >= (128))
			{
				p[0] = (byte)(c[2]);
				p[1] = (byte)(c[1]);
				p[2] = (byte)(c[0]);
				p[3] = (byte)(c[3]);
			}

			g.cur_x += (int)(4);
			if ((g.cur_x) >= (g.max_x))
			{
				g.cur_x = (int)(g.start_x);
				g.cur_y += (int)(g.step);
				while (((g.cur_y) >= (g.max_y)) && ((g.parse) > (0)))
				{
					g.step = (int)((1 << g.parse) * g.line_size);
					g.cur_y = (int)(g.start_y + (g.step >> 1));
					--g.parse;
				}
			}

		}

		public static byte* stbi__process_gif_raster(stbi__context s, stbi__gif g)
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
				((stbi__gif_lzw*)(g.codes))[init_code].prefix = (short)(-1);
				((stbi__gif_lzw*)(g.codes))[init_code].first = ((byte)(init_code));
				((stbi__gif_lzw*)(g.codes))[init_code].suffix = ((byte)(init_code));
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
							return g._out_;
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
						stbi__skip(s, (int)(len));
						while ((len = (int)(Context.Get8())) > (0))
						{
							stbi__skip(s, (int)(len));
						}

						return g._out_;
					}
					else if (code <= avail)
					{
						if ((first) != 0)
							return ((byte*)((ulong)((stbi__err("no clear code")) != 0 ? ((byte*)null) : (null))));
						if ((oldcode) >= (0))
						{
							p = (stbi__gif_lzw*)g.codes + avail++;
							if ((avail) > (4096))
								return ((byte*)((ulong)((stbi__err("too many codes")) != 0 ? ((byte*)null) : (null)))
									);
							p->prefix = ((short)(oldcode));
							p->first = (byte)(g.codes[oldcode].first);
							p->suffix = (byte)(((code) == (avail)) ? p->first : g.codes[code].first);
						}
						else if ((code) == (avail))
							return ((byte*)((ulong)((stbi__err("illegal code in raster")) != 0
								? ((byte*)null)
								: (null))));

						stbi__out_gif_code(g, (ushort)(code));
						if (((avail & codemask) == (0)) && (avail <= 0x0FFF))
						{
							codesize++;
							codemask = (int)((1 << codesize) - 1);
						}

						oldcode = (int)(code);
					}
					else
					{
						return ((byte*)((ulong)((stbi__err("illegal code in raster")) != 0 ? ((byte*)null) : (null)))
							);
					}
				}
			}
		}

		public static void stbi__fill_gif_background(stbi__gif g, int x0, int y0, int x1, int y1)
		{
			int x;
			int y;
			byte* c = (byte*)g.pal + g.bgindex;
			for (y = (int)(y0); (y) < (y1); y += (int)(4 * g.w))
			{
				for (x = (int)(x0); (x) < (x1); x += (int)(4))
				{
					byte* p = &g._out_[y + x];
					p[0] = (byte)(c[2]);
					p[1] = (byte)(c[1]);
					p[2] = (byte)(c[0]);
					p[3] = (byte)(0);
				}
			}
		}

		public static byte* stbi__gif_load_next(stbi__context s, stbi__gif g, ColorComponents* comp, ColorComponents req_comp)
		{
			int i;
			byte* prev_out = null;
			if (((g._out_) == null) && (stbi__gif_header(s, g, comp, (int)(0)) == 0))
				return null;
			if (Utility.Mad3SizesValid((int)(g.w), (int)(g.h), (int)(4), (int)(0)) == 0)
				return ((byte*)((ulong)((stbi__err("too large")) != 0 ? ((byte*)null) : (null))));
			prev_out = g._out_;
			g._out_ = (byte*)(Utility.MallocMad3((int)(4), (int)(g.w), (int)(g.h), (int)(0)));
			if ((g._out_) == null)
				return ((byte*)((ulong)((stbi__err("outofmem")) != 0 ? ((byte*)null) : (null))));
			switch ((g.eflags & 0x1C) >> 2)
			{
				case 0:
					stbi__fill_gif_background(g, (int)(0), (int)(0), (int)(4 * g.w), (int)(4 * g.w * g.h));
					break;
				case 1:
					if ((prev_out) != null)
						CRuntime.memcpy(g._out_, prev_out, (ulong)(4 * g.w * g.h));
					g.old_out = prev_out;
					break;
				case 2:
					if ((prev_out) != null)
						CRuntime.memcpy(g._out_, prev_out, (ulong)(4 * g.w * g.h));
					stbi__fill_gif_background(g, (int)(g.start_x), (int)(g.start_y), (int)(g.max_x),
						(int)(g.max_y));
					break;
				case 3:
					if ((g.old_out) != null)
					{
						for (i = (int)(g.start_y); (i) < (g.max_y); i += (int)(4 * g.w))
						{
							CRuntime.memcpy(&g._out_[i + g.start_x], &g.old_out[i + g.start_x],
								(ulong)(g.max_x - g.start_x));
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
						x = (int)(Get16le(s));
						y = (int)(Get16le(s));
						w = (int)(Get16le(s));
						h = (int)(Get16le(s));
						if (((x + w) > (g.w)) || ((y + h) > (g.h)))
							return ((byte*)((ulong)((stbi__err("bad Image Descriptor")) != 0
								? ((byte*)null)
								: (null))));
						g.line_size = (int)(g.w * 4);
						g.start_x = (int)(x * 4);
						g.start_y = (int)(y * g.line_size);
						g.max_x = (int)(g.start_x + w * 4);
						g.max_y = (int)(g.start_y + h * g.line_size);
						g.cur_x = (int)(g.start_x);
						g.cur_y = (int)(g.start_y);
						g.lflags = (int)(Context.Get8());
						if ((g.lflags & 0x40) != 0)
						{
							g.step = (int)(8 * g.line_size);
							g.parse = (int)(3);
						}
						else
						{
							g.step = (int)(g.line_size);
							g.parse = (int)(0);
						}

						if ((g.lflags & 0x80) != 0)
						{
							stbi__gif_parse_colortable(s, g.lpal, (int)(2 << (g.lflags & 7)),
								(int)((g.eflags & 0x01) != 0 ? g.transparent : -1));
							g.color_table = (byte*)(g.lpal);
						}
						else if ((g.flags & 0x80) != 0)
						{
							if (((g.transparent) >= (0)) && (g.eflags & 0x01) != 0)
							{
								prev_trans = (int)(g.pal[g.transparent * 4 + 3]);
								g.pal[g.transparent * 4 + 3] = (byte)(0);
							}

							g.color_table = (byte*)(g.pal);
						}
						else
							return ((byte*)((ulong)((stbi__err("missing color table")) != 0
								? ((byte*)null)
								: (null))));

						o = stbi__process_gif_raster(s, g);
						if ((o) == (null))
							return (null);
						if (prev_trans != -1)
							g.pal[g.transparent * 4 + 3] = ((byte)(prev_trans));
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
								g.eflags = (int)(Context.Get8());
								g.delay = (int)(Get16le(s));
								g.transparent = (int)(Context.Get8());
							}
							else
							{
								stbi__skip(s, (int)(len));
								break;
							}
						}

						while ((len = (int)(Context.Get8())) != 0)
						{
							stbi__skip(s, (int)(len));
						}

						break;
					}
					case 0x3B:
						return null;
					default:
						return ((byte*)((ulong)((stbi__err("unknown code")) != 0 ? ((byte*)null) : (null))));
				}
			}
		}

		public static void* stbi__gif_load(stbi__context s, int* x, int* y, ColorComponents* comp, ColorComponents req_comp,
			stbi__result_info* ri)
		{
			byte* u = null;
			stbi__gif g = new stbi__gif();

			u = stbi__gif_load_next(s, g, comp, (int)(req_comp));
			if ((u) != null)
			{
				*x = (int)(g.w);
				*y = (int)(g.h);
				if (((req_comp) != 0) && (req_comp != 4))
					u = stbi__convert_format(u, (int)(4), (int)(req_comp), (uint)(g.w), (uint)(g.h));
			}
			else if ((g._out_) != null)
				CRuntime.free(g._out_);

			return u;
		}

		public static int stbi__gif_info(stbi__context s, int* x, int* y, ColorComponents* comp)
		{
			return (int)(stbi__gif_info_raw(s, x, y, comp));
		}

		public static int stbi__bmp_info(stbi__context s, int* x, int* y, ColorComponents* comp)
		{
			void* p;
			stbi__bmp_data info = new stbi__bmp_data();
			info.all_a = (uint)(255);
			p = stbi__bmp_parse_header(s, &info);
			stbi__rewind(s);
			if ((p) == (null))
				return (int)(0);
			if ((x) != null)
				*x = (int)(s.img_x);
			if ((y) != null)
				*y = (int)(s.img_y);
			if ((comp) != null)
				*comp = (int)((info.ma) != 0 ? 4 : 3);
			return (int)(1);
		}

		public static int stbi__psd_info(stbi__context s, int* x, int* y, ColorComponents* comp)
		{
			int channelCount;
			int dummy;
			if (x == null)
				x = &dummy;
			if (y == null)
				y = &dummy;
			if (comp == null)
				comp = &dummy;
			if (Get32be(s) != 0x38425053)
			{
				stbi__rewind(s);
				return (int)(0);
			}

			if (Get16be(s) != 1)
			{
				stbi__rewind(s);
				return (int)(0);
			}

			stbi__skip(s, (int)(6));
			channelCount = (int)(Get16be(s));
			if (((channelCount) < (0)) || ((channelCount) > (16)))
			{
				stbi__rewind(s);
				return (int)(0);
			}

			*y = (int)(Get32be(s));
			*x = (int)(Get32be(s));
			if (Get16be(s) != 8)
			{
				stbi__rewind(s);
				return (int)(0);
			}

			if (Get16be(s) != 3)
			{
				stbi__rewind(s);
				return (int)(0);
			}

			*comp = (int)(4);
			return (int)(1);
		}

		public static int stbi__info_main(stbi__context s, int* x, int* y, ColorComponents* comp)
		{
			if ((stbi__jpeg_info(s, x, y, comp)) != 0)
				return (int)(1);
			if ((stbi__png_info(s, x, y, comp)) != 0)
				return (int)(1);
			if ((stbi__gif_info(s, x, y, comp)) != 0)
				return (int)(1);
			if ((stbi__bmp_info(s, x, y, comp)) != 0)
				return (int)(1);
			if ((stbi__psd_info(s, x, y, comp)) != 0)
				return (int)(1);
			if ((stbi__tga_info(s, x, y, comp)) != 0)
				return (int)(1);
			return (int)(stbi__err("unknown image type"));
		}

		public static int stbi_info_from_memory(byte* buffer, int len, int* x, int* y, ColorComponents* comp)
		{
			stbi__context s = new stbi__context();
			stbi__start_mem(s, buffer, (int)(len));
			return (int)(stbi__info_main(s, x, y, comp));
		}

		public static int stbi_info_from_callbacks(stbi_io_callbacks c, void* user, int* x, int* y, ColorComponents* comp)
		{
			stbi__context s = new stbi__context();
			stbi__start_callbacks(s, c, user);
			return (int)(stbi__info_main(s, x, y, comp));
		}
	}
}