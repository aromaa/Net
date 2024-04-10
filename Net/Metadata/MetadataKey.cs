namespace Net.Metadata;

public abstract class MetadataKey
{
	private static int NextId;

	private int Id { get; }
	public string Name { get; }

	private protected MetadataKey(int id, string name)
	{
		this.Id = id;
		this.Name = name;
	}

	public override int GetHashCode() => this.Id;

	private protected static int GetNextId() => Interlocked.Increment(ref MetadataKey.NextId);
}

public sealed class MetadataKey<T> : MetadataKey
{
	private MetadataKey(int id, string name)
		: base(id, name)
	{
	}

	public static MetadataKey<T> Create(string name) => new(MetadataKey.GetNextId(), name);
}
