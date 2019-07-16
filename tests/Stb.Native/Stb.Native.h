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
	public ref class Native
	{
	public:
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
	};
}
