namespace Net.Communication.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public sealed class PacketComposerIdAttribute(object id) : Attribute
{
	public object Id { get; } = id;
}
