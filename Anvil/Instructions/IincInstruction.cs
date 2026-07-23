namespace Anvil.Instructions;

public class IincInstruction : Instruction
{
    public int VarIndex { get; set; }
    public int Increment { get; set; }

    public IincInstruction(int varIndex, int increment) : base(OperationCode.IINC)
    {
        if (varIndex < 0 || varIndex > ushort.MaxValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(varIndex),
                varIndex,
                "Local variable index must be between 0 and 65535.");
        }

        if (increment < short.MinValue || increment > short.MaxValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(increment),
                increment,
                "Increment must fit in a signed 16-bit value.");
        }

        VarIndex = varIndex;
        Increment = increment;
    }

    private bool NeedsWide
    {
        get
        {
            return VarIndex > byte.MaxValue || Increment > sbyte.MaxValue || Increment < sbyte.MinValue;
        }
    }

    public override int GetSize() => NeedsWide ? 6 : 3;

    public override void Write(Stream stream)
    {
        if (NeedsWide)
        {
            stream.WriteByte((byte)OperationCode.WIDE);
        }

        stream.WriteByte((byte)OperationCode.IINC);

        if (NeedsWide)
        {
            stream.WriteByte((byte)((VarIndex >> 8) & 0xFF));
        }

        stream.WriteByte((byte)(VarIndex & 0xFF));

        if (NeedsWide)
        {
            stream.WriteByte((byte)((Increment >> 8) & 0xFF));
        }

        stream.WriteByte((byte)(Increment & 0xFF));
    }

    public override string ToString() => $"{OpCode} {VarIndex} {Increment}";
}
