using System;
using System.Runtime.InteropServices;

namespace StbImageSharp
{
	public unsafe class BmpDecoder : BaseDecoder
	{
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

		public void stbi__bmp_parse_header(stbi__bmp_data* info)
		{
			int hsz;
			if ((Context.Get8() != 'B') || (Context.Get8() != 'M'))
				throw new Exception("not BMP");
			Context.Get32LittleEndian();
			Context.Get16LittleEndian();
			Context.Get16LittleEndian();
			info->offset = (int)(Context.Get32LittleEndian());
			info->hsz = (int)(hsz = (int)(Context.Get32LittleEndian()));
			info->mr = (uint)(info->mg = (uint)(info->mb = (uint)(info->ma = (uint)(0))));
			if (((((hsz != 12) && (hsz != 40)) && (hsz != 56)) && (hsz != 108)) && (hsz != 124))
				throw new Exception("Unknown BMP");
			if ((hsz) == (12))
			{
				Context.img_x = Context.Get16LittleEndian();
				Context.img_y = Context.Get16LittleEndian();
			}
			else
			{
				Context.img_x = (int)Context.Get32LittleEndian();
				Context.img_y = (int)Context.Get32LittleEndian();
			}

			if (Context.Get16LittleEndian() != 1)
				throw new Exception("bad BMP");
			info->bpp = (int)(Context.Get16LittleEndian());
			if ((info->bpp) == (1))
				throw new Exception("monochrome");
			if (hsz != 12)
			{
				int compress = (int)(Context.Get32LittleEndian());
				if (((compress) == (1)) || ((compress) == (2)))
					throw new Exception("BMP RLE");
				Context.Get32LittleEndian();
				Context.Get32LittleEndian();
				Context.Get32LittleEndian();
				Context.Get32LittleEndian();
				Context.Get32LittleEndian();
				if (((hsz) == (40)) || ((hsz) == (56)))
				{
					if ((hsz) == (56))
					{
						Context.Get32LittleEndian();
						Context.Get32LittleEndian();
						Context.Get32LittleEndian();
						Context.Get32LittleEndian();
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
							info->mr = (uint)(Context.Get32LittleEndian());
							info->mg = (uint)(Context.Get32LittleEndian());
							info->mb = (uint)(Context.Get32LittleEndian());
							if (((info->mr) == (info->mg)) && ((info->mg) == (info->mb)))
							{
								throw new Exception("bad BMP");
							}
						}
						else
							throw new Exception("bad BMP");
					}
				}
				else
				{
					int i;
					if ((hsz != 108) && (hsz != 124))
						throw new Exception("bad BMP");
					info->mr = (uint)(Context.Get32LittleEndian());
					info->mg = (uint)(Context.Get32LittleEndian());
					info->mb = (uint)(Context.Get32LittleEndian());
					info->ma = (uint)(Context.Get32LittleEndian());
					Context.Get32LittleEndian();
					for (i = (int)(0); (i) < (12); ++i)
					{
						Context.Get32LittleEndian();
					}

					if ((hsz) == (124))
					{
						Context.Get32LittleEndian();
						Context.Get32LittleEndian();
						Context.Get32LittleEndian();
						Context.Get32LittleEndian();
					}
				}
			}
		}

		protected override unsafe byte* InternalLoad(ColorComponents req_comp, ref int x, ref int y, ref ColorComponents sourceComp, ref int bitsPerChannel)
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
			stbi__bmp_parse_header(&info);
			flip_vertically = (int)(((int)(Context.img_y)) > (0) ? 1 : 0);
			Context.img_y = CRuntime.abs(Context.img_y);
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

			Context.img_n = (int)((ma) != 0 ? 4 : 3);
			if (((req_comp) != ColorComponents.Default) && (((int)req_comp) >= (3)))
				target = (int)(req_comp);
			else
				target = (int)(Context.img_n);
			if (Utility.Mad3SizesValid((int)(target), (int)(Context.img_x), (int)(Context.img_y), (int)(0)) == 0)
				throw new Exception("too large");
			_out_ = (byte*)(Utility.MallocMad3((int)(target), (int)(Context.img_x), (int)(Context.img_y), (int)(0)));
			if ((info.bpp) < (16))
			{
				int z = (int)(0);
				if (((psize) == (0)) || ((psize) > (256)))
				{
					CRuntime.free(_out_);
					throw new Exception("invalid");
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

				Context.Skip(info.offset - 14 - info.hsz - psize * ((info.hsz) == (12) ? 3 : 4));
				if ((info.bpp) == (4))
					width = (int)((Context.img_x + 1) >> 1);
				else if ((info.bpp) == (8))
					width = (int)(Context.img_x);
				else
				{
					CRuntime.free(_out_);
					throw new Exception("bad bpp");
				}

				pad = (int)((-width) & 3);
				for (j = (int)(0); (j) < ((int)(Context.img_y)); ++j)
				{
					for (i = (int)(0); (i) < ((int)(Context.img_x)); i += (int)(2))
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
						if ((i + 1) == ((int)(Context.img_x)))
							break;
						v = (int)(((info.bpp) == (8)) ? Context.Get8() : v2);
						_out_[z++] = (byte)(pal[v * 4 + 0]);
						_out_[z++] = (byte)(pal[v * 4 + 1]);
						_out_[z++] = (byte)(pal[v * 4 + 2]);
						if ((target) == (4))
							_out_[z++] = (byte)(255);
					}

					Context.Skip(pad);
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

				Context.Skip((int)(info.offset - 14 - info.hsz));
				if ((info.bpp) == (24))
					width = (int)(3 * Context.img_x);
				else if ((info.bpp) == (16))
					width = (int)(2 * Context.img_x);
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
						throw new Exception("bad masks");
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

				for (j = (int)(0); (j) < ((int)(Context.img_y)); ++j)
				{
					if ((easy) != 0)
					{
						for (i = (int)(0); (i) < ((int)(Context.img_x)); ++i)
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
						for (i = (int)(0); (i) < ((int)(Context.img_x)); ++i)
						{
							uint v = (uint)((bpp) == (16) ? (uint)(Context.Get16LittleEndian()) : Context.Get32LittleEndian());
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

					Context.Skip(pad);
				}
			}

			if (((target) == (4)) && ((all_a) == (0)))
				for (i = (int)(4 * Context.img_x * Context.img_y - 1); (i) >= (0); i -= (int)(4))
				{
					_out_[i] = (byte)(255);
				}

			if ((flip_vertically) != 0)
			{
				byte t;
				for (j = (int)(0); (j) < ((int)(Context.img_y) >> 1); ++j)
				{
					byte* p1 = _out_ + j * Context.img_x * target;
					byte* p2 = _out_ + (Context.img_y - 1 - j) * Context.img_x * target;
					for (i = (int)(0); (i) < ((int)(Context.img_x) * target); ++i)
					{
						t = (byte)(p1[i]);
						p1[i] = (byte)(p2[i]);
						p2[i] = (byte)(t);
					}
				}
			}

			x = (int)(Context.img_x);
			y = (int)(Context.img_y);
			sourceComp = (ColorComponents)(Context.img_n);

			if ((req_comp != 0) && ((int)req_comp != target))
			{
				_out_ = Utility.ConvertFormat(_out_, target, (int)(req_comp), (uint)(Context.img_x),
					(uint)(Context.img_y));
			}

			return _out_;
		}

		protected override unsafe bool InternalInfo(ref int width, ref int height, ref ColorComponents sourceComp)
		{
			stbi__bmp_data info = new stbi__bmp_data
			{
				all_a = 255
			};

			try
			{
				stbi__bmp_parse_header(&info);
			}
			catch (Exception)
			{
				return false;
			}
			finally
			{
				Context.Rewind();
			}

			width = Context.img_x;
			height = Context.img_y;
			sourceComp = (ColorComponents)(info.ma != 0 ? 4 : 3);

			return true;
		}

		protected override bool InternalTest()
		{
			int r;
			int sz;
			if (Context.Get8() != 'B')
				return false;
			if (Context.Get8() != 'M')
				return false;

			Context.Get32LittleEndian();
			Context.Get16LittleEndian();
			Context.Get16LittleEndian();
			Context.Get32LittleEndian();
			sz = (int)Context.Get32LittleEndian();
			r = ((sz) == (12)) || ((sz) == (40)) || ((sz) == (56)) || ((sz) == (108)) || ((sz) == (124))
				? 1
				: 0;
			return true;
		}
	}
}
