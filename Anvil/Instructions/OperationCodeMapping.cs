namespace Anvil.Instructions;

public static class OperationCodeMapping
{
    private static readonly OperandDefinition[] NoOperands = Array.Empty<OperandDefinition>();
    
    private static InstructionInfo ZeroOp() => new(NoOperands);
    private static InstructionInfo SingleOp(int size, OperandType type) => new(new[] { new OperandDefinition(size, type) });

    private static readonly Dictionary<OperationCode, InstructionInfo> Mapping = new()
    {
        // Zero operand instructions
        [OperationCode.NOP] = ZeroOp(),
        [OperationCode.ACONST_NULL] = ZeroOp(),
        [OperationCode.ICONST_M1] = ZeroOp(),
        [OperationCode.ICONST_0] = ZeroOp(),
        [OperationCode.ICONST_1] = ZeroOp(),
        [OperationCode.ICONST_2] = ZeroOp(),
        [OperationCode.ICONST_3] = ZeroOp(),
        [OperationCode.ICONST_4] = ZeroOp(),
        [OperationCode.ICONST_5] = ZeroOp(),
        [OperationCode.LCONST_0] = ZeroOp(),
        [OperationCode.LCONST_1] = ZeroOp(),
        [OperationCode.FCONST_0] = ZeroOp(),
        [OperationCode.FCONST_1] = ZeroOp(),
        [OperationCode.FCONST_2] = ZeroOp(),
        [OperationCode.DCONST_0] = ZeroOp(),
        [OperationCode.DCONST_1] = ZeroOp(),
        [OperationCode.IALOAD] = ZeroOp(),
        [OperationCode.LALOAD] = ZeroOp(),
        [OperationCode.FALOAD] = ZeroOp(),
        [OperationCode.DALOAD] = ZeroOp(),
        [OperationCode.AALOAD] = ZeroOp(),
        [OperationCode.BALOAD] = ZeroOp(),
        [OperationCode.CALOAD] = ZeroOp(),
        [OperationCode.SALOAD] = ZeroOp(),
        [OperationCode.IASTORE] = ZeroOp(),
        [OperationCode.LASTORE] = ZeroOp(),
        [OperationCode.FASTORE] = ZeroOp(),
        [OperationCode.DASTORE] = ZeroOp(),
        [OperationCode.AASTORE] = ZeroOp(),
        [OperationCode.BASTORE] = ZeroOp(),
        [OperationCode.CASTORE] = ZeroOp(),
        [OperationCode.SASTORE] = ZeroOp(),
        [OperationCode.POP] = ZeroOp(),
        [OperationCode.POP2] = ZeroOp(),
        [OperationCode.DUP] = ZeroOp(),
        [OperationCode.DUP_X1] = ZeroOp(),
        [OperationCode.DUP_X2] = ZeroOp(),
        [OperationCode.DUP2] = ZeroOp(),
        [OperationCode.DUP2_X1] = ZeroOp(),
        [OperationCode.DUP2_X2] = ZeroOp(),
        [OperationCode.SWAP] = ZeroOp(),
        [OperationCode.IADD] = ZeroOp(),
        [OperationCode.LADD] = ZeroOp(),
        [OperationCode.FADD] = ZeroOp(),
        [OperationCode.DADD] = ZeroOp(),
        [OperationCode.ISUB] = ZeroOp(),
        [OperationCode.LSUB] = ZeroOp(),
        [OperationCode.FSUB] = ZeroOp(),
        [OperationCode.DSUB] = ZeroOp(),
        [OperationCode.IMUL] = ZeroOp(),
        [OperationCode.LMUL] = ZeroOp(),
        [OperationCode.FMUL] = ZeroOp(),
        [OperationCode.DMUL] = ZeroOp(),
        [OperationCode.IDIV] = ZeroOp(),
        [OperationCode.LDIV] = ZeroOp(),
        [OperationCode.FDIV] = ZeroOp(),
        [OperationCode.DDIV] = ZeroOp(),
        [OperationCode.IREM] = ZeroOp(),
        [OperationCode.LREM] = ZeroOp(),
        [OperationCode.FREM] = ZeroOp(),
        [OperationCode.DREM] = ZeroOp(),
        [OperationCode.INEG] = ZeroOp(),
        [OperationCode.LNEG] = ZeroOp(),
        [OperationCode.FNEG] = ZeroOp(),
        [OperationCode.DNEG] = ZeroOp(),
        [OperationCode.ISHL] = ZeroOp(),
        [OperationCode.LSHL] = ZeroOp(),
        [OperationCode.ISHR] = ZeroOp(),
        [OperationCode.LSHR] = ZeroOp(),
        [OperationCode.IUSHR] = ZeroOp(),
        [OperationCode.LUSHR] = ZeroOp(),
        [OperationCode.IAND] = ZeroOp(),
        [OperationCode.LAND] = ZeroOp(),
        [OperationCode.IOR] = ZeroOp(),
        [OperationCode.LOR] = ZeroOp(),
        [OperationCode.IXOR] = ZeroOp(),
        [OperationCode.LXOR] = ZeroOp(),
        [OperationCode.I2L] = ZeroOp(),
        [OperationCode.I2F] = ZeroOp(),
        [OperationCode.I2D] = ZeroOp(),
        [OperationCode.L2I] = ZeroOp(),
        [OperationCode.L2F] = ZeroOp(),
        [OperationCode.L2D] = ZeroOp(),
        [OperationCode.F2I] = ZeroOp(),
        [OperationCode.F2L] = ZeroOp(),
        [OperationCode.F2D] = ZeroOp(),
        [OperationCode.D2I] = ZeroOp(),
        [OperationCode.D2L] = ZeroOp(),
        [OperationCode.D2F] = ZeroOp(),
        [OperationCode.I2B] = ZeroOp(),
        [OperationCode.I2C] = ZeroOp(),
        [OperationCode.I2S] = ZeroOp(),
        [OperationCode.LCMP] = ZeroOp(),
        [OperationCode.FCMPL] = ZeroOp(),
        [OperationCode.FCMPG] = ZeroOp(),
        [OperationCode.DCMPL] = ZeroOp(),
        [OperationCode.DCMPG] = ZeroOp(),
        [OperationCode.ARRAYLENGTH] = ZeroOp(),
        [OperationCode.ATHROW] = ZeroOp(),
        [OperationCode.MONITORENTER] = ZeroOp(),
        [OperationCode.MONITOREXIT] = ZeroOp(),
        [OperationCode.IRETURN] = ZeroOp(),
        [OperationCode.LRETURN] = ZeroOp(),
        [OperationCode.FRETURN] = ZeroOp(),
        [OperationCode.DRETURN] = ZeroOp(),
        [OperationCode.ARETURN] = ZeroOp(),
        [OperationCode.RETURN] = ZeroOp(),

        // Reserved Instructions
        [OperationCode.BREAKPOINT] = ZeroOp(),
        [OperationCode.IMPDEP1] = ZeroOp(),
        [OperationCode.IMPDEP2] = ZeroOp(),

        // Single operand instructions
        [OperationCode.BIPUSH] = SingleOp(1, OperandType.ByteImmediate),
        [OperationCode.SIPUSH] = SingleOp(2, OperandType.ShortImmediate),
        [OperationCode.LDC] = SingleOp(1, OperandType.ConstantPoolIndex),
        [OperationCode.LDC_W] = SingleOp(2, OperandType.ConstantPoolIndex),
        [OperationCode.LDC2_W] = SingleOp(2, OperandType.ConstantPoolIndex),
        
        // Local Variables (Can be Wide)
        [OperationCode.ILOAD] = SingleOp(1, OperandType.LocalIndex).WithWide(),
        [OperationCode.LLOAD] = SingleOp(1, OperandType.LocalIndex).WithWide(),
        [OperationCode.FLOAD] = SingleOp(1, OperandType.LocalIndex).WithWide(),
        [OperationCode.DLOAD] = SingleOp(1, OperandType.LocalIndex).WithWide(),
        [OperationCode.ALOAD] = SingleOp(1, OperandType.LocalIndex).WithWide(),
        [OperationCode.ISTORE] = SingleOp(1, OperandType.LocalIndex).WithWide(),
        [OperationCode.LSTORE] = SingleOp(1, OperandType.LocalIndex).WithWide(),
        [OperationCode.FSTORE] = SingleOp(1, OperandType.LocalIndex).WithWide(),
        [OperationCode.DSTORE] = SingleOp(1, OperandType.LocalIndex).WithWide(),
        [OperationCode.ASTORE] = SingleOp(1, OperandType.LocalIndex).WithWide(),
        [OperationCode.RET] = SingleOp(1, OperandType.LocalIndex).WithWide(),
        
        // IINC (Special case: 2 operands)
        [OperationCode.IINC] = new InstructionInfo(new[] 
        { 
            new OperandDefinition(1, OperandType.LocalIndex), 
            new OperandDefinition(1, OperandType.ByteImmediate) // Treated as immediate for parsing
        }, canBeWide: true),

        [OperationCode.NEWARRAY] = SingleOp(1, OperandType.NewArrayAtype),
        [OperationCode.NEW] = SingleOp(2, OperandType.ConstantPoolIndex),
        [OperationCode.ANEWARRAY] = SingleOp(2, OperandType.ConstantPoolIndex),
        [OperationCode.CHECKCAST] = SingleOp(2, OperandType.ConstantPoolIndex),
        [OperationCode.INSTANCEOF] = SingleOp(2, OperandType.ConstantPoolIndex),
        [OperationCode.GETSTATIC] = SingleOp(2, OperandType.ConstantPoolIndex),
        [OperationCode.PUTSTATIC] = SingleOp(2, OperandType.ConstantPoolIndex),
        [OperationCode.GETFIELD] = SingleOp(2, OperandType.ConstantPoolIndex),
        [OperationCode.PUTFIELD] = SingleOp(2, OperandType.ConstantPoolIndex),
        [OperationCode.INVOKEVIRTUAL] = SingleOp(2, OperandType.ConstantPoolIndex),
        [OperationCode.INVOKESPECIAL] = SingleOp(2, OperandType.ConstantPoolIndex),
        [OperationCode.INVOKESTATIC] = SingleOp(2, OperandType.ConstantPoolIndex),
        
        // Dynamic Invocation
        [OperationCode.INVOKEDYNAMIC] = new InstructionInfo(new[]
        {
            new OperandDefinition(4, OperandType.InvokeDynamicArgs) 
        }),

        // Branching
        [OperationCode.IFEQ] = SingleOp(2, OperandType.BranchOffset),
        [OperationCode.IFNE] = SingleOp(2, OperandType.BranchOffset),
        [OperationCode.IFLT] = SingleOp(2, OperandType.BranchOffset),
        [OperationCode.IFGE] = SingleOp(2, OperandType.BranchOffset),
        [OperationCode.IFGT] = SingleOp(2, OperandType.BranchOffset),
        [OperationCode.IFLE] = SingleOp(2, OperandType.BranchOffset),
        [OperationCode.IF_ICMPEQ] = SingleOp(2, OperandType.BranchOffset),
        [OperationCode.IF_ICMPNE] = SingleOp(2, OperandType.BranchOffset),
        [OperationCode.IF_ICMPLT] = SingleOp(2, OperandType.BranchOffset),
        [OperationCode.IF_ICMPGE] = SingleOp(2, OperandType.BranchOffset),
        [OperationCode.IF_ICMPGT] = SingleOp(2, OperandType.BranchOffset),
        [OperationCode.IF_ICMPLE] = SingleOp(2, OperandType.BranchOffset),
        [OperationCode.IF_ACMPEQ] = SingleOp(2, OperandType.BranchOffset),
        [OperationCode.IF_ACMPNE] = SingleOp(2, OperandType.BranchOffset),
        [OperationCode.IFNULL] = SingleOp(2, OperandType.BranchOffset),
        [OperationCode.IFNONNULL] = SingleOp(2, OperandType.BranchOffset),
        [OperationCode.GOTO] = SingleOp(2, OperandType.BranchOffset),
        [OperationCode.JSR] = SingleOp(2, OperandType.BranchOffset),
        [OperationCode.GOTO_W] = SingleOp(4, OperandType.BranchOffset),
        [OperationCode.JSR_W] = SingleOp(4, OperandType.BranchOffset),

        // Special multiple operands
        [OperationCode.INVOKEINTERFACE] = new InstructionInfo(new[]
        {
            // Treated as one 4-byte operand to ensure correct serialization of the 'count' and '0' bytes
            new OperandDefinition(4, OperandType.InvokeInterfaceArgs)
        }),
        
        [OperationCode.MULTIANEWARRAY] = new InstructionInfo(new[]
        {
            new OperandDefinition(2, OperandType.ConstantPoolIndex),
            new OperandDefinition(1, OperandType.ByteImmediate) // dimensions
        }),
        
        // Switches (Variable length, handled specially)
        [OperationCode.TABLESWITCH] = new InstructionInfo(Array.Empty<OperandDefinition>()),
        [OperationCode.LOOKUPSWITCH] = new InstructionInfo(Array.Empty<OperandDefinition>()),
    };

    public readonly struct OperandDefinition
    {
        public int Size { get; }
        public OperandType Type { get; }

        public OperandDefinition(int size, OperandType type)
        {
            Size = size;
            Type = type;
        }
    }

    public class InstructionInfo
    {
        public OperandDefinition[] Operands { get; }
        public bool CanBeWide { get; private set; }

        public InstructionInfo(OperandDefinition[] operands, bool canBeWide = false)
        {
            Operands = operands;
            CanBeWide = canBeWide;
        }

        public InstructionInfo WithWide()
        {
            CanBeWide = true;
            return this;
        }
    }

    public static bool TryGetInfo(OperationCode op, out InstructionInfo? info)
        => Mapping.TryGetValue(op, out info);
}
