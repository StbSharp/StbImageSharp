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
#include "stb_image.h"

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
