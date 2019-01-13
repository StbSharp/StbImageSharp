using System.Threading;

namespace StbImageSharp
{
	public unsafe static class Memory
	{
		private static int _allocations;

		public static int Allocations
		{
			get
			{
				return _allocations;
			}
		}

		internal static void Allocated()
		{
			Interlocked.Increment(ref _allocations);
		}

		internal static void Freed()
		{
			Interlocked.Decrement(ref _allocations);
		}
	}
}
