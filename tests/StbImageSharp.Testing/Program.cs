using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using StbNative;

namespace StbImageSharp.Testing
{
	internal static class Program
	{
		private class LoadResult
		{
			public int Width;
			public int Height;
			public ColorComponents Components;
			public byte[] Data;
			public int TimeInMs;
		}

		private class LoadingTimes
		{
			private readonly ConcurrentDictionary<string, int> _byExtension = new ConcurrentDictionary<string, int>();
			private readonly ConcurrentDictionary<string, int> _byExtensionCount = new ConcurrentDictionary<string, int>();
			private int _total, _totalCount;

			public void Add(string extension, int value)
			{
				if (!_byExtension.ContainsKey(extension))
				{
					_byExtension[extension] = 0;
					_byExtensionCount[extension] = 0;
				}

				_byExtension[extension] += value;
				++_byExtensionCount[extension];
				_total += value;
				++_totalCount;
			}

			public string BuildString()
			{
				var sb = new StringBuilder();
				foreach (var pair in _byExtension)
				{
					sb.AppendFormat("{0}: {1} ms, ", pair.Key, pair.Value);
				}

				sb.AppendFormat("Total: {0} ms", _total);

				return sb.ToString();
			}

			public string BuildStringCount()
			{
				var sb = new StringBuilder();
				foreach (var pair in _byExtensionCount)
				{
					sb.AppendFormat("{0}: {1}, ", pair.Key, pair.Value);
				}

				sb.AppendFormat("Total: {0}", _totalCount);

				return sb.ToString();
			}
		}

		private const int LoadTries = 10;
		private static int tasksStarted;
		private static int filesProcessed, filesMatches;
		private static LoadingTimes stbImageSharpTotal = new LoadingTimes();
		private static LoadingTimes stbNativeTotal = new LoadingTimes();
		private static LoadingTimes imageSharpTotal = new LoadingTimes();

		public static void Log(string message)
		{
			Console.WriteLine(Thread.CurrentThread.ManagedThreadId + " -- " + message);
		}

		public static void Log(string format, params object[] args)
		{
			Log(string.Format(format, args));
		}

		private static void BeginWatch(Stopwatch sw)
		{
			sw.Restart();
		}

		private static int EndWatch(Stopwatch sw)
		{
			sw.Stop();
			return (int)sw.ElapsedMilliseconds;
		}

		private static LoadResult ParseTest(string name, LoadDelegate load)
		{
			var sw = new Stopwatch();

			Log("With " + name);
			int x = 0, y = 0;
			var comp = ColorComponents.Grey;
			var parsed = new byte[0];
			BeginWatch(sw);

			for (var i = 0; i < LoadTries; ++i)
				parsed = load(out x, out y, out comp);

			Log("x: {0}, y: {1}, comp: {2}, size: {3}", x, y, comp, parsed.Length);
			var passed = EndWatch(sw) / LoadTries;
			Log("Span: {0} ms", passed);

			return new LoadResult
			{
				Width = x,
				Height = y,
				Components = comp,
				Data = parsed,
				TimeInMs = passed
			};
		}

		public static bool RunTests(string imagesPath)
		{
			var files = Directory.EnumerateFiles(imagesPath, "*.*", SearchOption.AllDirectories).ToArray();

			Log("Files count: {0}", files.Length);

			foreach (var file in files)
			{
				Task.Factory.StartNew(() => { ThreadProc(file); });
				Interlocked.Increment(ref tasksStarted);
			}

			while (true)
			{
				Thread.Sleep(1000);

				if (tasksStarted == 0)
					break;
			}

			return true;
		}

		private static void ThreadProc(string f)
		{
			if (!f.EndsWith(".bmp") && !f.EndsWith(".jpg") && !f.EndsWith(".png") &&
				!f.EndsWith(".jpg") && !f.EndsWith(".psd") && !f.EndsWith(".pic") &&
				!f.EndsWith(".tga") && !f.EndsWith(".hdr"))
			{
				Interlocked.Decrement(ref tasksStarted);
				return;
			}

			bool match = false;
			try
			{
				Log(string.Empty);
				Log("{0}: Loading {1} into memory", DateTime.Now.ToLongTimeString(), f);
				var data = File.ReadAllBytes(f);
				var extension = Path.GetExtension(f).ToLower();
				if (extension.StartsWith("."))
				{
					extension = extension.Substring(1);
				}

				Log("----------------------------");

				var stbImageSharpResult = ParseTest(
					"StbImageSharp",
					(out int x, out int y, out ColorComponents ccomp) =>
					{
						var img = ImageResult.FromMemory(data, ColorComponents.RedGreenBlueAlpha);

						x = img.Width;
						y = img.Height;
						ccomp = img.SourceComp;

						return img.Data;
					});

				var stbNativeResult = ParseTest(
					"Stb.Native",
					(out int x, out int y, out ColorComponents ccomp) =>
					{
						var result = Native.load_from_memory(data, out x, out y, out var icomp,
							(int)ColorComponents.RedGreenBlueAlpha);
						ccomp = (ColorComponents)icomp;
						return result;
					});


				if (stbImageSharpResult.Width != stbNativeResult.Width)
					throw new Exception(string.Format("Inconsistent x: StbSharp={0}, Stb.Native={1}", stbImageSharpResult.Width, stbNativeResult.Width));

				if (stbImageSharpResult.Height != stbNativeResult.Height)
					throw new Exception(string.Format("Inconsistent y: StbSharp={0}, Stb.Native={1}", stbImageSharpResult.Height, stbNativeResult.Height));

				if (stbImageSharpResult.Components != stbNativeResult.Components)
					throw new Exception(string.Format("Inconsistent comp: StbSharp={0}, Stb.Native={1}", stbImageSharpResult.Components, stbNativeResult.Components));

				if (stbImageSharpResult.Data.Length != stbNativeResult.Data.Length)
					throw new Exception(string.Format("Inconsistent parsed length: StbSharp={0}, Stb.Native={1}",
						stbImageSharpResult.Data.Length,
						stbNativeResult.Data.Length));

				for (var i = 0; i < stbImageSharpResult.Data.Length; ++i)
					if (stbImageSharpResult.Data[i] != stbNativeResult.Data[i])
						throw new Exception(string.Format("Inconsistent data: index={0}, StbSharp={1}, Stb.Native={2}",
							i,
							(int)stbImageSharpResult.Data[i],
							(int)stbNativeResult.Data[i]));

				match = true;

				if (extension != "tga" && extension != "psd" && extension != "pic" && extension != "hdr")
				{
					var imageSharpResult = ParseTest(
						"ImageSharp",
						(out int x, out int y, out ColorComponents ccomp) =>
						{
							using (Image<Rgba32> image = Image.Load(data))
							{
								x = image.Width;
								y = image.Height;
								ccomp = ColorComponents.Default;

								Span<Rgba32> span;
								image.TryGetSinglePixelSpan(out span);

								return MemoryMarshal.AsBytes(span).ToArray();
							}
						}
					);
					imageSharpTotal.Add(extension, imageSharpResult.TimeInMs);
				}

				stbImageSharpTotal.Add(extension, stbImageSharpResult.TimeInMs);
				stbNativeTotal.Add(extension, stbNativeResult.TimeInMs);
			}
			catch (Exception ex)
			{
				Log("Error: " + ex.Message);
			}
			finally
			{
				if (match)
				{
					Interlocked.Increment(ref filesMatches);
				}

				Interlocked.Increment(ref filesProcessed);
				Interlocked.Decrement(ref tasksStarted);

				Log("StbImageSharp - {0}", stbImageSharpTotal.BuildString());
				Log("Stb.Native - {0}", stbNativeTotal.BuildString());
				Log("ImageSharp - {0}", imageSharpTotal.BuildString());
				Log("Total files processed - {0}", stbImageSharpTotal.BuildStringCount());
				Log("StbImageSharp/Stb.Native matches/processed - {0}/{1}", filesMatches, filesProcessed);
				Log("Tasks left - {0}", tasksStarted);
				Log("GC Memory - {0}", GC.GetTotalMemory(true));
				Log("Native Memory Allocations - {0}", MemoryStats.Allocations);
			}
		}

		public static int Main(string[] args)
		{
			try
			{
				if (args == null || args.Length < 1)
				{
					Console.WriteLine("Usage: StbImageSharp.Testing <path_to_folder_with_images>");
					return 1;
				}

				var start = DateTime.Now;

				var res = RunTests(args[0]);
				var passed = DateTime.Now - start;
				Log("Span: {0} ms", passed.TotalMilliseconds);
				Log(DateTime.Now.ToLongTimeString() + " -- " + (res ? "Success" : "Failure"));

				return res ? 1 : 0;
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
				return 0;
			}
		}

		private delegate void WriteDelegate(ImageResult image, Stream stream);

		private delegate byte[] LoadDelegate(out int x, out int y, out ColorComponents comp);
	}
}