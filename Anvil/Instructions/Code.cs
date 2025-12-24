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

        // Automatic WIDE prefix detection
        if (OperationCodeMapping.TryGetInfo(opCode, out var info) && info!.CanBeWide)
        {
            // Check specific conditions that require WIDE:
            
            // 1. Local Variable Access (Load/Store/Ret): Index > 255
            // Standard operand is 1 byte. If we have 2 bytes of data for the index, it's wide.
            bool isWideLocal = Operands.Any(o => o.Type == OperandType.LocalIndex && o.Data.Length == 2);

            // 2. IINC: Index > 255 OR Increment value not in sbyte range
            // Standard IINC is 2 bytes (1 index + 1 const). Wide is 4 bytes (2 index + 2 const).
            bool isWideIinc = Operands.Any(o => o.Type == OperandType.IincPair && o.Data.Length == 4);

            if (isWideLocal || isWideIinc)
            {
                WidePrefix = OperationCode.WIDE;
            }
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
            
            // Avoid printing massive binary blobs for switch instructions in debug output
            if (OpCode == OperationCode.TABLESWITCH || OpCode == OperationCode.LOOKUPSWITCH)
            {
                sb.Append("[Switch Data]");
            }
            else
            {
                sb.Append(string.Join(" ", Operands.Select(o => o.ToString())));
            }
        }

        return sb.ToString();
    }

    // ========================================================================
    // Factory Methods
    // ========================================================================

    // --- Constants ---

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
            _ => throw new ArgumentOutOfRangeException(nameof(value), "Value too large for immediate push. Use Ldc.")
        };
    }

    public static Code PushLong(long value)
    {
        if (value == 0) return new(OperationCode.LCONST_0);
        if (value == 1) return new(OperationCode.LCONST_1);
        throw new ArgumentOutOfRangeException(nameof(value), "Use Ldc2_w for arbitrary longs.");
    }

    public static Code PushFloat(float value)
    {
        if (value == 0f) return new(OperationCode.FCONST_0);
        if (value == 1f) return new(OperationCode.FCONST_1);
        if (value == 2f) return new(OperationCode.FCONST_2);
        throw new ArgumentOutOfRangeException(nameof(value), "Use Ldc for arbitrary floats.");
    }

    public static Code PushDouble(double value)
    {
        if (value == 0.0) return new(OperationCode.DCONST_0);
        if (value == 1.0) return new(OperationCode.DCONST_1);
        throw new ArgumentOutOfRangeException(nameof(value), "Use Ldc2_w for arbitrary doubles.");
    }

    public static Code Ldc(ushort cpIndex)
    {
        if (cpIndex <= 0xFF)
            return new(OperationCode.LDC, Operand.ConstantPoolIndex((byte)cpIndex));
        return new(OperationCode.LDC_W, Operand.ConstantPoolIndex(cpIndex));
    }

    public static Code Ldc2(ushort cpIndex) 
        => new(OperationCode.LDC2_W, Operand.ConstantPoolIndex(cpIndex));

    // --- Local Variables (Load) ---

    public static Code ILoad(ushort index)
        => new(OperationCode.ILOAD, index <= 0xFF ? Operand.LocalIndex((byte)index) : Operand.WideLocalIndex(index));

    public static Code LLoad(ushort index)
        => new(OperationCode.LLOAD, index <= 0xFF ? Operand.LocalIndex((byte)index) : Operand.WideLocalIndex(index));

    public static Code FLoad(ushort index)
        => new(OperationCode.FLOAD, index <= 0xFF ? Operand.LocalIndex((byte)index) : Operand.WideLocalIndex(index));

    public static Code DLoad(ushort index)
        => new(OperationCode.DLOAD, index <= 0xFF ? Operand.LocalIndex((byte)index) : Operand.WideLocalIndex(index));

    public static Code ALoad(ushort index)
        => new(OperationCode.ALOAD, index <= 0xFF ? Operand.LocalIndex((byte)index) : Operand.WideLocalIndex(index));

    // --- Local Variables (Store) ---

    public static Code IStore(ushort index)
        => new(OperationCode.ISTORE, index <= 0xFF ? Operand.LocalIndex((byte)index) : Operand.WideLocalIndex(index));

    public static Code LStore(ushort index)
        => new(OperationCode.LSTORE, index <= 0xFF ? Operand.LocalIndex((byte)index) : Operand.WideLocalIndex(index));

    public static Code FStore(ushort index)
        => new(OperationCode.FSTORE, index <= 0xFF ? Operand.LocalIndex((byte)index) : Operand.WideLocalIndex(index));

    public static Code DStore(ushort index)
        => new(OperationCode.DSTORE, index <= 0xFF ? Operand.LocalIndex((byte)index) : Operand.WideLocalIndex(index));

    public static Code AStore(ushort index)
        => new(OperationCode.ASTORE, index <= 0xFF ? Operand.LocalIndex((byte)index) : Operand.WideLocalIndex(index));

    // --- Stack Management ---

    public static Code Pop() => new(OperationCode.POP);
    public static Code Pop2() => new(OperationCode.POP2);
    public static Code Dup() => new(OperationCode.DUP);
    public static Code Swap() => new(OperationCode.SWAP);

    // --- Arithmetic & Logic ---

    public static Code IAdd() => new(OperationCode.IADD);
    public static Code LAdd() => new(OperationCode.LADD);
    public static Code FAdd() => new(OperationCode.FADD);
    public static Code DAdd() => new(OperationCode.DADD);
    
    public static Code IInc(ushort index, short increment)
        => new(OperationCode.IINC, Operand.Iinc(index, increment));

    // --- Type Conversion ---

    public static Code I2L() => new(OperationCode.I2L);
    public static Code I2F() => new(OperationCode.I2F);
    public static Code L2I() => new(OperationCode.L2I);

    // --- Control Flow (Branching) ---

    public static Code Goto(short offset) => new(OperationCode.GOTO, Operand.BranchOffset(offset));
    public static Code GotoW(int offset) => new(OperationCode.GOTO_W, Operand.WideBranchOffset(offset));
    
    public static Code IfEq(short offset) => new(OperationCode.IFEQ, Operand.BranchOffset(offset));
    public static Code IfNe(short offset) => new(OperationCode.IFNE, Operand.BranchOffset(offset));
    public static Code IfLt(short offset) => new(OperationCode.IFLT, Operand.BranchOffset(offset));
    public static Code IfGe(short offset) => new(OperationCode.IFGE, Operand.BranchOffset(offset));
    public static Code IfGt(short offset) => new(OperationCode.IFGT, Operand.BranchOffset(offset));
    public static Code IfLe(short offset) => new(OperationCode.IFLE, Operand.BranchOffset(offset));
    
    public static Code IfNull(short offset) => new(OperationCode.IFNULL, Operand.BranchOffset(offset));
    public static Code IfNonNull(short offset) => new(OperationCode.IFNONNULL, Operand.BranchOffset(offset));

    // --- Control Flow (Switch) ---

    public static Code TableSwitch(int defaultOffset, int low, int high, int[] offsets)
        => new(OperationCode.TABLESWITCH, Operand.TableSwitch(defaultOffset, low, high, offsets));

    public static Code LookupSwitch(int defaultOffset, (int match, int offset)[] pairs)
        => new(OperationCode.LOOKUPSWITCH, Operand.LookupSwitch(defaultOffset, pairs));

    // --- Method Invocation ---

    public static Code InvokeVirtual(ushort methodIndex)
        => new(OperationCode.INVOKEVIRTUAL, Operand.ConstantPoolIndex(methodIndex));

    public static Code InvokeSpecial(ushort methodIndex)
        => new(OperationCode.INVOKESPECIAL, Operand.ConstantPoolIndex(methodIndex));

    public static Code InvokeStatic(ushort methodIndex)
        => new(OperationCode.INVOKESTATIC, Operand.ConstantPoolIndex(methodIndex));

    public static Code InvokeInterface(ushort methodIndex, byte argsCount)
        => new(OperationCode.INVOKEINTERFACE, Operand.InvokeInterface(methodIndex, argsCount));
    
    public static Code InvokeDynamic(ushort callSiteIndex)
        => new(OperationCode.INVOKEDYNAMIC, Operand.ConstantPoolIndex(callSiteIndex), Operand.LocalIndex(0)); // 0 is reserved byte

    // --- Object & Array ---

    public static Code New(ushort classIndex)
        => new(OperationCode.NEW, Operand.ConstantPoolIndex(classIndex));

    public static Code NewArray(byte atype)
        => new(OperationCode.NEWARRAY, Operand.NewArrayAtype(atype));

    public static Code ANewArray(ushort classIndex)
        => new(OperationCode.ANEWARRAY, Operand.ConstantPoolIndex(classIndex));

    public static Code MultiANewArray(ushort classIndex, byte dimensions)
        => new(OperationCode.MULTIANEWARRAY, Operand.MultiANewArray(classIndex, dimensions));

    public static Code ArrayLength() => new(OperationCode.ARRAYLENGTH);
    public static Code AThrow() => new(OperationCode.ATHROW);
    public static Code CheckCast(ushort classIndex) => new(OperationCode.CHECKCAST, Operand.ConstantPoolIndex(classIndex));
    public static Code InstanceOf(ushort classIndex) => new(OperationCode.INSTANCEOF, Operand.ConstantPoolIndex(classIndex));

    // --- Return ---

    public static Code ReturnVoid() => new(OperationCode.RETURN);
    public static Code IReturn() => new(OperationCode.IRETURN);
    public static Code LReturn() => new(OperationCode.LRETURN);
    public static Code FReturn() => new(OperationCode.FRETURN);
    public static Code DReturn() => new(OperationCode.DRETURN);
    public static Code AReturn() => new(OperationCode.ARETURN);
}
