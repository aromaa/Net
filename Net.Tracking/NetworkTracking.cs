using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Net.Tracking
{
    public static class NetworkTracking
    {
        public static readonly bool IsEnabled = true;

        internal static long DownstreamBytes;
        internal static long UpstreamBytes;

        public static long CurrentDownstream { get; private set; }
        public static long CurrentUpstream { get; private set; }

        static NetworkTracking()
        {
            if (NetworkTracking.IsEnabled)
            {
                Task.Run(NetworkTracking.DoJob);
            }
        }

        private static async Task DoJob()
        {
            while (true)
            {
                NetworkTracking.CurrentDownstream = Interlocked.Exchange(ref NetworkTracking.DownstreamBytes, 0);
                NetworkTracking.CurrentUpstream = Interlocked.Exchange(ref NetworkTracking.UpstreamBytes, 0);

                await Task.Delay(1000).ConfigureAwait(false);
            }
        }
    }
}
