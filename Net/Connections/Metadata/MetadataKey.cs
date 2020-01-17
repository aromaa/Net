using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Net.Connections.Metadata
{
    public class MetadataKey
    {
        private static int NextId;

        private int Id { get; }
        public string Name { get; }

        internal MetadataKey(int id, string name)
        {
            this.Id = id;
            this.Name = name;
        }

        public override int GetHashCode() => this.Id;

        internal static int GetNextId() => Interlocked.Increment(ref MetadataKey.NextId);
    }

    public class MetadataKey<T> : MetadataKey
    {
        internal MetadataKey(int id, string name) : base(id, name)
        {
        }

        public static MetadataKey<T> Create(string name) => new MetadataKey<T>(MetadataKey.GetNextId(), name);
    }
}
