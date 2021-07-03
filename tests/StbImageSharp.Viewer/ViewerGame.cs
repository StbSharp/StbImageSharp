using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace StbImageSharp.Samples.MonoGame
{
	/// <summary>
	/// This is the main type for your game.
	/// </summary>
	public class ViewerGame : Game
	{
		private class FrameInfo
		{
			public Texture2D Texture;
			public int DelayInMs;
		}

		private GraphicsDeviceManager _graphics;
		private SpriteBatch _spriteBatch;
		private readonly string _filePath;
		private readonly bool _isAnimatedGif;
		private readonly List<FrameInfo> _frames = new List<FrameInfo>();
		private int _totalDelayInMs;
		private DateTime? _started;

		public ViewerGame(string filePath, bool isAnimatedGif)
		{
			if (string.IsNullOrEmpty(filePath))
			{
				throw new ArgumentNullException(nameof(filePath));
			}

			_filePath = filePath;
			_isAnimatedGif = isAnimatedGif;

			_graphics = new GraphicsDeviceManager(this)
			{
				PreferredBackBufferWidth = 1400,
				PreferredBackBufferHeight = 960
			};

			Content.RootDirectory = "Content";
			IsMouseVisible = true;
			Window.AllowUserResizing = true;
		}

		/// <summary>
		/// LoadContent will be called once per game and is the place to load
		/// all of your content.
		/// </summary>
		protected override void LoadContent()
		{
			// Create a new SpriteBatch, which can be used to draw textures.
			_spriteBatch = new SpriteBatch(GraphicsDevice);

			// Load image data into memory
			using (var stream = File.OpenRead(_filePath))
			{
				if (!_isAnimatedGif)
				{
					var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
					var texture = new Texture2D(GraphicsDevice, image.Width, image.Height, false, SurfaceFormat.Color);
					texture.SetData(image.Data);

					var frame = new FrameInfo
					{
						Texture = texture,
						DelayInMs = 0
					};

					_frames.Add(frame);
				}
				else
				{
					_totalDelayInMs = 0;
					foreach(var image in ImageResult.AnimatedGifFramesFromStream(stream))
					{
						var texture = new Texture2D(GraphicsDevice, image.Width, image.Height, false, SurfaceFormat.Color);
						texture.SetData(image.Data);

						var frame = new FrameInfo
						{
							Texture = texture,
							DelayInMs = image.DelayInMs
						};

						_totalDelayInMs += frame.DelayInMs;

						_frames.Add(frame);
					}
				}
			}
		}

		/// <summary>
		/// This is called when the game should draw itself.
		/// </summary>
		/// <param name="gameTime">Provides a snapshot of timing values.</param>
		protected override void Draw(GameTime gameTime)
		{
			GraphicsDevice.Clear(Color.CornflowerBlue);

			// TODO: Add your drawing code here
			_spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

			FrameInfo frame = null;
			if (_started == null || _frames.Count == 1)
			{
				frame = _frames[0];
				_started = DateTime.Now;
			}
			else
			{
				var passed = (int)(DateTime.Now - _started.Value).TotalMilliseconds;

				passed %= _totalDelayInMs;
				for(var i = 0; i < _frames.Count; ++i)
				{
					if (passed < 0)
					{
						break;
					}

					frame = _frames[i];
					passed -= frame.DelayInMs;
				}

			} 
			_spriteBatch.Draw(frame.Texture, Vector2.Zero, Color.White);

			_spriteBatch.End();

			base.Draw(gameTime);
		}
	}
}