#pragma once

using namespace System;
using namespace System::Runtime::InteropServices;

#include <memory>

#define STB_VORBIS_NO_INLINE_DECODE
#define STB_VORBIS_NO_FAST_SCALED_FLOAT
#include "stb_vorbis.c"

namespace StbNative {
	public ref class Vorbis
	{
	private:
		stb_vorbis *_vorbis;
		stb_vorbis_info *_vorbisInfo;
		float _lengthInSeconds;
		array<short> ^_songBuffer;
		array<unsigned char> ^_data;
		int _decoded;
	public:
		property int SampleRate
		{
			int get()
			{
				return (int)_vorbisInfo->sample_rate;
			}
		}

		property int Channels
		{
			int get()
			{
				return (int)_vorbisInfo->channels;
			}
		}

		property float LengthInSeconds
		{
			float get()
			{
				return _lengthInSeconds;
			}
		}

		property array<short>^ SongBuffer
		{
			array<short>^ get()
			{
				return _songBuffer;
			}
		}

		property int Decoded
		{
			int get()
			{
				return _decoded;
			}
		}

		Vorbis(array<unsigned char> ^data)
		{
			_data = data;

			pin_ptr<unsigned char> b = &data[0];
			stb_vorbis *vorbis = stb_vorbis_open_memory(b, data->Length, NULL, NULL);
			_vorbis = vorbis;
			_vorbisInfo = new stb_vorbis_info();

			auto vi = stb_vorbis_get_info(vorbis);
			memcpy(_vorbisInfo, &vi, sizeof(stb_vorbis_info));

			_lengthInSeconds = stb_vorbis_stream_length_in_seconds(_vorbis);

			_songBuffer = gcnew array<short>(_vorbisInfo->sample_rate);

			Restart();
		}

		void Restart()
		{
			stb_vorbis_seek_start(_vorbis);
		}

		void SubmitBuffer()
		{
			pin_ptr<short> ptr = &_songBuffer[0];
			_decoded = stb_vorbis_get_samples_short_interleaved(_vorbis, _vorbisInfo->channels, ptr, (int)_vorbisInfo->sample_rate);
		}

		static Vorbis^ FromMemory(array<unsigned char> ^data)
		{
			return gcnew Vorbis(data);
		}

		static array<short> ^ decode_vorbis_from_memory(array<unsigned char> ^bytes, [Out] int %sampleRate, [Out] int %channels)
		{
			pin_ptr<unsigned char> p = &bytes[0];

			int c, s;
			short *output;

			const unsigned char *data = (const unsigned char *)p;
			int size = stb_vorbis_decode_memory(data, bytes->Length, &c, &s, &output);

			array<short> ^result = gcnew array<short>(size);

			Marshal::Copy(IntPtr((void *)output), result, 0, result->Length);
			free(output);

			sampleRate = s;
			channels = c;

			return result;
		}
	};
}