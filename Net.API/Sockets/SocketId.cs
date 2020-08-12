using System;
using System.Threading;

namespace Net.API.Sockets
{
    public readonly struct SocketId : IEquatable<SocketId>
    {
        private static int NextId;

        private readonly uint Id;

        private SocketId(uint id)
        {
            this.Id = id;
        }

        public override bool Equals(object? obj) => obj is SocketId other && this.Equals(other);
        public bool Equals(SocketId other)
        {
            if (this.Id != other.Id)
            {
                return false;
            }

            return true;
        }

        public override int GetHashCode() => (int)this.Id;

        public static SocketId GenerateNew()
        {
            uint id = (uint)Interlocked.Increment(ref SocketId.NextId);

            return new SocketId(id);
        }
    }
}
