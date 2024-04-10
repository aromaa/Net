namespace Net.Communication.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public sealed class PacketManagerTagsAttribute(params string[] tags) : Attribute
{
	public string[] Tags { get; } = tags;
}
