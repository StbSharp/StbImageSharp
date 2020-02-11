using StbNative;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace StbImageSharp.Testing
{
	internal static class Program
	{
		private static int tasksStarted;
		private static int filesProcessed, filesMatches;
		private static int stbSharpLoadingFromMemory;
		private static int stbNativeLoadingFromMemory;

		private delegate void WriteDelegate(ImageResult image, Stream stream);

		private const int LoadTries = 10;

		private static readonly int[] JpgQualities = { 1, 4, 8, 16, 25, 32, 50, 64, 72, 80, 90, 100 };

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

		private delegate byte[] LoadDelegate(out int x, out int y, out ColorComponents comp);

		private static void ParseTest(Stopwatch sw,
			LoadDelegate load1, LoadDelegate load2)
		{
			Log("With StbSharp");
			int x = 0, y = 0;
			var comp = ColorComponents.Grey;
			byte[] parsed = new byte[0];
			BeginWatch(sw);

			for (var i = 0; i < LoadTries; ++i)
			{
				parsed = load1(out x, out y, out comp);
			}

			Log("x: {0}, y: {1}, comp: {2}, size: {3}", x, y, comp, parsed.Length);
			var load1Passed = EndWatch(sw) / LoadTries;
			Log("Span: {0} ms", load1Passed);

			Log("With Stb.Native");
			int x2 = 0, y2 = 0;
			var comp2 = ColorComponents.Grey;
			byte[] parsed2 = new byte[0];

			BeginWatch(sw);
			for (var i = 0; i < LoadTries; ++i)
			{
				parsed2 = load2(out x2, out y2, out comp2);
			}
			Log("x: {0}, y: {1}, comp: {2}, size: {3}", x2, y2, comp2, parsed2.Length);
			var load2Passed = EndWatch(sw) / LoadTries;
			Log("Span: {0} ms", load2Passed);

			stbSharpLoadingFromMemory += load1Passed;
			stbNativeLoadingFromMemory += load2Passed;

			if (x != x2)
			{
				throw new Exception(string.Format("Inconsistent x: StbSharp={0}, Stb.Native={1}", x, x2));
			}

			if (y != y2)
			{
				throw new Exception(string.Format("Inconsistent y: StbSharp={0}, Stb.Native={1}", y, y2));
			}

			if (comp != comp2)
			{
				throw new Exception(string.Format("Inconsistent comp: StbSharp={0}, Stb.Native={1}", comp, comp2));
			}

			if (parsed.Length != parsed2.Length)
			{
				throw new Exception(string.Format("Inconsistent parsed length: StbSharp={0}, Stb.Native={1}", parsed.Length,
					parsed2.Length));
			}

			for (var i = 0; i < parsed.Length; ++i)
			{
				if (parsed[i] != parsed2[i])
				{
					throw new Exception(string.Format("Inconsistent data: index={0}, StbSharp={1}, Stb.Native={2}",
						i,
						(int)parsed[i],
						(int)parsed2[i]));
				}
			}
		}

		public static bool RunTests(string imagesPath)
		{
			var files = Directory.EnumerateFiles(imagesPath, "*.*", SearchOption.AllDirectories).ToArray();

			Log("Files count: {0}", files.Length);

			foreach (var file in files)
			{
				Task.Factory.StartNew(() => { ThreadProc(file); });
				tasksStarted++;
			}

			while (true)
			{
				Thread.Sleep(1000);

				if (tasksStarted == 0)
				{
					break;
				}
			}

			return true;
		}

		private static void ThreadProc(string f)
		{

			if (!f.EndsWith(".bmp") && !f.EndsWith(".jpg") && !f.EndsWith(".png") &&
				!f.EndsWith(".jpg") && !f.EndsWith(".psd") && !f.EndsWith(".pic") &&
				!f.EndsWith(".tga"))
			{
				--tasksStarted;
				return;
			}

			try
			{
				var sw = new Stopwatch();

				Log(string.Empty);
				Log("{0}: Loading {1} into memory", DateTime.Now.ToLongTimeString(), f);
				var data = File.ReadAllBytes(f);
				Log("----------------------------");

				Log("Loading from memory");
				int x = 0, y = 0;
				var comp = ColorComponents.Grey;
				byte[] parsed = new byte[0];
				ParseTest(
					sw,
					(out int xx, out int yy, out ColorComponents ccomp) =>
					{
							var img = ImageResult.FromMemory(data, ColorComponents.RedGreenBlueAlpha);

							parsed = img.Data;
							xx = img.Width;
							yy = img.Height;
							ccomp = img.SourceComp;

							x = xx;
							y = yy;
							comp = ccomp;
							return parsed;
					},
					(out int xx, out int yy, out ColorComponents ccomp) =>
					{
						var result = Native.load_from_memory(data, out xx, out yy, out int icomp, (int)ColorComponents.RedGreenBlueAlpha);
						ccomp = (ColorComponents)icomp;
						return result;
					});

				++filesMatches;
			}
			catch (Exception ex)
			{
				Log("Error: " + ex.Message);
			}
			finally
			{
				++filesProcessed;
				--tasksStarted;

				Log("Total StbSharp Loading From memory Time: {0} ms", stbSharpLoadingFromMemory);
				Log("Total Stb.Native Loading From memory Time: {0} ms", stbNativeLoadingFromMemory);
				Log("Files matches/processed: {0}/{1}", filesMatches, filesProcessed);
				Log("Tasks left: {0}", tasksStarted);

				Log("GC Memory: {0}", GC.GetTotalMemory(true));
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
	}
}