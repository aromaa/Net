using JetBrains.Annotations;

namespace Net.Communication.Attributes;

[AttributeUsage(AttributeTargets.Class)]
[MeansImplicitUse(ImplicitUseTargetFlags.Members)]
public sealed class PacketByRefTypeAttribute(Type byRefType) : Attribute
{
	public Type ByRefType { get; } = byRefType;

	public ConsumerType Type { get; set; }

	public bool Parser
	{
		get => this.Type.HasFlag(ConsumerType.Parser);
		set => this.Type |= value ? ConsumerType.Parser : ~ConsumerType.Parser;
	}

	public bool Handler
	{
		get => this.Type.HasFlag(ConsumerType.Handler);
		set => this.Type |= value ? ConsumerType.Handler : ~ConsumerType.Handler;
	}

	[Flags]
	public enum ConsumerType
	{
		Parser = 1 << 0,
		Handler = 1 << 1,

		//Shortcuts
		ParserAndHandler = ConsumerType.Parser | ConsumerType.Handler
	}
}
