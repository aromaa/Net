﻿using System.Runtime.CompilerServices;

namespace Net.Extensions;

internal static class InterlockedExtension
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static T Or<T>(ref this T @this, T value)
		where T : struct, Enum
	{
		if (Unsafe.SizeOf<T>() == sizeof(int))
		{
			int result = Interlocked.Or(ref Unsafe.As<T, int>(ref @this), Unsafe.As<T, int>(ref value));

			return Unsafe.As<int, T>(ref result);
		}
		else if (Unsafe.SizeOf<T>() == sizeof(long))
		{
			long result = Interlocked.Or(ref Unsafe.As<T, long>(ref @this), Unsafe.As<T, long>(ref value));

			return Unsafe.As<long, T>(ref result);
		}

		throw new NotSupportedException();
	}
}
