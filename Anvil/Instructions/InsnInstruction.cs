namespace Anvil.Instructions;

public class InsnInstruction : Instruction
{
    private static readonly HashSet<OperationCode> OperandOpCodes =
    [
        OperationCode.BIPUSH, OperationCode.SIPUSH, OperationCode.LDC,
        OperationCode.LDC_W, OperationCode.LDC2_W, OperationCode.ILOAD,
        OperationCode.LLOAD, OperationCode.FLOAD, OperationCode.DLOAD,
        OperationCode.ALOAD, OperationCode.ISTORE, OperationCode.LSTORE,
        OperationCode.FSTORE, OperationCode.DSTORE, OperationCode.ASTORE,
        OperationCode.IINC, OperationCode.IFEQ, OperationCode.IFNE,
        OperationCode.IFLT, OperationCode.IFGE, OperationCode.IFGT,
        OperationCode.IFLE, OperationCode.IF_ICMPEQ, OperationCode.IF_ICMPNE,
        OperationCode.IF_ICMPLT, OperationCode.IF_ICMPGE, OperationCode.IF_ICMPGT,
        OperationCode.IF_ICMPLE, OperationCode.IF_ACMPEQ, OperationCode.IF_ACMPNE,
        OperationCode.GOTO, OperationCode.JSR, OperationCode.RET,
        OperationCode.TABLESWITCH, OperationCode.LOOKUPSWITCH,
        OperationCode.GETSTATIC, OperationCode.PUTSTATIC, OperationCode.GETFIELD,
        OperationCode.PUTFIELD, OperationCode.INVOKEVIRTUAL,
        OperationCode.INVOKESPECIAL, OperationCode.INVOKESTATIC,
        OperationCode.INVOKEINTERFACE, OperationCode.INVOKEDYNAMIC,
        OperationCode.NEW, OperationCode.NEWARRAY, OperationCode.ANEWARRAY,
        OperationCode.CHECKCAST, OperationCode.INSTANCEOF, OperationCode.WIDE,
        OperationCode.MULTIANEWARRAY, OperationCode.IFNULL,
        OperationCode.IFNONNULL, OperationCode.GOTO_W, OperationCode.JSR_W
    ];

    public InsnInstruction(OperationCode opCode) : base(opCode)
    {
        if (!Enum.IsDefined(opCode)
            || OperandOpCodes.Contains(opCode)
            || opCode is OperationCode.BREAKPOINT or OperationCode.IMPDEP1 or OperationCode.IMPDEP2)
        {
            throw new ArgumentException(
                $"OpCode {opCode} is not a valid operandless instruction.",
                nameof(opCode));
        }
    }

    public override int GetSize() => 1;

    public override void Write(Stream stream)
    {
        stream.WriteByte((byte)OpCode);
    }
}
