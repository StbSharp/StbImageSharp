using System;

namespace StbImageSharp
{
	public unsafe class TgaDecoder : BaseDecoder
	{
		public static int stbi__tga_get_comp(int bits_per_pixel, int is_grey, int* is_rgb16)
		{
			if ((is_rgb16) != null)
				*is_rgb16 = (int)(0);
			switch (bits_per_pixel)
			{
				case 8:
					return (int)(ColorComponents.Grey);
				case 15:
				case 16:
					if (((bits_per_pixel) == (16)) && ((is_grey) != 0))
						return (int)(ColorComponents.GreyAlpha);
					if ((is_rgb16) != null)
						*is_rgb16 = (int)(1);
					return (int)(ColorComponents.RedGreenBlue);
				case 24:
				case 32:
					return (int)(bits_per_pixel / 8);
				default:
					return (int)(0);
			}
		}

		protected override unsafe bool InternalInfo(ref int width, ref int height, ref ColorComponents sourceComp)
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
				Context.Rewind();
				return false;
			}

			tga_image_type = (int)(Context.Get8());
			if ((tga_colormap_type) == (1))
			{
				if ((tga_image_type != 1) && (tga_image_type != 9))
				{
					Context.Rewind();
					return false;
				}

				Context.Skip((int)(4));
				sz = (int)(Context.Get8());
				if (((((sz != 8) && (sz != 15)) && (sz != 16)) && (sz != 24)) && (sz != 32))
				{
					Context.Rewind();
					return false;
				}

				Context.Skip((int)(4));
				tga_colormap_bpp = (int)(sz);
			}
			else
			{
				if ((((tga_image_type != 2) && (tga_image_type != 3)) && (tga_image_type != 10)) &&
					(tga_image_type != 11))
				{
					Context.Rewind();
					return false;
				}

				Context.Skip((int)(9));
				tga_colormap_bpp = 0;
			}

			tga_w = (int)(Context.Get16LittleEndian());
			if ((tga_w) < (1))
			{
				Context.Rewind();
				return false;
			}

			tga_h = (int)(Context.Get16LittleEndian());
			if ((tga_h) < (1))
			{
				Context.Rewind();
				return false;
			}

			tga_bits_per_pixel = (int)(Context.Get8());
			Context.Get8();
			if (tga_colormap_bpp != 0)
			{
				if ((tga_bits_per_pixel != 8) && (tga_bits_per_pixel != 16))
				{
					Context.Rewind();
					return false;
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
				Context.Rewind();
				return false;
			}

			width = (int)(tga_w);
			height = (int)(tga_h);
			sourceComp = (ColorComponents)(tga_comp);
			return true;
		}

		protected override bool InternalTest()
		{
			var res = false;
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
				Context.Skip((int)(4));
				sz = (int)(Context.Get8());
				if (((((sz != 8) && (sz != 15)) && (sz != 16)) && (sz != 24)) && (sz != 32))
					goto errorEnd;
				Context.Skip((int)(4));
			}
			else
			{
				if ((((sz != 2) && (sz != 3)) && (sz != 10)) && (sz != 11))
					goto errorEnd;
				Context.Skip((int)(9));
			}

			if ((Context.Get16LittleEndian()) < (1))
				goto errorEnd;
			if ((Context.Get16LittleEndian()) < (1))
				goto errorEnd;
			sz = (int)(Context.Get8());
			if ((((tga_color_type) == (1)) && (sz != 8)) && (sz != 16))
				goto errorEnd;
			if (((((sz != 8) && (sz != 15)) && (sz != 16)) && (sz != 24)) && (sz != 32))
				goto errorEnd;
			res = true;
		errorEnd:
			;
			Context.Rewind();
			return res;
		}

		public void stbi__tga_read_rgb16(byte* _out_)
		{
			ushort px = (ushort)(Context.Get16LittleEndian());
			ushort fiveBitMask = (ushort)(31);
			int r = (int)((px >> 10) & fiveBitMask);
			int g = (int)((px >> 5) & fiveBitMask);
			int b = (int)(px & fiveBitMask);
			_out_[0] = ((byte)((r * 255) / 31));
			_out_[1] = ((byte)((g * 255) / 31));
			_out_[2] = ((byte)((b * 255) / 31));
		}

		protected override unsafe byte* InternalLoad(ColorComponents comp, ref int width, ref int height, ref ColorComponents sourceComp, ref int bitsPerChannel)
		{
			int tga_offset = (int)(Context.Get8());
			int tga_indexed = (int)(Context.Get8());
			int tga_image_type = (int)(Context.Get8());
			int tga_is_RLE = (int)(0);
			int tga_palette_start = (int)(Context.Get16LittleEndian());
			int tga_palette_len = (int)(Context.Get16LittleEndian());
			int tga_palette_bits = (int)(Context.Get8());
			int tga_x_origin = (int)(Context.Get16LittleEndian());
			int tga_y_origin = (int)(Context.Get16LittleEndian());
			int tga_width = (int)(Context.Get16LittleEndian());
			int tga_height = (int)(Context.Get16LittleEndian());
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
				throw new Exception("bad format");
			width = (int)(tga_width);
			height = (int)(tga_height);
			sourceComp = (ColorComponents)(tga_comp);
			if (Utility.Mad3SizesValid((int)(tga_width), (int)(tga_height), (int)(tga_comp), (int)(0)) == 0)
				throw new Exception("too large");
			tga_data = (byte*)(Utility.MallocMad3((int)(tga_width), (int)(tga_height), (int)(tga_comp), (int)(0)));
			Context.Skip((int)(tga_offset));
			if (((tga_indexed == 0) && (tga_is_RLE == 0)) && (tga_rgb16 == 0))
			{
				for (i = (int)(0); (i) < (tga_height); ++i)
				{
					int row = (int)((tga_inverted) != 0 ? tga_height - i - 1 : i);
					byte* tga_row = tga_data + row * tga_width * tga_comp;
					Context.Getn(tga_row, (int)(tga_width * tga_comp));
				}
			}
			else
			{
				if ((tga_indexed) != 0)
				{
					Context.Skip((int)(tga_palette_start));
					tga_palette = (byte*)(Utility.MallocMad2((int)(tga_palette_len), (int)(tga_comp), (int)(0)));

					if ((tga_rgb16) != 0)
					{
						byte* pal_entry = tga_palette;
						for (i = (int)(0); (i) < (tga_palette_len); ++i)
						{
							stbi__tga_read_rgb16(pal_entry);
							pal_entry += tga_comp;
						}
					}
					else if (!Context.Getn(tga_palette, (int)(tga_palette_len * tga_comp)))
					{
						CRuntime.free(tga_data);
						CRuntime.free(tga_palette);
						throw new Exception("bad palette");
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
							int pal_idx = (int)(((tga_bits_per_pixel) == (8)) ? Context.Get8() : Context.Get16LittleEndian());
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
							stbi__tga_read_rgb16(raw_data);
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

			if (((comp) != ColorComponents.Default) && ((int)comp != tga_comp))
				tga_data = Utility.ConvertFormat(tga_data, (int)(tga_comp), (int)(comp), (uint)(tga_width), (uint)(tga_height));
			tga_palette_start =
				(int)(tga_palette_len =
					(int)(tga_palette_bits = (int)(tga_x_origin = (int)(tga_y_origin = (int)(0)))));
			return tga_data;
		}
	}
}