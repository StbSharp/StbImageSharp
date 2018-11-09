using System;

namespace StbImageSharp
{
	public unsafe static class Utility
	{
		public static int AddSizesValid(int a, int b)
		{
			if (b < 0)
				return 0;
			return (a <= 2147483647 - b) ? 1 : 0;
		}

		public static int Mul2SizesValid(int a, int b)
		{
			if ((a < 0) || (b < 0))
				return 0;
			if (b == 0)
				return 1;
			return (a <= 2147483647 / b) ? 1 : 0;
		}

		public static int Mad2SizesValid(int a, int b, int add)
		{
			return (Mul2SizesValid(a, b) != 0) &&
				 (AddSizesValid(a * b, add) != 0)
					? 1
					: 0;
		}

		public static int Mad3SizesValid(int a, int b, int c, int add)
		{
			return (Mul2SizesValid(a, b) != 0) &&
				  (Mul2SizesValid(a * b, c) != 0) &&
				 (AddSizesValid(a * b * c, add) != 0)
					? 1
					: 0;
		}

		public static int Mad4SizesValid(int a, int b, int c, int d, int add)
		{
			return (Mul2SizesValid(a, b) != 0) &&
				   (Mul2SizesValid(a * b, c) != 0) &&
				  (Mul2SizesValid(a * b * c, d) != 0) &&
				 (AddSizesValid(a * b * c * d, add) != 0) ? 1 : 0;
		}

		public static void* MallocMad2(int a, int b, int add)
		{
			if (Mad2SizesValid(a, b, add) == 0)
				return null;
			return CRuntime.malloc((ulong)(a * b + add));
		}

		public static void* MallocMad3(int a, int b, int c, int add)
		{
			if (Mad3SizesValid(a, b, c, add) == 0)
				return null;
			return CRuntime.malloc((ulong)(a * b * c + add));
		}

		public static void* MallocMad4(int a, int b, int c, int d, int add)
		{
			if (Mad4SizesValid(a, b, c, d, add) == 0)
				return null;
			return CRuntime.malloc((ulong)(a * b * c * d + add));
		}

		public static byte* Convert16to8(ushort* orig, int w, int h, int channels)
		{
			int i;
			int img_len = w * h * channels;
			byte* reduced;
			reduced = (byte*)CRuntime.malloc((ulong)img_len);
			for (i = 0; i < img_len; ++i)
			{
				reduced[i] = (byte)((orig[i] >> 8) & 0xFF);
			}

			CRuntime.free(orig);
			return reduced;
		}

		public static ushort* Convert8to16(byte* orig, int w, int h, int channels)
		{
			int i;
			int img_len = w * h * channels;
			ushort* enlarged;
			enlarged = (ushort*)CRuntime.malloc((ulong)(img_len * 2));
			for (i = 0; i < img_len; ++i)
			{
				enlarged[i] = (ushort)((orig[i] << 8) + orig[i]);
			}

			CRuntime.free(orig);
			return enlarged;
		}

		public static void stbi__vertical_flip(void* image, int w, int h, int bytes_per_pixel)
		{
			int row;
			ulong bytes_per_row = (ulong)(w * bytes_per_pixel);
			byte* temp = stackalloc byte[2048];
			byte* bytes = (byte*)image;
			for (row = 0; row < (h >> 1); row++)
			{
				byte* row0 = bytes + (ulong)row * bytes_per_row;
				byte* row1 = bytes + (ulong)(h - row - 1) * bytes_per_row;
				ulong bytes_left = bytes_per_row;
				while (bytes_left != 0)
				{
					ulong bytes_copy = (bytes_left < 2048) ? bytes_left : 2048;
					CRuntime.memcpy(temp, row0, bytes_copy);
					CRuntime.memcpy(row0, row1, bytes_copy);
					CRuntime.memcpy(row1, temp, bytes_copy);
					row0 += bytes_copy;
					row1 += bytes_copy;
					bytes_left -= bytes_copy;
				}
			}
		}

		public static byte* stbi__load_and_postprocess_8bit(stbi__context s, int* x, int* y, ColorComponents* comp, ColorComponents req_comp)
		{
			stbi__result_info ri = new stbi__result_info();
			void* result = stbi__load_main(s, x, y, comp, req_comp, &ri, 8);
			if (result == null)
				return null;
			if (ri.bits_per_channel != 8)
			{
				result = Convert16to8((ushort*)result, *x, *y,
					req_comp == 0 ? *comp : req_comp);
				ri.bits_per_channel = 8;
			}

			if (stbi__vertically_flip_on_load != 0)
			{
				int channels = req_comp != 0 ? req_comp : *comp;
				stbi__vertical_flip(result, *x, *y, channels);
			}

			return (byte*)result;
		}

		public static ushort* stbi__load_and_postprocess_16bit(stbi__context s, int* x, int* y, ColorComponents* comp, ColorComponents req_comp)
		{
			stbi__result_info ri = new stbi__result_info();
			void* result = stbi__load_main(s, x, y, comp, req_comp, &ri, 16);
			if (result == null)
				return null;
			if (ri.bits_per_channel != 16)
			{
				result = Convert8to16((byte*)result, *x, *y,
					req_comp == 0 ? *comp : req_comp);
				ri.bits_per_channel = 16;
			}

			if (stbi__vertically_flip_on_load != 0)
			{
				int channels = req_comp != 0 ? req_comp : *comp;
				stbi__vertical_flip(result, *x, *y, channels * 2);
			}

			return (ushort*)result;
		}

		public static ushort* stbi_load_16_from_memory(byte* buffer, int len, int* x, int* y, int* channels_in_file,
			int desired_channels)
		{
			stbi__context s = new stbi__context();
			stbi__start_mem(s, buffer, len);
			return stbi__load_and_postprocess_16bit(s, x, y, channels_in_file, desired_channels);
		}

		public static ushort* stbi_load_16_from_callbacks(stbi_io_callbacks clbk, void* user, int* x, int* y,
			int* channels_in_file, int desired_channels)
		{
			stbi__context s = new stbi__context();
			stbi__start_callbacks(s, clbk, user);
			return stbi__load_and_postprocess_16bit(s, x, y, channels_in_file, desired_channels);
		}

		public static byte* stbi_load_from_memory(byte* buffer, int len, int* x, int* y, ColorComponents* comp, ColorComponents req_comp)
		{
			stbi__context s = new stbi__context();
			stbi__start_mem(s, buffer, len);
			return stbi__load_and_postprocess_8bit(s, x, y, comp, req_comp);
		}

		public static byte* stbi_load_from_callbacks(stbi_io_callbacks clbk, void* user, int* x, int* y, ColorComponents* comp,
			ColorComponents req_comp)
		{
			stbi__context s = new stbi__context();
			stbi__start_callbacks(s, clbk, user);
			return stbi__load_and_postprocess_8bit(s, x, y, comp, req_comp);
		}

		public static void stbi_hdr_to_ldr_gamma(float gamma)
		{
			stbi__h2l_gamma_i = 1 / gamma;
		}

		public static void stbi_hdr_to_ldr_scale(float scale)
		{
			stbi__h2l_scale_i = 1 / scale;
		}

		public static byte ComputeY(int r, int g, int b)
		{
			return (byte)(((r * 77) + (g * 150) + (29 * b)) >> 8);
		}

		public static ushort ComputeY16(int r, int g, int b)
		{
			return (ushort)(((r * 77) + (g * 150) + (29 * b)) >> 8);
		}

		public static byte* ConvertFormat(byte* data, int img_n, ColorComponents req_comp, uint x, uint y)
		{
			int i;
			int j;
			byte* good;
			if (req_comp == img_n)
				return data;
			good = (byte*)Utility.MallocMad3(req_comp, (int)x, (int)y, 0);

			for (j = 0; j < ((int)y); ++j)
			{
				byte* src = data + j * x * img_n;
				byte* dest = good + j * x * req_comp;
				switch (img_n * 8 + req_comp)
				{
					case 1 * 8 + 2:
						for (i = (int)(x - 1); i >= 0; --i, src += 1, dest += 2)
						{
							dest[0] = src[0];
							dest[1] = 255;
						}

						break;
					case 1 * 8 + 3:
						for (i = (int)(x - 1); i >= 0; --i, src += 1, dest += 3)
						{
							dest[0] = dest[1] = dest[2] = src[0];
						}

						break;
					case 1 * 8 + 4:
						for (i = (int)(x - 1); i >= 0; --i, src += 1, dest += 4)
						{
							dest[0] = dest[1] = dest[2] = src[0];
							dest[3] = 255;
						}

						break;
					case 2 * 8 + 1:
						for (i = (int)(x - 1); i >= 0; --i, src += 2, dest += 1)
						{
							dest[0] = src[0];
						}

						break;
					case 2 * 8 + 3:
						for (i = (int)(x - 1); i >= 0; --i, src += 2, dest += 3)
						{
							dest[0] = dest[1] = dest[2] = src[0];
						}

						break;
					case 2 * 8 + 4:
						for (i = (int)(x - 1); i >= 0; --i, src += 2, dest += 4)
						{
							dest[0] = dest[1] = dest[2] = src[0];
							dest[3] = src[1];
						}

						break;
					case 3 * 8 + 4:
						for (i = (int)(x - 1); i >= 0; --i, src += 3, dest += 4)
						{
							dest[0] = src[0];
							dest[1] = src[1];
							dest[2] = src[2];
							dest[3] = 255;
						}

						break;
					case 3 * 8 + 1:
						for (i = (int)(x - 1); i >= 0; --i, src += 3, dest += 1)
						{
							dest[0] = ComputeY(src[0], src[1], src[2]);
						}

						break;
					case 3 * 8 + 2:
						for (i = (int)(x - 1); i >= 0; --i, src += 3, dest += 2)
						{
							dest[0] = ComputeY(src[0], src[1], src[2]);
							dest[1] = 255;
						}

						break;
					case 4 * 8 + 1:
						for (i = (int)(x - 1); i >= 0; --i, src += 4, dest += 1)
						{
							dest[0] = ComputeY(src[0], src[1], src[2]);
						}

						break;
					case 4 * 8 + 2:
						for (i = (int)(x - 1); i >= 0; --i, src += 4, dest += 2)
						{
							dest[0] = ComputeY(src[0], src[1], src[2]);
							dest[1] = src[3];
						}

						break;
					case 4 * 8 + 3:
						for (i = (int)(x - 1); i >= 0; --i, src += 4, dest += 3)
						{
							dest[0] = src[0];
							dest[1] = src[1];
							dest[2] = src[2];
						}

						break;
					default:
						throw new Exception("0");
				}
			}

			CRuntime.free(data);
			return good;
		}

		public static ushort* ConvertFormat16(ushort* data, int img_n, ColorComponents req_comp, uint x, uint y)
		{
			int i;
			int j;
			ushort* good;
			if (req_comp == img_n)
				return data;
			good = (ushort*)CRuntime.malloc((ulong)(req_comp * x * y * 2));
			if (good == null)
			{
				CRuntime.free(data);
				return (ushort*)(stbi__err("outofmem") != 0 ? ((byte*)null) : null);
			}

			for (j = 0; j < ((int)y); ++j)
			{
				ushort* src = data + j * x * img_n;
				ushort* dest = good + j * x * req_comp;
				switch (img_n * 8 + req_comp)
				{
					case 1 * 8 + 2:
						for (i = (int)(x - 1); i >= 0; --i, src += 1, dest += 2)
						{
							dest[0] = src[0];
							dest[1] = 0xffff;
						}

						break;
					case 1 * 8 + 3:
						for (i = (int)(x - 1); i >= 0; --i, src += 1, dest += 3)
						{
							dest[0] = dest[1] = dest[2] = src[0];
						}

						break;
					case 1 * 8 + 4:
						for (i = (int)(x - 1); i >= 0; --i, src += 1, dest += 4)
						{
							dest[0] = dest[1] = dest[2] = src[0];
							dest[3] = 0xffff;
						}

						break;
					case 2 * 8 + 1:
						for (i = (int)(x - 1); i >= 0; --i, src += 2, dest += 1)
						{
							dest[0] = src[0];
						}

						break;
					case 2 * 8 + 3:
						for (i = (int)(x - 1); i >= 0; --i, src += 2, dest += 3)
						{
							dest[0] = dest[1] = dest[2] = src[0];
						}

						break;
					case 2 * 8 + 4:
						for (i = (int)(x - 1); i >= 0; --i, src += 2, dest += 4)
						{
							dest[0] = dest[1] = dest[2] = src[0];
							dest[3] = src[1];
						}

						break;
					case 3 * 8 + 4:
						for (i = (int)(x - 1); i >= 0; --i, src += 3, dest += 4)
						{
							dest[0] = src[0];
							dest[1] = src[1];
							dest[2] = src[2];
							dest[3] = 0xffff;
						}

						break;
					case 3 * 8 + 1:
						for (i = (int)(x - 1); i >= 0; --i, src += 3, dest += 1)
						{
							dest[0] = ComputeY16(src[0], src[1], src[2]);
						}

						break;
					case 3 * 8 + 2:
						for (i = (int)(x - 1); i >= 0; --i, src += 3, dest += 2)
						{
							dest[0] = ComputeY16(src[0], src[1], src[2]);
							dest[1] = 0xffff;
						}

						break;
					case 4 * 8 + 1:
						for (i = (int)(x - 1); i >= 0; --i, src += 4, dest += 1)
						{
							dest[0] = ComputeY16(src[0], src[1], src[2]);
						}

						break;
					case 4 * 8 + 2:
						for (i = (int)(x - 1); i >= 0; --i, src += 4, dest += 2)
						{
							dest[0] = ComputeY16(src[0], src[1], src[2]);
							dest[1] = src[3];
						}

						break;
					case 4 * 8 + 3:
						for (i = (int)(x - 1); i >= 0; --i, src += 4, dest += 3)
						{
							dest[0] = src[0];
							dest[1] = src[1];
							dest[2] = src[2];
						}

						break;
					default:
						return (ushort*)(stbi__err("0") != 0 ? ((byte*)null) : null);
				}
			}

			CRuntime.free(data);
			return good;
		}

		public static int stbi__bitreverse16(int n)
		{
			n = ((n & 0xAAAA) >> 1) | ((n & 0x5555) << 1);
			n = ((n & 0xCCCC) >> 2) | ((n & 0x3333) << 2);
			n = ((n & 0xF0F0) >> 4) | ((n & 0x0F0F) << 4);
			n = ((n & 0xFF00) >> 8) | ((n & 0x00FF) << 8);
			return n;
		}

		public static int stbi__bit_reverse(int v, int bits)
		{
			return stbi__bitreverse16(v) >> (16 - bits);
		}
	}
}