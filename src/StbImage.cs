using System;
using System.IO;
using System.Runtime.InteropServices;

namespace StbImageSharp
{
#if !STBSHARP_INTERNAL
	public
#else
	internal
#endif
	static unsafe partial class StbImage
	{
		public static string LastError;

		public const int STBI__ZFAST_BITS = 9;

		public delegate void idct_block_kernel(byte* output, int out_stride, short* data);

		public delegate void YCbCr_to_RGB_kernel(
			byte* output, byte* y, byte* pcb, byte* pcr, int count, int step);

		public delegate byte* Resampler(byte* a, byte* b, byte* c, int d, int e);

		public static string stbi__g_failure_reason;
		public static int stbi__vertically_flip_on_load;

		public class stbi__context
		{
			private readonly Stream _stream;

			public byte[] _tempBuffer;
			public int img_n = 0;
			public int img_out_n = 0;
			public uint img_x = 0;
			public uint img_y = 0;

			public stbi__context(Stream stream)
			{
				if (stream == null)
					throw new ArgumentNullException("stream");

				_stream = stream;
			}

			public Stream Stream
			{
				get
				{
					return _stream;
				}
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct img_comp
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

		public class stbi__jpeg
		{
			public readonly ushort[][] dequant;

			public readonly short[][] fast_ac;
			public readonly stbi__huffman[] huff_ac = new stbi__huffman[4];
			public readonly stbi__huffman[] huff_dc = new stbi__huffman[4];
			public int app14_color_transform; // Adobe APP14 tag
			public int code_bits; // number of valid bits

			public uint code_buffer; // jpeg entropy-coded buffer
			public int eob_run;

			// kernels
			public idct_block_kernel idct_block_kernel;

			// definition of jpeg image component
			public img_comp[] img_comp = new img_comp[4];

			// sizes for components, interleaved MCUs
			public int img_h_max, img_v_max;
			public int img_mcu_w, img_mcu_h;
			public int img_mcu_x, img_mcu_y;
			public int jfif;
			public byte marker; // marker seen while filling entropy buffer
			public int nomore; // flag if we saw a marker so must stop
			public int[] order = new int[4];

			public int progressive;
			public Resampler resample_row_hv_2_kernel;
			public int restart_interval, todo;
			public int rgb;
			public stbi__context s;

			public int scan_n;
			public int spec_end;
			public int spec_start;
			public int succ_high;
			public int succ_low;
			public YCbCr_to_RGB_kernel YCbCr_to_RGB_kernel;

			public stbi__jpeg()
			{
				for (var i = 0; i < 4; ++i)
				{
					huff_ac[i] = new stbi__huffman();
					huff_dc[i] = new stbi__huffman();
				}

				for (var i = 0; i < img_comp.Length; ++i)
					img_comp[i] = new img_comp();

				fast_ac = new short[4][];
				for (var i = 0; i < fast_ac.Length; ++i)
					fast_ac[i] = new short[1 << STBI__ZFAST_BITS];

				dequant = new ushort[4][];
				for (var i = 0; i < dequant.Length; ++i)
					dequant[i] = new ushort[64];
			}
		}

		public class stbi__resample
		{
			public int hs;
			public byte* line0;
			public byte* line1;
			public Resampler resample;
			public int vs;
			public int w_lores;
			public int ypos;
			public int ystep;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct stbi__gif_lzw
		{
			public short prefix;
			public byte first;
			public byte suffix;
		}

		public class stbi__gif : IDisposable
		{
			public byte* _out_;
			public byte* background;
			public int bgindex;
			public stbi__gif_lzw* codes = (stbi__gif_lzw*)stbi__malloc(8192 * sizeof(stbi__gif_lzw));
			public byte* color_table;
			public int cur_x;
			public int cur_y;
			public int delay;
			public int eflags;
			public int flags;
			public int h;
			public byte* history;
			public int lflags;
			public int line_size;
			public byte* lpal;
			public int max_x;
			public int max_y;
			public byte* pal;
			public int parse;
			public int ratio;
			public int start_x;
			public int start_y;
			public int step;
			public int transparent;
			public int w;

			public stbi__gif()
			{
				pal = (byte*)stbi__malloc(256 * 4 * sizeof(byte));
				lpal = (byte*)stbi__malloc(256 * 4 * sizeof(byte));
			}

			public void Dispose()
			{
				if (pal != null)
				{
					CRuntime.free(pal);
					pal = null;
				}

				if (lpal != null)
				{
					CRuntime.free(lpal);
					lpal = null;
				}

				if (codes != null)
				{
					CRuntime.free(codes);
					codes = null;
				}
			}

			~stbi__gif()
			{
				Dispose();
			}
		}

		private static void* stbi__malloc(int size)
		{
			return CRuntime.malloc((ulong)size);
		}

		private static void* stbi__malloc(ulong size)
		{
			return stbi__malloc((int)size);
		}

		private static int stbi__err(string str)
		{
			LastError = str;
			return 0;
		}

		public static void stbi__gif_parse_colortable(stbi__context s, byte* pal, int num_entries, int transp)
		{
			int i;
			for (i = 0; i < num_entries; ++i)
			{
				pal[i * 4 + 2] = stbi__get8(s);
				pal[i * 4 + 1] = stbi__get8(s);
				pal[i * 4] = stbi__get8(s);
				pal[i * 4 + 3] = (byte)(transp == i ? 0 : 255);
			}
		}

		public static byte stbi__get8(stbi__context s)
		{
			var b = s.Stream.ReadByte();
			if (b == -1)
			{
				return 0;
			}

			return (byte)b;
		}

		public static void stbi__skip(stbi__context s, int skip)
		{
			s.Stream.Seek(skip, SeekOrigin.Current);
		}

		public static void stbi__rewind(stbi__context s)
		{
			s.Stream.Seek(0, SeekOrigin.Begin);
		}

		public static int stbi__at_eof(stbi__context s)
		{
			return s.Stream.Position == s.Stream.Length ? 1 : 0;
		}

		public static int stbi__getn(stbi__context s, byte* buf, int size)
		{
			if (s._tempBuffer == null ||
				s._tempBuffer.Length < size)
				s._tempBuffer = new byte[size * 2];

			var result = s.Stream.Read(s._tempBuffer, 0, size);
			Marshal.Copy(s._tempBuffer, 0, new IntPtr(buf), result);

			return result;
		}
	}
}
