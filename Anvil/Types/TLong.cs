using System.Buffers.Binary;
using Anvil.Interfaces;

namespace Anvil.Types;

/// <summary>
/// Represents a JVM s8 (signed long).
/// </summary>
public readonly struct TLong(long value) : IType<TLong>
{
    public long Value { get; } = value;

    public void Write(Stream stream)
    {
        Span<byte> buffer = stackalloc byte[8];
        BinaryPrimitives.WriteInt64BigEndian(buffer, Value);
        stream.Write(buffer);
    }

    public static TLong Read(Stream stream)
    {
        Span<byte> buffer = stackalloc byte[8];
        stream.ReadExactly(buffer);
        return new TLong(BinaryPrimitives.ReadInt64BigEndian(buffer));
    }

    public override string ToString() => Value.ToString();
    public static implicit operator long(TLong v) => v.Value;
    public static implicit operator TLong(long v) => new(v);
}