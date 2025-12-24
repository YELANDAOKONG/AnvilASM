using System.Buffers.Binary;
using Anvil.Interfaces;

namespace Anvil.Types;

/// <summary>
/// Represents a JVM s2 (signed short).
/// </summary>
public readonly struct TShort(short value) : IType<TShort>
{
    public short Value { get; } = value;

    public void Write(Stream stream)
    {
        Span<byte> buffer = stackalloc byte[2];
        BinaryPrimitives.WriteInt16BigEndian(buffer, Value);
        stream.Write(buffer);
    }

    public static TShort Read(Stream stream)
    {
        Span<byte> buffer = stackalloc byte[2];
        stream.ReadExactly(buffer);
        return new TShort(BinaryPrimitives.ReadInt16BigEndian(buffer));
    }

    public override string ToString() => Value.ToString();
    public static implicit operator short(TShort v) => v.Value;
    public static implicit operator TShort(short v) => new(v);
}