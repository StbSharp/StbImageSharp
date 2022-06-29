using System;
using System.Runtime.InteropServices;

namespace Hebron.Runtime
{
	internal unsafe class UnsafeArray1D<T> where T : struct
	{
		private readonly T[] _data;
		private readonly GCHandle _pinHandle;

		internal GCHandle PinHandle => _pinHandle;

		public T this[int index]
		{
			get => _data[index];
			set
			{
				_data[index] = value;
			}
		}

		internal UnsafeArray1D(int size)
		{
			if (size < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(size));
			}

			_data = new T[size];
			_pinHandle = GCHandle.Alloc(_data, GCHandleType.Pinned);
		}

		internal UnsafeArray1D(T[] data, int sizeOf)
		{
			if (sizeOf <= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(sizeOf));
			}

			_data = data ?? throw new ArgumentNullException(nameof(data));
			_pinHandle = GCHandle.Alloc(_data, GCHandleType.Pinned);
		}

		~UnsafeArray1D()
		{
			_pinHandle.Free();
		}

		public void* ToPointer()
		{
			return _pinHandle.AddrOfPinnedObject().ToPointer();
		}

		public static implicit operator void*(UnsafeArray1D<T> array)
		{
			return array.ToPointer();
		}
	}
}