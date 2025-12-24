using System.Buffers.Binary;
using Anvil.Interfaces;

namespace Anvil.Types;

/// <summary>
/// Represents a JVM s4 (signed int).
/// </summary>
public readonly struct TInt(int value) : IType<TInt>
{
    public int Value { get; } = value;

    public void Write(Stream stream)
    {
        Span<byte> buffer = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(buffer, Value);
        stream.Write(buffer);
    }

    public static TInt Read(Stream stream)
    {
        Span<byte> buffer = stackalloc byte[4];
        stream.ReadExactly(buffer);
        return new TInt(BinaryPrimitives.ReadInt32BigEndian(buffer));
    }

    public override string ToString() => Value.ToString();
    public static implicit operator int(TInt v) => v.Value;
    public static implicit operator TInt(int v) => new(v);
}