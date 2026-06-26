namespace Anvil.Instructions;

public class IincInstruction : Instruction
{
    public int VarIndex { get; set; }
    public int Increment { get; set; }

    public IincInstruction(int varIndex, int increment) : base(OperationCode.IINC)
    {
        VarIndex = varIndex;
        Increment = increment;
    }

    private bool NeedsWide
    {
        get
        {
            if (VarIndex > 0xFFFF)
            {
                throw new ArgumentOutOfRangeException(nameof(VarIndex), $"VarIndex {VarIndex} exceeds maximum 65535.");
            }

            if (Increment < short.MinValue || Increment > short.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(Increment),
                    $"Increment {Increment} does not fit in signed 16-bit.");
            }

            return VarIndex > 0xFF || Increment > 0x7F || Increment < -0x80;
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
