// Stb.Native.h

#pragma once

using namespace System;
using namespace System::IO;
using namespace System::Collections::Generic;
using namespace System::Runtime::InteropServices;
using namespace System::Threading;

#include <stdio.h>
#include <vector>
#include <functional>

#define STBI_NO_STDIO
#define STB_IMAGE_IMPLEMENTATION
#include "../../Generation/StbSharp.StbImage.Generator/stb_image.h"

#define STBI_WRITE_NO_STDIO
#define STB_IMAGE_WRITE_IMPLEMENTATION
#include "../../Generation/StbSharp.StbImageWrite.Generator/stb_image_write.h"

#define STB_DXT_IMPLEMENTATION
#include "../../Generation/StbSharp.StbDxt.Generator/stb_dxt.h"

#define STB_TRUETYPE_IMPLEMENTATION
#include "../../Generation/StbSharp.StbTrueType.Generator/stb_truetype.h"

namespace StbNative {
	int read_callback(void *user, char *data, int size);
	void skip_callback(void *user, int size);
	int eof_callback(void *user);
	void write_func(void *context, void *data, int size);

	public ref class ReadInfo
	{
	public:
		Stream ^stream;
		array<unsigned char> ^buffer;

		ReadInfo(Stream ^s, array<unsigned char> ^b)
		{
			stream = s;
			buffer = b;
		}
	};

	public ref class Native
	{
	public:
		static Dictionary<int, ReadInfo ^> ^readInfo = gcnew Dictionary<int, ReadInfo ^>();
		static Dictionary<int, Stream ^> ^writeInfo = gcnew Dictionary<int, Stream ^>();
		static int _id = 0;

		static int GenerateId()
		{
			int %trackRefCounter = _id;
			return System::Threading::Interlocked::Increment(trackRefCounter);
		}

		// TODO: Add your methods for this class here.
		static array<unsigned char> ^ load_from_memory(array<unsigned char> ^bytes, [Out] int %x, [Out] int %y, [Out] int %comp, int req_comp)
		{
			pin_ptr<unsigned char> p = &bytes[0];

			int xx, yy, ccomp;
			const unsigned char *ptr = (const unsigned char *)p;
			void *res = stbi_load_from_memory(ptr, bytes->Length, &xx, &yy, &ccomp, req_comp);

			x = xx;
			y = yy;
			comp = ccomp;

			int c = req_comp != 0 ? req_comp : comp;
			array<unsigned char> ^result = gcnew array<unsigned char>(x * y * c);

			Marshal::Copy(IntPtr((void *)res), result, 0, result->Length);
			free(res);

			return result;
		}

		static array<unsigned char> ^ load_from_stream(Stream ^input, [Out] int %x, [Out] int %y, [Out] int %comp, int req_comp)
		{
			array<unsigned char> ^buffer = gcnew array<unsigned char>(32768);

			Monitor::Enter(readInfo);
			int id;
			try {
				id = GenerateId();

				ReadInfo ^newInfo = gcnew ReadInfo(input, buffer);
				readInfo->Add(id, newInfo);
			}
			finally
			{
				Monitor::Exit(readInfo);
			}

			stbi_io_callbacks callbacks;
			callbacks.read = read_callback;
			callbacks.skip = skip_callback;
			callbacks.eof = eof_callback;

			int xx, yy, ccomp;

			void *res = stbi_load_from_callbacks(&callbacks, (void *)id, &xx, &yy, &ccomp, req_comp);

			x = xx;
			y = yy;
			comp = ccomp;

			int c = req_comp != 0 ? req_comp : comp;
			array<unsigned char> ^result = gcnew array<unsigned char>(x * y * c);

			Marshal::Copy(IntPtr((void *)res), result, 0, result->Length);
			free(res);

			buffer = nullptr;

			Monitor::Enter(readInfo);
			try {
				readInfo->Remove(id);
			}
			finally
			{
				Monitor::Exit(readInfo);
			}

			return result;
		}

		// TODO: Add your methods for this class here.
		static void save_to_stream(array<unsigned char> ^bytes, int x, int y, int comp, int type, Stream ^output)
		{
			Monitor::Enter(writeInfo);
			int id;
			try {
				id = GenerateId();

				writeInfo->Add(id, output);
			}
			finally
			{
				Monitor::Exit(writeInfo);
			}

			pin_ptr<unsigned char> p = &bytes[0];
			unsigned char *ptr = (unsigned char *)p;

			std::vector<float> ff;
			switch (type)
			{
				case 0:
					stbi_write_bmp_to_func(write_func, (void *)id, x, y, comp, ptr);
					break;
				case 1:
					stbi_write_tga_to_func(write_func, (void *)id, x, y, comp, ptr);
					break;
				case 2:
				{
					ff.resize(bytes->Length);
					for (int i = 0; i < bytes->Length; ++i)
					{
						ff[i] = (float)(bytes[i] / 255.0f);
					}

					stbi_write_hdr_to_func(write_func, (void *)id, x, y, comp, &ff[0]);
					break;
				}
				case 3:
					stbi_write_png_to_func(write_func, (void *)id, x, y, comp, ptr, x * comp);
					break;
			}

			Monitor::Enter(writeInfo);
			try {
				writeInfo->Remove(id);
			}
			finally
			{
				Monitor::Exit(writeInfo);
			}
		}

		static void save_to_jpg(array<unsigned char> ^bytes, int x, int y, int comp, Stream ^output, int quality)
		{
			Monitor::Enter(writeInfo);
			int id;
			try {
				id = GenerateId();
				writeInfo->Add(id, output);
			}
			finally
			{
				Monitor::Exit(writeInfo);
			}

			pin_ptr<unsigned char> p = &bytes[0];
			unsigned char *ptr = (unsigned char *)p;

			stbi_write_jpg_to_func(write_func, (void *)id, x, y, comp, ptr, quality);

			Monitor::Enter(writeInfo);
			try {
				writeInfo->Remove(id);
			}
			finally
			{
				Monitor::Exit(writeInfo);
			}
		}

		static array<unsigned char> ^ compress_dxt(array<unsigned char> ^input, int w, int h, bool hasAlpha)
		{
			int osize = hasAlpha ? 16 : 8;

			array<unsigned char> ^result = gcnew array<unsigned char>((w + 3)*(h + 3) / 16 * osize);

			pin_ptr<unsigned char> ip = &input[0];
			unsigned char *rgba = ip;

			pin_ptr<unsigned char> op = &result[0];
			unsigned char *p = op;

			unsigned char block[16 * 4];
			for (int j = 0; j < w; j += 4)
			{
				int x = 4;
				for (int i = 0; i < h; i += 4)
				{
					if (j + 3 >= w) x = w - j;
					int y;
					for (y = 0; y < 4; ++y)
					{
						if (j + y >= h) break;
						memcpy(block + y * 16, rgba + w * 4 * (j + y) + i * 4, x * 4);
					}
					int y2;
					if (x < 4)
					{
						switch (x)
						{
						case 0:
							throw gcnew Exception("Unknown error");
						case 1:
							for (y2 = 0; y2 < y; ++y2)
							{
								memcpy(block + y2 * 16 + 1 * 4, block + y2 * 16 + 0 * 4, 4);
								memcpy(block + y2 * 16 + 2 * 4, block + y2 * 16 + 0 * 4, 8);
							}
							break;
						case 2:
							for (y2 = 0; y2 < y; ++y2)
								memcpy(block + y2 * 16 + 2 * 4, block + y2 * 16 + 0 * 4, 8);
							break;
						case 3:
							for (y2 = 0; y2 < y; ++y2)
								memcpy(block + y2 * 16 + 3 * 4, block + y2 * 16 + 1 * 4, 4);
							break;
						}
					}
					y2 = 0;
					for (; y < 4; ++y, ++y2)
						memcpy(block + y * 16, block + y2 * 16, 4 * 4);
					stb_compress_dxt_block(p, block, hasAlpha ? 1 : 0, 10);
					p += hasAlpha ? 16 : 8;
				}
			}

			return result;
		}

		static array<unsigned char> ^stbtt_BakeFontBitmap2(array<unsigned char> ^ttf, int offset, float pixel_height, array<unsigned char> ^pixels, int pw, int ph,
			int first_char, int num_chars)
		{

			pin_ptr<unsigned char> ttfPin = &ttf[0];
			unsigned char *ttfPtr = ttfPin;

			pin_ptr<unsigned char> pixelsPin = &pixels[0];
			unsigned char *pixelsPtr = pixelsPin;

			stbtt_bakedchar chardata[256];
			int result = stbtt_BakeFontBitmap(ttfPtr, offset, pixel_height, pixelsPtr, pw, ph, first_char, num_chars, chardata);

			if (result == 0)
			{
				return nullptr;
			}

			size_t sz = sizeof(stbtt_bakedchar) * num_chars;
			array<unsigned char> ^data = gcnew array<unsigned char>(sz);
			pin_ptr<unsigned char> dataPin = &data[0];
			unsigned char *dataPtr = dataPin;

			memcpy(dataPtr, chardata, sizeof(stbtt_bakedchar) * sz);

			return data;
		}
	};

	int read_callback(void *user, char *data, int size)
	{
		ReadInfo ^info;
		Monitor::Enter(Native::readInfo);
		int id = (int)user;
		try {
			info = Native::readInfo[id];
		}
		finally
		{
			Monitor::Exit(Native::readInfo);
		}

		if (size > info->buffer->Length) {
			info->buffer = gcnew array<unsigned char>(size * 2);
		}

		int res = info->stream->Read(info->buffer, 0, size);

		Marshal::Copy(info->buffer, 0, IntPtr(data), res);

		return res;
	}

	void skip_callback(void *user, int size)
	{
		ReadInfo ^info;
		Monitor::Enter(Native::readInfo);
		int id = (int)user;
		try {
			info = Native::readInfo[id];
		}
		finally
		{
			Monitor::Exit(Native::readInfo);
		}

		info->stream->Seek(size, SeekOrigin::Current);
	}

	int eof_callback(void *user)
	{
		ReadInfo ^info;
		Monitor::Enter(Native::readInfo);
		int id = (int)user;
		try {
			info = Native::readInfo[id];
		}
		finally
		{
			Monitor::Exit(Native::readInfo);
		}

		return info->stream->CanRead ? 1 : 0;
	}

	void write_func(void *context, void *data, int size)
	{
		Stream ^ info;
		Monitor::Enter(Native::writeInfo);
		int id = (int)context;
		try {
			info = Native::writeInfo[id];
		}
		finally
		{
			Monitor::Exit(Native::writeInfo);
		}

		unsigned char *bptr = (unsigned char *)data;
		for (int i = 0; i < size; ++i)
		{
			info->WriteByte(*bptr);
			++bptr;
		}
	}
}
