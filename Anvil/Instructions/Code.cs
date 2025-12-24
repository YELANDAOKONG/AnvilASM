using System.Text;

namespace Anvil.Instructions;

public class Code
{
    public OperationCode? WidePrefix { get; private set; }
    public OperationCode OpCode { get; }
    public IReadOnlyList<Operand> Operands { get; }

    public Code(OperationCode opCode, params Operand[] operands)
    {
        OpCode = opCode;
        Operands = operands.ToList().AsReadOnly();

        // Automatically add wide prefix if applicable
        if (OperationCodeMapping.TryGetInfo(opCode, out var info) && info!.CanBeWide)
        {
            // Check if operands exceed standard size limits requiring WIDE
            bool needsWide = Operands.Any(o =>
                (o.Type == OperandType.LocalIndex && o.Data.Length == 2) || // Index > 255
                (o.Type == OperandType.IincPair && o.Data.Length == 4));    // Index > 255 or Increment > 127

            if (needsWide)
                WidePrefix = OperationCode.WIDE;
        }
    }

    public override string ToString()
    {
        var sb = new StringBuilder();

        if (WidePrefix.HasValue)
            sb.Append("wide ");

        sb.Append(OpCode.ToString().ToLowerInvariant());

        if (Operands.Count > 0)
        {
            sb.Append(" ");
            sb.Append(string.Join(" ", Operands.Select(o => o.ToString())));
        }

        return sb.ToString();
    }

    // ================== Factory Methods ==================

    // Push Constants
    public static Code PushInt(int value)
    {
        return value switch
        {
            -1 => new(OperationCode.ICONST_M1),
            0 => new(OperationCode.ICONST_0),
            1 => new(OperationCode.ICONST_1),
            2 => new(OperationCode.ICONST_2),
            3 => new(OperationCode.ICONST_3),
            4 => new(OperationCode.ICONST_4),
            5 => new(OperationCode.ICONST_5),
            >= sbyte.MinValue and <= sbyte.MaxValue => new(OperationCode.BIPUSH, Operand.ByteImmediate((sbyte)value)),
            >= short.MinValue and <= short.MaxValue => new(OperationCode.SIPUSH, Operand.ShortImmediate((short)value)),
            _ => throw new ArgumentOutOfRangeException(nameof(value), "Value is too large for immediate push. Use 'ldc' instead.")
        };
    }

    public static Code PushLong(long value)
    {
        if (value == 0) return new(OperationCode.LCONST_0);
        if (value == 1) return new(OperationCode.LCONST_1);
        throw new ArgumentOutOfRangeException(nameof(value), "Value cannot be optimized to a constant instruction. Use 'ldc2_w' instead.");
    }

    public static Code PushFloat(float value)
    {
        if (value == 0f) return new(OperationCode.FCONST_0);
        if (value == 1f) return new(OperationCode.FCONST_1);
        if (value == 2f) return new(OperationCode.FCONST_2);
        throw new ArgumentOutOfRangeException(nameof(value), "Value cannot be optimized to a constant instruction. Use 'ldc' instead.");
    }

    public static Code PushDouble(double value)
    {
        if (value == 0.0) return new(OperationCode.DCONST_0);
        if (value == 1.0) return new(OperationCode.DCONST_1);
        throw new ArgumentOutOfRangeException(nameof(value), "Value cannot be optimized to a constant instruction. Use 'ldc2_w' instead.");
    }

    // LDC Series (Load Constants)
    public static Code Ldc(ushort cpIndex)
    {
        if (cpIndex <= 0xFF)
            return new(OperationCode.LDC, Operand.ConstantPoolIndex((byte)cpIndex));
        return new(OperationCode.LDC_W, Operand.ConstantPoolIndex(cpIndex));
    }

    public static Code Ldc2(ushort cpIndex) => new(OperationCode.LDC2_W, Operand.ConstantPoolIndex(cpIndex));

    // Load Local Variables
    public static Code ILoad(ushort index)
        => new(OperationCode.ILOAD, index <= 0xFF ? Operand.LocalIndex((byte)index) : Operand.WideLocalIndex(index));

    public static Code LLoad(ushort index)
        => new(OperationCode.LLOAD, index <= 0xFF ? Operand.LocalIndex((byte)index) : Operand.WideLocalIndex(index));

    public static Code ALoad(ushort index)
        => new(OperationCode.ALOAD, index <= 0xFF ? Operand.LocalIndex((byte)index) : Operand.WideLocalIndex(index));

    // Store Local Variables
    public static Code IStore(ushort index)
        => new(OperationCode.ISTORE, index <= 0xFF ? Operand.LocalIndex((byte)index) : Operand.WideLocalIndex(index));

    public static Code LStore(ushort index)
        => new(OperationCode.LSTORE, index <= 0xFF ? Operand.LocalIndex((byte)index) : Operand.WideLocalIndex(index));

    public static Code AStore(ushort index)
        => new(OperationCode.ASTORE, index <= 0xFF ? Operand.LocalIndex((byte)index) : Operand.WideLocalIndex(index));

    // Method Invocations
    public static Code InvokeVirtual(ushort methodIndex)
        => new(OperationCode.INVOKEVIRTUAL, Operand.ConstantPoolIndex(methodIndex));

    public static Code InvokeSpecial(ushort methodIndex)
        => new(OperationCode.INVOKESPECIAL, Operand.ConstantPoolIndex(methodIndex));

    public static Code InvokeStatic(ushort methodIndex)
        => new(OperationCode.INVOKESTATIC, Operand.ConstantPoolIndex(methodIndex));

    public static Code InvokeInterface(ushort methodIndex, byte argsCount)
        => new(OperationCode.INVOKEINTERFACE, Operand.InvokeInterface(methodIndex, argsCount));

    // Object Creation
    public static Code New(ushort classIndex)
        => new(OperationCode.NEW, Operand.ConstantPoolIndex(classIndex));

    public static Code NewArray(byte atype)
        => new(OperationCode.NEWARRAY, Operand.NewArrayAtype(atype));

    public static Code ANewArray(ushort classIndex)
        => new(OperationCode.ANEWARRAY, Operand.ConstantPoolIndex(classIndex));

    public static Code MultiANewArray(ushort classIndex, byte dimensions)
        => new(OperationCode.MULTIANEWARRAY, Operand.MultiANewArray(classIndex, dimensions));

    // Control Flow / Branching
    public static Code Goto(short offset) => new(OperationCode.GOTO, Operand.BranchOffset(offset));
    public static Code IfEq(short offset) => new(OperationCode.IFEQ, Operand.BranchOffset(offset));
    public static Code IfNull(short offset) => new(OperationCode.IFNULL, Operand.BranchOffset(offset));

    // Local Variable Increment
    public static Code IInc(ushort index, short increment)
        => new(OperationCode.IINC, Operand.Iinc(index, increment));

    // Return Instructions
    public static Code ReturnVoid() => new(OperationCode.RETURN);
    public static Code IReturn() => new(OperationCode.IRETURN);
    public static Code LReturn() => new(OperationCode.LRETURN);
    public static Code AReturn() => new(OperationCode.ARETURN);
}
