namespace Net.Communication.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public sealed class PacketParserIdAttribute(object id) : Attribute
{
	public object Id { get; } = id;
}
