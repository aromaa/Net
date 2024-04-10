using Net.Sockets;

namespace Net.Collections;

/// <summary>
/// Internal implementation detail.
/// </summary>
public interface ISocketHolder
{
	internal ISocket Socket { get; }
}
