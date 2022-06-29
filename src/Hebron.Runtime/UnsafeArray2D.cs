using System;
using System.Runtime.InteropServices;

namespace Hebron.Runtime
{
	internal unsafe class UnsafeArray2D<T> where T : struct
	{
		private readonly UnsafeArray1D<T>[] _data;
		private IntPtr[] _pinAddresses;
		private readonly GCHandle _pinAddressesHandle;

		internal UnsafeArray1D<T> this[int index]
		{
			get => _data[index];
			set
			{
				_data[index] = value;
			}
		}

		internal UnsafeArray2D(int size1, int size2)
		{
			_data = new UnsafeArray1D<T>[size1];
			_pinAddresses = new IntPtr[size1];
			for (var i = 0; i < size1; ++i)
			{
				_data[i] = new UnsafeArray1D<T>(size2);
				_pinAddresses[i] = _data[i].PinHandle.AddrOfPinnedObject();
			}

			_pinAddressesHandle = GCHandle.Alloc(_pinAddresses, GCHandleType.Pinned);
		}

		~UnsafeArray2D()
		{
			_pinAddressesHandle.Free();
		}

		public void* ToPointer() => _pinAddressesHandle.AddrOfPinnedObject().ToPointer();

		public static implicit operator void*(UnsafeArray2D<T> array)
		{
			return array.ToPointer();
		}
	}
}