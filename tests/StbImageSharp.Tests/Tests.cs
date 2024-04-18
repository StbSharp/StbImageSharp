using NUnit.Framework;
using StbImageSharp.Tests.Utility;
using System;
using System.IO;
using System.Reflection;

namespace StbImageSharp.Tests
{
	[TestFixture]
	public class Tests
	{
		private static readonly Assembly _assembly = typeof(Tests).Assembly;

		[TestCase("The Public Domain_ Enclosing the Commons of the Mind.pdf")]
		[TestCase("empty")]
		public void LoadUnknownFormat(string filename)
		{
			Assert.Throws<InvalidOperationException>(() =>
			{
				ImageResult result = null;
				using (var stream = _assembly.OpenResourceStream(filename))
				{
					result = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
				}
			});
		}

		[TestCase("IDockable.png", 715, 426, ColorComponents.RedGreenBlueAlpha)]
		[TestCase("sample_1280×853.hdr", 1280, 853, ColorComponents.RedGreenBlue)]
		[TestCase("DockPanes.jpg", 609, 406, ColorComponents.RedGreenBlue)]
		public void Load(string filename, int width, int height, ColorComponents colorComponents)
		{
			ImageResult result = null;
			using (var stream = _assembly.OpenResourceStream(filename))
			{
				result = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
			}

			Assert.IsNotNull(result);
			Assert.AreEqual(width, result.Width);
			Assert.AreEqual(height, result.Height);
			Assert.AreEqual(ColorComponents.RedGreenBlueAlpha, result.Comp);
			Assert.AreEqual(colorComponents, result.SourceComp);
			Assert.IsNotNull(result.Data);
			Assert.AreEqual(result.Width * result.Height * 4, result.Data.Length);
		}

		[TestCase("sample_1280×853.hdr", 1280, 853, ColorComponents.RedGreenBlue)]
		public void LoadHdr(string filename, int width, int height, ColorComponents colorComponents)
		{
			ImageResultFloat result = null;
			using(var stream = _assembly.OpenResourceStream(filename))
			{
				result = ImageResultFloat.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
			}

			Assert.IsNotNull(result);
			Assert.AreEqual(width, result.Width);
			Assert.AreEqual(height, result.Height);
			Assert.AreEqual(ColorComponents.RedGreenBlueAlpha, result.Comp);
			Assert.AreEqual(colorComponents, result.SourceComp);
			Assert.IsNotNull(result.Data);
			Assert.AreEqual(result.Width * result.Height * 4, result.Data.Length);
		}

		[TestCase("sample_1280×853.hdr", 2000, 1280, 853, ColorComponents.RedGreenBlue, false)]
		[TestCase("DockPanes.jpg", 2000, 609, 406, ColorComponents.RedGreenBlue, false)]
		public void Info(string filename, int headerSize, int width, int height, ColorComponents colorComponents, bool is16bit)
		{
			ImageInfo? result;

			var data = new byte[headerSize];
			using (var stream = _assembly.OpenResourceStream(filename))
			{
				stream.Read(data, 0, data.Length);
			}

			using (var stream = new MemoryStream(data))
			{
				result = ImageInfo.FromStream(stream);
			}

			Assert.IsNotNull(result);

			var info = result.Value;
			Assert.AreEqual(width, info.Width);
			Assert.AreEqual(height, info.Height);
			Assert.AreEqual(colorComponents, info.ColorComponents);
			Assert.AreEqual(is16bit ? 16 : 8, info.BitsPerChannel);
		}

		[TestCase("somersault.gif", 384, 480, ColorComponents.RedGreenBlueAlpha, 43)]
		public void AnimatedGifFrames(string fileName, int width, int height, ColorComponents colorComponents, int originalFrameCount)
		{
			using (var stream = _assembly.OpenResourceStream(fileName))
			{
				var frameCount = 0;
				foreach(var frame in ImageResult.AnimatedGifFramesFromStream(stream))
				{
					Assert.AreEqual(width, frame.Width);
					Assert.AreEqual(height, frame.Height);
					Assert.AreEqual(colorComponents, frame.Comp);
					Assert.IsNotNull(frame.Data);
					Assert.AreEqual(frame.Width * frame.Height * (int)frame.Comp, frame.Data.Length);

					++frameCount;
				}

				Assert.AreEqual(frameCount, originalFrameCount);

				stream.Seek(0, SeekOrigin.Begin);
			}

			Assert.AreEqual(0, StbImage.NativeAllocations);
		}
	}
}
