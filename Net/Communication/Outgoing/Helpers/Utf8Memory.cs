using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Utf8;

namespace Net.Communication.Outgoing.Helpers
{
    public readonly struct Utf8Memory
    {
        public static Utf8Memory Empty { get; } = new Utf8Memory(ReadOnlyMemory<byte>.Empty);

        private ReadOnlyMemory<byte> _Buffer { get; }

        public Utf8Memory(ReadOnlyMemory<byte> buffer)
        {
            this._Buffer = buffer;
        }

        public static implicit operator Utf8Span(Utf8Memory value) => new Utf8Span(value._Buffer.Span);

        public static implicit operator Utf8String(Utf8Memory value) => new Utf8String(value._Buffer.Span);

        public static implicit operator Utf8Memory(Utf8Span value) => new Utf8Memory(value.Bytes.ToArray());
        public static implicit operator Utf8Memory(Utf8String value) => new Utf8Memory(value.Bytes.ToArray());
    }
}
