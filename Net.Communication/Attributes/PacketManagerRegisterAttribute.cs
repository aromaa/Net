using JetBrains.Annotations;

namespace Net.Communication.Attributes;

[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Class)]
public sealed class PacketManagerRegisterAttribute(Type? defaultManager = default) : Attribute
{
	public Type? DefaultManager { get; } = defaultManager;

	public bool Enabled { get; set; } = true;
	public int Order { get; set; }
}
