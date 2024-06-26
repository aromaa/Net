﻿using System.Net;
using System.Net.Sockets;

namespace Net.Utils;

internal static class IPEndPointUtils
{
	internal static readonly IPEndPoint None = new(IPAddress.None, 3);
	internal static readonly IPEndPoint IPv6None = new(IPAddress.IPv6None, 3);

	internal static IPEndPoint GetNone(AddressFamily addressFamily)
	{
		return addressFamily switch
		{
			AddressFamily.InterNetwork => IPEndPointUtils.None,
			AddressFamily.InterNetworkV6 => IPEndPointUtils.IPv6None,
			_ => throw new NotSupportedException()
		};
	}
}
