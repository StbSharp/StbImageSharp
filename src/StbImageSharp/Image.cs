using System;
using System.IO;
using System.Runtime.InteropServices;

namespace StbImageSharp
{
	public class Image
	{
		public int Width { get; set; }
		public int Height { get; set; }
		public ColorComponents SourceComp { get; set; }
		public ColorComponents Comp { get; set; }
		public byte[] Data { get; set; }

		internal unsafe static Image FromResult(byte* result, int width, int height, ColorComponents comp, ColorComponents req_comp)
		{
			if (result == null)
			{
				throw new InvalidOperationException(StbImage.LastError);
			}

			var image = new Image
			{
				Width = width,
				Height = height,
				SourceComp = comp,
				Comp = req_comp == ColorComponents.Default ? comp : req_comp
			};

			// Convert to array
			image.Data = new byte[width * height * (int)image.Comp];
			Marshal.Copy(new IntPtr(result), image.Data, 0, image.Data.Length);

			return image;
		}

		public unsafe static Image FromMemory(byte[] bytes, ColorComponents req_comp = ColorComponents.Default)
		{
			byte* result = null;

			try
			{
				int x, y, comp;
				fixed (byte* b = bytes)
				{
					result = StbImage.stbi_load_from_memory(b, bytes.Length, &x, &y, &comp, (int)req_comp);
				}

				return FromResult(result, x, y, (ColorComponents)comp, req_comp);
			}
			finally
			{
				if (result != null)
				{
					CRuntime.free(result);
				}
			}
		}
	}
}