namespace Net.Communication.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public sealed class PacketManagerGeneratorAttribute(Type target) : Attribute
{
	public Type Target { get; } = target;
}
