using Anvil.Interfaces;

namespace Anvil.Types;

/// <summary>
/// Represents a JVM u1 (unsigned byte), used for Tags and Flags.
/// </summary>
public readonly struct TUByte(byte value) : IType<TUByte>
{
    public byte Value { get; } = value;

    public void Write(Stream stream) => stream.WriteByte(Value);

    public static TUByte Read(Stream stream)
    {
        int b = stream.ReadByte();
        if (b == -1) throw new EndOfStreamException();
        return new TUByte((byte)b);
    }

    public override string ToString() => Value.ToString("X2");
    public static implicit operator byte(TUByte v) => v.Value;
    public static implicit operator TUByte(byte v) => new(v);
}