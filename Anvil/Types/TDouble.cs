using System.Buffers.Binary;
using Anvil.Interfaces;

namespace Anvil.Types;

/// <summary>
/// Represents a JVM double (IEEE 754 double-precision).
/// </summary>
public readonly struct TDouble(double value) : IType<TDouble>
{
    public double Value { get; } = value;

    public void Write(Stream stream)
    {
        Span<byte> buffer = stackalloc byte[8];
        BinaryPrimitives.WriteDoubleBigEndian(buffer, Value);
        stream.Write(buffer);
    }

    public static TDouble Read(Stream stream)
    {
        Span<byte> buffer = stackalloc byte[8];
        stream.ReadExactly(buffer);
        return new TDouble(BinaryPrimitives.ReadDoubleBigEndian(buffer));
    }

    public override string ToString() => Value.ToString();
    public static implicit operator double(TDouble v) => v.Value;
    public static implicit operator TDouble(double v) => new(v);
}