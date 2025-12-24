using System.Buffers.Binary;
using Anvil.Interfaces;

namespace Anvil.Types;

/// <summary>
/// Represents a JVM float (IEEE 754 single-precision).
/// </summary>
public readonly struct TFloat(float value) : IType<TFloat>
{
    public float Value { get; } = value;

    public void Write(Stream stream)
    {
        Span<byte> buffer = stackalloc byte[4];
        BinaryPrimitives.WriteSingleBigEndian(buffer, Value);
        stream.Write(buffer);
    }

    public static TFloat Read(Stream stream)
    {
        Span<byte> buffer = stackalloc byte[4];
        stream.ReadExactly(buffer);
        return new TFloat(BinaryPrimitives.ReadSingleBigEndian(buffer));
    }

    public override string ToString() => Value.ToString();
    public static implicit operator float(TFloat v) => v.Value;
    public static implicit operator TFloat(float v) => new(v);
}