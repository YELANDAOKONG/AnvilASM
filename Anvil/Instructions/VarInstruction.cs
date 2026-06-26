namespace Anvil.Instructions;

public class VarInstruction : Instruction
{
    public int VarIndex { get; set; }

    private static readonly HashSet<OperationCode> ValidOpCodes =
    [
        OperationCode.ILOAD, OperationCode.LLOAD, OperationCode.FLOAD, OperationCode.DLOAD, OperationCode.ALOAD,
        OperationCode.ISTORE, OperationCode.LSTORE, OperationCode.FSTORE, OperationCode.DSTORE, OperationCode.ASTORE,
        OperationCode.RET
    ];

    public VarInstruction(OperationCode opCode, int varIndex) : base(opCode)
    {
        if (!ValidOpCodes.Contains(opCode))
        {
            throw new ArgumentException(
                $"OpCode {opCode} is not valid for VarInstruction. Use ILOAD/LLOAD/FLOAD/DLOAD/ALOAD/ISTORE/LSTORE/FSTORE/DSTORE/ASTORE/RET.",
                nameof(opCode));
        }

        VarIndex = varIndex;
    }

    private bool HasShortForm => VarIndex is >= 0 and <= 3 && GetTypeIndex() >= 0;

    private bool NeedsWide => VarIndex > 0xFFFF
        ? throw new ArgumentOutOfRangeException(nameof(VarIndex), $"VarIndex {VarIndex} exceeds maximum 65535.")
        : VarIndex > 0xFF;

    private int GetTypeIndex()
    {
        return OpCode switch
        {
            OperationCode.ILOAD or OperationCode.ISTORE => 0,
            OperationCode.LLOAD or OperationCode.LSTORE => 1,
            OperationCode.FLOAD or OperationCode.FSTORE => 2,
            OperationCode.DLOAD or OperationCode.DSTORE => 3,
            OperationCode.ALOAD or OperationCode.ASTORE => 4,
            _ => -1
        };
    }

    public override int GetSize()
    {
        if (HasShortForm)
        {
            return 1;
        }

        return NeedsWide ? 4 : 2;
    }

    public override void Write(Stream stream)
    {
        if (HasShortForm)
        {
            var shortOpCode = (OperationCode)((byte)OpCode + 5 + GetTypeIndex() * 3 + VarIndex);
            stream.WriteByte((byte)shortOpCode);
            return;
        }

        if (NeedsWide)
        {
            stream.WriteByte((byte)OperationCode.WIDE);
        }

        stream.WriteByte((byte)OpCode);

        if (NeedsWide)
        {
            stream.WriteByte((byte)((VarIndex >> 8) & 0xFF));
        }

        stream.WriteByte((byte)(VarIndex & 0xFF));
    }

    public override string ToString() => $"{OpCode} {VarIndex}";
}
