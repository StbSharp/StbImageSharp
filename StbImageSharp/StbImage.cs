using System;
using System.Runtime.InteropServices;

namespace StbImageSharp
{
	public static unsafe partial class StbImage
	{
		[StructLayout(LayoutKind.Sequential)]
		public struct stbi__gif_lzw
		{
			public short prefix;
			public byte first;
			public byte suffix;
		}

		public class stbi__gif
		{
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

			public stbi__gif()
			{
				codes = (stbi__gif_lzw*) CRuntime.malloc(4096 * sizeof(stbi__gif_lzw));
				pal = (byte*) CRuntime.malloc(256 * 4 * sizeof(byte));
				lpal = (byte*) CRuntime.malloc(256 * 4 * sizeof(byte));
			}
		}

		public static void stbi__gif_parse_colortable(stbi__context s, byte* pal, int num_entries, int transp)
		{
			int i;
			for (i = 0; i < num_entries; ++i)
			{
				pal[i * 4 + 2] = Context.Get8();
				pal[i * 4 + 1] = Context.Get8();
				pal[i * 4] = Context.Get8();
				pal[i * 4 + 3] = (byte) (transp == i ? 0 : 255);
			}
		}

		public static Image LoadFromMemory(byte[] bytes, ColorComponents req_comp = STBI_default)
		{
			Image image;
			byte* result = null;
			int x, y, comp;

			try
			{
				fixed (byte* b = bytes)
				{
					result = stbi_load_from_memory(b, bytes.Length, &x, &y, &comp, req_comp);
				}

				if (result == null)
				{
					throw new InvalidOperationException(LastError);
				}

				image = new Image
				{
					Width = x,
					Height = y,
					SourceComp = comp,
					Comp = req_comp == STBI_default ? comp : req_comp
				};

				// Convert to array
				image.Data = new byte[x * y * image.Comp];
				Marshal.Copy(new IntPtr(result), image.Data, 0, image.Data.Length);
			}
			finally
			{
				if (result != null)
				{
					CRuntime.free(result);
				}
			}

			return image;
		}
	}
}