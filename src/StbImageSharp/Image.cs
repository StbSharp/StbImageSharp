using System;
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

		public unsafe static Image FromMemory(byte[] bytes, int req_comp = StbImage.STBI_default)
		{
			Image image;
			byte* result = null;
			int x, y, comp;

			try
			{
				fixed (byte* b = bytes)
				{
					result = StbImage.stbi_load_from_memory(b, bytes.Length, &x, &y, &comp, req_comp);
				}

				if (result == null)
				{
					throw new InvalidOperationException(StbImage.LastError);
				}

				image = new Image
				{
					Width = x,
					Height = y,
					SourceComp = (ColorComponents)comp,
					Comp = (ColorComponents)(req_comp == StbImage.STBI_default ? comp : req_comp)
				};

				// Convert to array
				image.Data = new byte[x * y * (int)image.Comp];
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