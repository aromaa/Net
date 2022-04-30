using Net.Sockets;

namespace Net.Collections;

public interface ISocketMatcher
{
	public bool Matches(ISocket socket);
}