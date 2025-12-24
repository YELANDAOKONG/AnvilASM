using System.Buffers.Binary;
using Anvil.Interfaces;

namespace Anvil.Types;

/// <summary>
/// Represents a JVM u4 (unsigned int), commonly used for lengths and magic numbers.
/// </summary>
public readonly struct TUInt(uint value) : IType<TUInt>
{
    public uint Value { get; } = value;

    public void Write(Stream stream)
    {
        Span<byte> buffer = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(buffer, Value);
        stream.Write(buffer);
    }

    public static TUInt Read(Stream stream)
    {
        Span<byte> buffer = stackalloc byte[4];
        stream.ReadExactly(buffer);
        return new TUInt(BinaryPrimitives.ReadUInt32BigEndian(buffer));
    }

    public override string ToString() => Value.ToString();
    public static implicit operator uint(TUInt v) => v.Value;
    public static implicit operator TUInt(uint v) => new(v);
}