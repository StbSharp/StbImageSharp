using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace StbImageSharp
{
	internal class AnimatedGifEnumerator: IEnumerator<AnimatedFrameResult>
	{
		private readonly StbImage.stbi__context _context;
		private StbImage.stbi__gif _gif;
		private AnimatedFrameResult _current;

		public ColorComponents ColorComponents { get; private set; }

		public AnimatedFrameResult Current => _current;

		object IEnumerator.Current => _current;

		public AnimatedGifEnumerator(Stream input, ColorComponents colorComponents)
		{
			if (input == null)
			{
				throw new ArgumentNullException(nameof(input));
			}

			_context = new StbImage.stbi__context(input);

			if (StbImage.stbi__gif_test(_context) == 0)
			{
				throw new Exception("Input stream is not GIF file.");
			}

			_gif = new StbImage.stbi__gif();
			ColorComponents = colorComponents;
		}

		~AnimatedGifEnumerator()
		{
			Dispose(false);
		}

		protected unsafe virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (_gif != null)
				{
					_gif.Dispose();
					_gif = null;
				}
			}
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		public unsafe bool MoveNext()
		{
			// Read next frame
			int ccomp;
			byte two_back;
			var result = StbImage.stbi__gif_load_next(_context, _gif, &ccomp, (int)ColorComponents, &two_back);
			if (result == null)
			{
				return false;
			}

			if (_current == null)
			{
				_current = new AnimatedFrameResult
				{
					Width = _gif.w,
					Height = _gif.h,
					SourceComp = (ColorComponents)ccomp,
					Comp = ColorComponents == ColorComponents.Default ? (ColorComponents)ccomp : ColorComponents
				};

				_current.Data = new byte[_current.Width * _current.Height * (int)_current.Comp];
			}

			_current.DelayInMs = _gif.delay;

			Marshal.Copy(new IntPtr(result), _current.Data, 0, _current.Data.Length);

			return true;
		}

		public void Reset()
		{
			throw new NotImplementedException();
		}
	}

	internal class AnimatedGifEnumerable : IEnumerable<AnimatedFrameResult>
	{
		private readonly Stream _input;

		public ColorComponents ColorComponents { get; private set; }

		public AnimatedGifEnumerable(Stream input, ColorComponents colorComponents)
		{
			_input = input;
			ColorComponents = colorComponents;
		}

		public IEnumerator<AnimatedFrameResult> GetEnumerator()
		{
			return new AnimatedGifEnumerator(_input, ColorComponents);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
