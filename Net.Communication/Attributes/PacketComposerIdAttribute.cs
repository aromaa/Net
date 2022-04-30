using System;

namespace Net.Communication.Attributes;

public sealed class PacketComposerIdAttribute : Attribute
{
	public object Id { get; }

	public PacketComposerIdAttribute(object id)
	{
		this.Id = id;
	}
}