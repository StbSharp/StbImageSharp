using System;
using System.IO;
using System.Runtime.InteropServices;

namespace StbImageSharp
{
	public abstract unsafe class BaseDecoder
	{
		public enum ScanType
		{
			Load,
			Type,
			Header
		}

		internal DecodingContext Context;

		protected abstract byte *InternalLoad(ColorComponents comp, int *width, int *height, ColorComponents *sourceComp);
		protected abstract bool InternalTest();
		protected abstract bool InternalInfo(int* width, int* height, ColorComponents* sourceComp);

		public Image Load(Stream stream, ColorComponents comp)
		{
			var Context = new DecodingContext(stream);

			Image image;
			unsafe
			{
				byte* result = null;
				int x, y;
				ColorComponents sourceComp;

				try
				{
					InternalLoad(comp, &x, &y, &sourceComp);

					image = new Image
					{
						Width = x,
						Height = y,
						SourceComp = sourceComp,
						Comp = comp == ColorComponents.Default ? sourceComp : comp
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

		public bool Test(Stream stream)
		{
			var Context = new DecodingContext(stream);

			var result = InternalTest();

			// Rewind
			Context.Rewind();

			return result;
		}

		public bool Info(Stream stream, out int width, out int height, out ColorComponents sourceComp)
		{
			var Context = new DecodingContext(stream);

			unsafe
			{
				fixed(int *w = &width)
				{
					fixed(int *h = &height)
					{
						fixed(ColorComponents *comp = &sourceComp)
						{
							return InternalInfo(w, h, comp);
						}
					}
				}
			}
		}
	}
}
