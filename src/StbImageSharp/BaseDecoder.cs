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

		protected abstract byte *InternalLoad(ColorComponents comp, ref int width, ref int height, ref ColorComponents sourceComponents, ref int bitsPerChannel);
		protected abstract bool InternalTest();
		protected abstract bool InternalInfo(ref int width, ref int height, ref ColorComponents sourceComp);

		public Image Load(Stream stream, ColorComponents requiredComponents)
		{
			Context = new DecodingContext(stream);

			Image image;
			unsafe
			{
				byte* result = null;
				int x = 0, y = 0;
				ColorComponents sourceComponents = ColorComponents.Default;
				int bitsPerChannel = 8;
				try
				{
					result = InternalLoad(requiredComponents, ref x, ref y, ref sourceComponents, ref bitsPerChannel);

					if (bitsPerChannel != 8)
					{
						result = Utility.Convert16to8((ushort*)(result), x, y,
							requiredComponents == ColorComponents.Default ? (int)sourceComponents : (int)requiredComponents);
					}

					image = new Image
					{
						Width = x,
						Height = y,
						SourceComp = sourceComponents,
						Comp = requiredComponents == ColorComponents.Default ? sourceComponents : requiredComponents
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
			Context = new DecodingContext(stream);

			var result = InternalTest();

			// Rewind
			Context.Rewind();

			return result;
		}

		public bool Info(Stream stream, out int width, out int height, out ColorComponents sourceComponents)
		{
			Context = new DecodingContext(stream);

			width = 0;
			height = 0;
			sourceComponents = ColorComponents.Default;

			return InternalInfo(ref width, ref height, ref sourceComponents);
		}
	}
}
