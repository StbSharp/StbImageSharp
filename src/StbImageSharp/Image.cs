using System;
using System.IO;

namespace StbImageSharp
{
	public class Image
	{
		public int Width { get; set; }
		public int Height { get; set; }
		public ColorComponents SourceComp { get; set; }
		public ColorComponents Comp { get; set; }
		public byte[] Data { get; set; }

		private static Image TryLoadFromStream<DecoderType>(Stream stream, ColorComponents comp) where DecoderType : BaseDecoder, new()
		{
			var decoder = new DecoderType();

			if (!decoder.Test(stream))
			{
				return null;
			}

			return decoder.Load(stream, comp);
		}

		/// <summary>
		/// Tries to load image from stream containing JPG
		/// Returns null, if it's not JPG
		/// </summary>
		/// <param name="stream"></param>
		/// <param name="comp"></param>
		/// <returns></returns>
		public static Image FromJpgStream(Stream stream, ColorComponents comp = ColorComponents.Default)
		{
			return TryLoadFromStream<JpegDecoder>(stream, comp);
		}

		/// <summary>
		/// Tries to load image from stream containing PNG
		/// Returns null, if it's not PNG
		/// </summary>
		/// <param name="stream"></param>
		/// <param name="comp"></param>
		/// <returns></returns>
		public static Image FromPngStream(Stream stream, ColorComponents comp = ColorComponents.Default)
		{
			return TryLoadFromStream<PngDecoder>(stream, comp);
		}

		/// <summary>
		/// Tries to load image from stream containing BMP
		/// Returns null, if it's not BMP
		/// </summary>
		/// <param name="stream"></param>
		/// <param name="comp"></param>
		/// <returns></returns>
		public static Image FromBmpStream(Stream stream, ColorComponents comp = ColorComponents.Default)
		{
			return TryLoadFromStream<BmpDecoder>(stream, comp);
		}

		/// <summary>
		/// Tries to load image from stream containing GIF
		/// Returns null, if it's not GIF
		/// </summary>
		/// <param name="stream"></param>
		/// <param name="comp"></param>
		/// <returns></returns>
		public static Image FromGifStream(Stream stream, ColorComponents comp = ColorComponents.Default)
		{
			return TryLoadFromStream<GifDecoder>(stream, comp);
		}

		/// <summary>
		/// Tries to load image from stream containing PSD
		/// Returns null, if it's not PSD
		/// </summary>
		/// <param name="stream"></param>
		/// <param name="comp"></param>
		/// <returns></returns>
		public static Image FromPsdStream(Stream stream, ColorComponents comp = ColorComponents.Default)
		{
			return TryLoadFromStream<PsdDecoder>(stream, comp);
		}

		/// <summary>
		/// Tries to load image from stream containing TGA
		/// Returns null, if it's not TGA
		/// </summary>
		/// <param name="stream"></param>
		/// <param name="comp"></param>
		/// <returns></returns>
		public static Image FromTgaStream(Stream stream, ColorComponents comp = ColorComponents.Default)
		{
			return TryLoadFromStream<TgaDecoder>(stream, comp);
		}

		/// <summary>
		/// Tries to load image from  a stream
		/// Throws exception if it's type isn't supported
		/// </summary>
		/// <param name="stream"></param>
		/// <param name="comp"></param>
		/// <returns></returns>
		public static Image FromStream(Stream stream, ColorComponents comp = ColorComponents.Default)
		{
			var result = FromJpgStream(stream, comp);
			if (result != null)
			{
				return result;
			}

			result = FromPngStream(stream, comp);
			if (result != null)
			{
				return result;
			}

			result = FromBmpStream(stream, comp);
			if (result != null)
			{
				return result;
			}

			result = FromGifStream(stream, comp);
			if (result != null)
			{
				return result;
			}

			result = FromPsdStream(stream, comp);
			if (result != null)
			{
				return result;
			}

			result = FromTgaStream(stream, comp);
			if (result != null)
			{
				return result;
			}

			throw new Exception("Image type isn't supported.");
		}
	}
}