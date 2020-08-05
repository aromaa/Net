using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Net.Extensions
{
    internal static class InterlockedExtension
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static T Or<T>(ref this T @this, T value) where T: struct, Enum
        {
            if (Unsafe.SizeOf<T>() == 4)
            {
                int result = Interlocked.Or(ref Unsafe.As<T, int>(ref @this), Unsafe.As<T, int>(ref value));

                return Unsafe.As<int, T>(ref result);
            }
            else if (Unsafe.SizeOf<T>() == 8)
            {
                long result = Interlocked.Or(ref Unsafe.As<T, long>(ref @this), Unsafe.As<T, long>(ref value));

                return Unsafe.As<long, T>(ref result);
            }

            throw new NotSupportedException();
        }
    }
}
