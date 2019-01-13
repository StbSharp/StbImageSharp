using System;

namespace StbImageSharp
{
	public unsafe class PsdDecoder: BaseDecoder
	{
		protected override bool InternalTest()
		{
			return Context.Get32BigEndian() == 0x38425053;
		}

		public int PsdDecodeRle(byte* p, int pixelCount)
		{
			int count;
			int nleft;
			int len;
			count = (int)(0);
			while ((nleft = (int)(pixelCount - count)) > (0))
			{
				len = (int)(Context.Get8());
				if ((len) == (128))
				{
				}
				else if ((len) < (128))
				{
					len++;
					if ((len) > (nleft))
						return 0;
					count += len;
					while ((len) != 0)
					{
						*p = (byte)(Context.Get8());
						p += 4;
						len--;
					}
				}
				else if ((len) > (128))
				{
					byte val;
					len = (int)(257 - len);
					if ((len) > (nleft))
						return (int)(0);
					val = (byte)(Context.Get8());
					count += (int)(len);
					while ((len) != 0)
					{
						*p = (byte)(val);
						p += 4;
						len--;
					}
				}
			}

			return (int)(1);
		}

		protected unsafe byte* InternalLoad(ColorComponents req_comp, ref int x, ref int y, ref ColorComponents sourceComp, ref int bitsPerChannel, int bpc = 8)
		{
			int pixelCount;
			int channelCount;
			int compression;
			int channel;
			int i;
			int bitdepth;
			int w;
			int h;
			byte* _out_;
			if (Context.Get32BigEndian() != 0x38425053)
				throw new Exception("not PSD");
			if (Context.Get16BigEndian() != 1)
				throw new Exception("wrong version");
			Context.Skip((int)(6));
			channelCount = (int)(Context.Get16BigEndian());
			if (((channelCount) < (0)) || ((channelCount) > (16)))
				throw new Exception("wrong channel count");
			h = (int)(Context.Get32BigEndian());
			w = (int)(Context.Get32BigEndian());
			bitdepth = (int)(Context.Get16BigEndian());
			if ((bitdepth != 8) && (bitdepth != 16))
				throw new Exception("unsupported bit depth");
			if (Context.Get16BigEndian() != 3)
				throw new Exception("wrong color format");
			Context.Skip((int)(Context.Get32BigEndian()));
			Context.Skip((int)(Context.Get32BigEndian()));
			Context.Skip((int)(Context.Get32BigEndian()));
			compression = (int)(Context.Get16BigEndian());
			if ((compression) > (1))
				throw new Exception("bad compression");
			if (Utility.Mad3SizesValid((int)(4), (int)(w), (int)(h), (int)(0)) == 0)
				throw new Exception("too large");

			if (((compression == 0) && ((bitdepth) == (16))) && ((bpc) == (16)))
			{
				_out_ = (byte*)(Utility.MallocMad3((int)(8), (int)(w), (int)(h), (int)(0)));
				bitsPerChannel = (int)(16);
			}
			else
				_out_ = (byte*)(CRuntime.malloc((ulong)(4 * w * h)));

			pixelCount = (int)(w * h);
			if ((compression) != 0)
			{
				Context.Skip((int)(h * channelCount * 2));
				for (channel = (int)(0); (channel) < (4); channel++)
				{
					byte* p;
					p = _out_ + channel;
					if ((channel) >= (channelCount))
					{
						for (i = (int)(0); (i) < (pixelCount); i++, p += 4)
						{
							*p = (byte)((channel) == (3) ? 255 : 0);
						}
					}
					else
					{
						if (PsdDecodeRle(p, (int)(pixelCount)) == 0)
						{
							CRuntime.free(_out_);
							throw new Exception("corrupt");
						}
					}
				}
			}
			else
			{
				for (channel = (int)(0); (channel) < (4); channel++)
				{
					if ((channel) >= (channelCount))
					{
						if (((bitdepth) == (16)) && ((bpc) == (16)))
						{
							ushort* q = ((ushort*)(_out_)) + channel;
							ushort val = (ushort)((channel) == (3) ? 65535 : 0);
							for (i = (int)(0); (i) < (pixelCount); i++, q += 4)
							{
								*q = (ushort)(val);
							}
						}
						else
						{
							byte* p = _out_ + channel;
							byte val = (byte)((channel) == (3) ? 255 : 0);
							for (i = (int)(0); (i) < (pixelCount); i++, p += 4)
							{
								*p = (byte)(val);
							}
						}
					}
					else
					{
						if (bitsPerChannel == (16))
						{
							ushort* q = ((ushort*)(_out_)) + channel;
							for (i = (int)(0); (i) < (pixelCount); i++, q += 4)
							{
								*q = ((ushort)(Context.Get16BigEndian()));
							}
						}
						else
						{
							byte* p = _out_ + channel;
							if ((bitdepth) == (16))
							{
								for (i = (int)(0); (i) < (pixelCount); i++, p += 4)
								{
									*p = ((byte)(Context.Get16BigEndian() >> 8));
								}
							}
							else
							{
								for (i = (int)(0); (i) < (pixelCount); i++, p += 4)
								{
									*p = (byte)(Context.Get8());
								}
							}
						}
					}
				}
			}

			if ((channelCount) >= (4))
			{
				if (bitsPerChannel == (16))
				{
					for (i = (int)(0); (i) < (w * h); ++i)
					{
						ushort* pixel = (ushort*)(_out_) + 4 * i;
						if ((pixel[3] != 0) && (pixel[3] != 65535))
						{
							float a = (float)(pixel[3] / 65535.0f);
							float ra = (float)(1.0f / a);
							float inv_a = (float)(65535.0f * (1 - ra));
							pixel[0] = ((ushort)(pixel[0] * ra + inv_a));
							pixel[1] = ((ushort)(pixel[1] * ra + inv_a));
							pixel[2] = ((ushort)(pixel[2] * ra + inv_a));
						}
					}
				}
				else
				{
					for (i = (int)(0); (i) < (w * h); ++i)
					{
						byte* pixel = _out_ + 4 * i;
						if ((pixel[3] != 0) && (pixel[3] != 255))
						{
							float a = (float)(pixel[3] / 255.0f);
							float ra = (float)(1.0f / a);
							float inv_a = (float)(255.0f * (1 - ra));
							pixel[0] = ((byte)(pixel[0] * ra + inv_a));
							pixel[1] = ((byte)(pixel[1] * ra + inv_a));
							pixel[2] = ((byte)(pixel[2] * ra + inv_a));
						}
					}
				}
			}

			if (((req_comp) != ColorComponents.Default) && (req_comp != ColorComponents.RedGreenBlueAlpha))
			{
				if (bitsPerChannel == (16))
					_out_ = (byte *)Utility.ConvertFormat16((ushort*)(_out_), (int)(4), (int)(req_comp), (uint)(w),
						(uint)(h));
				else
					_out_ = Utility.ConvertFormat(_out_, (int)(4), (int)(req_comp), (uint)(w), (uint)(h));
				if ((_out_) == (null))
					return _out_;
			}

			sourceComp = ColorComponents.RedGreenBlueAlpha;
			y = (int)(h);
			x = (int)(w);
			return _out_;
		}

		protected override unsafe byte* InternalLoad(ColorComponents req_comp, ref int x, ref int y, ref ColorComponents sourceComp, ref int bitsPerChannel)
		{
			return InternalLoad(req_comp, ref x, ref y, ref sourceComp, ref bitsPerChannel, 8);
		}

		protected override unsafe bool InternalInfo(ref int x, ref int y, ref ColorComponents comp)
		{
			int channelCount;
			if (Context.Get32BigEndian() != 0x38425053)
			{
				Context.Rewind();
				return false;
			}

			if (Context.Get16BigEndian() != 1)
			{
				Context.Rewind();
				return false;
			}

			Context.Skip((int)(6));
			channelCount = (int)(Context.Get16BigEndian());
			if (((channelCount) < (0)) || ((channelCount) > (16)))
			{
				Context.Rewind();
				return false;
			}

			y = (int)(Context.Get32BigEndian());
			x = (int)(Context.Get32BigEndian());
			if (Context.Get16BigEndian() != 8)
			{
				Context.Rewind();
				return false;
			}

			if (Context.Get16BigEndian() != 3)
			{
				Context.Rewind();
				return false;
			}

			comp = ColorComponents.RedGreenBlueAlpha;
			return true;
		}
	}
}