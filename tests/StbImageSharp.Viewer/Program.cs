using System;

namespace StbImageSharp.Samples.MonoGame
{
	/// <summary>
	/// The main class.
	/// </summary>
	public static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
			if (args.Length == 0)
			{
				Console.WriteLine("Usage: StbImageSharp.Viewer <path_to_image_file> [-animated-gif]");
				return;
			}

			try
			{
				var isAnimatedGif = false;
				if (args.Length > 1 && args[1] == "-animated-gif")
				{
					isAnimatedGif = true;
				}

				using (var game = new ViewerGame(args[0], isAnimatedGif))
					game.Run();
			}
			catch(Exception ex)
			{
				Console.WriteLine(ex);
			}
		}
	}
}