using System.Collections.Immutable;

namespace Net.Metadata;

public sealed class MetadataMap
{
	//Internally this uses binary tree
	private ImmutableDictionary<MetadataKey, object?> Metadata;

	public MetadataMap()
	{
		this.Metadata = ImmutableDictionary<MetadataKey, object?>.Empty;
	}

	public void Set<T>(MetadataKey<T> key, T value)
	{
		do
		{
			ImmutableDictionary<MetadataKey, object?> metadata = this.Metadata;

			if (Interlocked.CompareExchange(ref this.Metadata, metadata.SetItem(key, value), metadata) == metadata)
			{
				break;
			}
		}
		while (true);
	}

	public bool TryGetValue<T>(MetadataKey<T> key, out T value)
	{
		if (this.Metadata.TryGetValue(key, out object? outValue))
		{
			value = (T)outValue!;

			return true;
		}

		value = default!;

		return false;
	}
}
