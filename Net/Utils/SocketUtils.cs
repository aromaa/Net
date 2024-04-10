using System.Net.Sockets;

namespace Net.Utils;

internal static class SocketUtils
{
	internal static int GetAddressFamilyLength(AddressFamily addressFamily)
	{
		return addressFamily switch
		{
			AddressFamily.InterNetwork => 4,
			AddressFamily.InterNetworkV6 => 16,
			_ => throw new NotSupportedException()
		};
	}
}
