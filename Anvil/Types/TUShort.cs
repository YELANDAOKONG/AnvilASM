using System.Buffers.Binary;
using Anvil.Interfaces;

namespace Anvil.Types;

/// <summary>
/// Represents a JVM u2 (unsigned short), commonly used for Constant Pool Indices.
/// </summary>
public readonly struct TUShort(ushort value) : IType<TUShort>
{
    public ushort Value { get; } = value;

    public void Write(Stream stream)
    {
        Span<byte> buffer = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(buffer, Value);
        stream.Write(buffer);
    }

    public static TUShort Read(Stream stream)
    {
        Span<byte> buffer = stackalloc byte[2];
        stream.ReadExactly(buffer); 
        return new TUShort(BinaryPrimitives.ReadUInt16BigEndian(buffer));
    }

    public override string ToString() => Value.ToString();
    public static implicit operator ushort(TUShort v) => v.Value;
    public static implicit operator TUShort(ushort v) => new(v);
}