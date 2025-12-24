using Anvil.Interfaces;

namespace Anvil.Types;

/// <summary>
/// Represents a boolean value (encoded as 1 byte: 0x01 for true, 0x00 for false).
/// </summary>
public readonly struct TBoolean(bool value) : IType<TBoolean>
{
    public bool Value { get; } = value;

    public void Write(Stream stream)
    {
        stream.WriteByte(Value ? (byte)1 : (byte)0);
    }

    public static TBoolean Read(Stream stream)
    {
        int b = stream.ReadByte();
        if (b == -1) throw new EndOfStreamException();
        return new TBoolean(b != 0);
    }

    public override string ToString() => Value ? "true" : "false";
    public static implicit operator bool(TBoolean v) => v.Value;
    public static implicit operator TBoolean(bool v) => new(v);
}