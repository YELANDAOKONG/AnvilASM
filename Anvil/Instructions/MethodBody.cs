using Anvil.Structures;
using Anvil.Structures.Attributes;
using Anvil.Structures.Attributes.Code;
using Anvil.Structures.ConstantPool;
using Anvil.Types;

namespace Anvil.Instructions;

public class MethodBody
{
    public int MaxStack { get; set; }
    public int MaxLocals { get; set; }

    public List<Instruction> Instructions { get; set; } = [];
    public List<TryCatchBlock> TryCatchBlocks { get; set; } = [];
    public List<AttributeInfo> Attributes { get; set; } = [];

    private readonly Dictionary<Label, int> _labelOffsets = new();

    public void ResolveLabels()
    {
        _labelOffsets.Clear();

        var pc = 0;
        foreach (var instruction in Instructions)
        {
            instruction.Offset = pc;
            pc += instruction.GetSize();

            foreach (var label in instruction.Labels)
            {
                label.Offset = instruction.Offset;
                _labelOffsets[label] = instruction.Offset.Value;
            }
        }

        foreach (var instruction in Instructions)
        {
            switch (instruction)
            {
                case JumpInstruction jump:
                {
                    var targetOffset = GetLabelOffset(jump.Target);
                    jump.BranchOffset = targetOffset - jump.Offset!.Value;
                    break;
                }
                case TableSwitchInstruction table:
                {
                    table.DefaultOffset = GetLabelOffset(table.DefaultTarget) - table.Offset!.Value;
                    table.TargetOffsets = table.Targets
                        .Select(t => GetLabelOffset(t) - table.Offset!.Value)
                        .ToList();
                    break;
                }
                case LookupSwitchInstruction lookup:
                {
                    lookup.DefaultOffset = GetLabelOffset(lookup.DefaultTarget) - lookup.Offset!.Value;
                    lookup.ResolvedPairs = lookup.Pairs
                        .Select(p => (p.Key, GetLabelOffset(p.Target) - lookup.Offset!.Value))
                        .ToList();
                    break;
                }
            }
        }
    }

    public void WriteBytecode(Stream stream)
    {
        foreach (var instruction in Instructions)
        {
            instruction.Write(stream);
        }
    }

    public CodeAttribute ToCodeAttribute(ConstantPoolBuilder cp)
    {
        ResolveCpReferences(cp);
        ResolveLabels();

        using var codeStream = new MemoryStream();
        WriteBytecode(codeStream);
        var codeBytes = codeStream.ToArray();

        var exceptionTable = new ExceptionTableEntry[TryCatchBlocks.Count];
        for (var i = 0; i < TryCatchBlocks.Count; i++)
        {
            var block = TryCatchBlocks[i];
            exceptionTable[i] = new ExceptionTableEntry
            {
                StartPc = new TUShort((ushort)GetLabelOffset(block.Start)),
                EndPc = new TUShort((ushort)GetLabelOffset(block.End)),
                HandlerPc = new TUShort((ushort)GetLabelOffset(block.Handler)),
                CatchType = new TUShort(0) // TODO: resolve CatchType to CP index via builder
            };
        }

        return new CodeAttribute
        {
            MaxStack = new TUShort((ushort)MaxStack),
            MaxLocals = new TUShort((ushort)MaxLocals),
            Code = codeBytes,
            ExceptionTable = exceptionTable,
            Attributes = Attributes.ToArray()
        };
    }

    private void ResolveCpReferences(ConstantPoolBuilder cp)
    {
        foreach (var instruction in Instructions)
        {
            switch (instruction)
            {
                case FieldInstruction field:
                    field.Resolve(cp);
                    break;
                case MethodInstruction method:
                    method.Resolve(cp);
                    break;
                case TypeInstruction type:
                    type.Resolve(cp);
                    break;
                case LdcInstruction ldc:
                    ldc.Resolve(cp);
                    break;
                case MultiANewArrayInstruction multi:
                    multi.Resolve(cp);
                    break;
            }
        }
    }

    public static MethodBody FromCodeAttribute(CodeAttribute attr, CpInfo[] constantPool)
    {
        var body = new MethodBody
        {
            MaxStack = attr.MaxStack.Value,
            MaxLocals = attr.MaxLocals.Value,
            Attributes = [.. attr.Attributes]
        };

        var code = attr.Code;
        var pcToInstruction = new Dictionary<int, Instruction>();
        var branchRecords = new List<(Instruction Source, int SourcePc, int TargetPc, bool IsSwitch, Label? ExistingLabel)>();
        var labelByPc = new Dictionary<int, Label>();

        var pc = 0;
        while (pc < code.Length)
        {
            var opc = (OperationCode)code[pc];
            var insnStartPc = pc;
            pc++;

            switch (opc)
            {
                case OperationCode.TABLESWITCH:
                {
                    var padding = (4 - pc % 4) % 4;
                    pc += padding;

                    var defaultOffset = ReadInt32(code, pc);
                    pc += 4;
                    var low = ReadInt32(code, pc);
                    pc += 4;
                    var high = ReadInt32(code, pc);
                    pc += 4;

                    var count = high - low + 1;
                    var targetOffsets = new int[count];
                    for (var i = 0; i < count; i++)
                    {
                        targetOffsets[i] = ReadInt32(code, pc);
                        pc += 4;
                    }

                    var defaultLabel = GetOrCreateLabel(labelByPc, insnStartPc + defaultOffset);
                    var targetLabels = targetOffsets
                        .Select(t => GetOrCreateLabel(labelByPc, insnStartPc + t))
                        .ToList();

                    var insn = new TableSwitchInstruction(low, high, defaultLabel, targetLabels);
                    body.Instructions.Add(insn);
                    pcToInstruction[insnStartPc] = insn;
                    break;
                }

                case OperationCode.LOOKUPSWITCH:
                {
                    var padding = (4 - pc % 4) % 4;
                    pc += padding;

                    var defaultOffset = ReadInt32(code, pc);
                    pc += 4;
                    var npairs = ReadInt32(code, pc);
                    pc += 4;

                    var pairs = new List<(int Key, Label Target)>(npairs);
                    for (var i = 0; i < npairs; i++)
                    {
                        var key = ReadInt32(code, pc);
                        pc += 4;
                        var offset = ReadInt32(code, pc);
                        pc += 4;
                        pairs.Add((key, GetOrCreateLabel(labelByPc, insnStartPc + offset)));
                    }

                    var defaultLabel = GetOrCreateLabel(labelByPc, insnStartPc + defaultOffset);
                    var insn = new LookupSwitchInstruction(defaultLabel, pairs);
                    body.Instructions.Add(insn);
                    pcToInstruction[insnStartPc] = insn;
                    break;
                }

                case OperationCode.WIDE:
                {
                    var wideOpc = (OperationCode)code[pc];
                    pc++;

                    if (wideOpc == OperationCode.IINC)
                    {
                        var index = ReadUInt16(code, pc);
                        pc += 2;
                        var increment = ReadInt16(code, pc);
                        pc += 2;
                        var insn = new IincInstruction(index, increment);
                        body.Instructions.Add(insn);
                        pcToInstruction[insnStartPc] = insn;
                    }
                    else
                    {
                        var index = ReadUInt16(code, pc);
                        pc += 2;
                        var insn = new VarInstruction(wideOpc, index);
                        body.Instructions.Add(insn);
                        pcToInstruction[insnStartPc] = insn;
                    }

                    break;
                }

                case OperationCode.GOTO_W or OperationCode.JSR_W:
                {
                    var offset = ReadInt32(code, pc);
                    pc += 4;
                    var targetPc = insnStartPc + offset;
                    var label = GetOrCreateLabel(labelByPc, targetPc);
                    var insn = new JumpInstruction(opc, label);
                    body.Instructions.Add(insn);
                    pcToInstruction[insnStartPc] = insn;
                    break;
                }

                // 2-byte instructions (opcode + 1-byte operand)
                case OperationCode.BIPUSH:
                {
                    var value = (sbyte)code[pc];
                    pc++;
                    body.Instructions.Add(new IntInstruction(opc, value));
                    break;
                }

                case OperationCode.NEWARRAY:
                {
                    var atype = code[pc];
                    pc++;
                    body.Instructions.Add(new IntInstruction(opc, atype));
                    break;
                }

                case OperationCode.LDC:
                {
                    var index = code[pc];
                    pc++;
                    var insn = CreateLdcInstruction(constantPool, index, false);
                    body.Instructions.Add(insn);
                    break;
                }

                // 3-byte instructions (opcode + 2-byte operand)
                case OperationCode.SIPUSH:
                {
                    var value = ReadInt16(code, pc);
                    pc += 2;
                    body.Instructions.Add(new IntInstruction(opc, value));
                    break;
                }

                case OperationCode.LDC_W or OperationCode.LDC2_W:
                {
                    var index = ReadUInt16(code, pc);
                    pc += 2;
                    var isWide2 = opc == OperationCode.LDC2_W;
                    var insn = CreateLdcInstruction(constantPool, index, isWide2);
                    body.Instructions.Add(insn);
                    break;
                }

                case OperationCode.ILOAD or OperationCode.LLOAD or OperationCode.FLOAD
                    or OperationCode.DLOAD or OperationCode.ALOAD
                    or OperationCode.ISTORE or OperationCode.LSTORE or OperationCode.FSTORE
                    or OperationCode.DSTORE or OperationCode.ASTORE
                    or OperationCode.RET:
                {
                    var index = code[pc];
                    pc++;
                    body.Instructions.Add(new VarInstruction(opc, index));
                    break;
                }

                case OperationCode.IINC:
                {
                    var index = code[pc];
                    pc++;
                    var increment = (sbyte)code[pc];
                    pc++;
                    body.Instructions.Add(new IincInstruction(index, increment));
                    break;
                }

                // 3-byte field/method/type instructions
                case OperationCode.GETSTATIC or OperationCode.PUTSTATIC
                    or OperationCode.GETFIELD or OperationCode.PUTFIELD:
                {
                    var index = ReadUInt16(code, pc);
                    pc += 2;
                    var (owner, name, desc) = ResolveFieldRef(constantPool, index);
                    body.Instructions.Add(new FieldInstruction(opc, owner, name, desc));
                    break;
                }

                case OperationCode.INVOKEVIRTUAL or OperationCode.INVOKESPECIAL
                    or OperationCode.INVOKESTATIC:
                {
                    var index = ReadUInt16(code, pc);
                    pc += 2;
                    var (owner, name, desc) = ResolveMethodRef(constantPool, index);
                    body.Instructions.Add(new MethodInstruction(opc, owner, name, desc));
                    break;
                }

                case OperationCode.INVOKEINTERFACE:
                {
                    var index = ReadUInt16(code, pc);
                    pc += 2;
                    var count = code[pc];
                    pc++;
                    pc++; // skip zero byte
                    var (owner, name, desc) = ResolveInterfaceMethodRef(constantPool, index);
                    body.Instructions.Add(new MethodInstruction(opc, owner, name, desc) { Count = count });
                    break;
                }

                case OperationCode.INVOKEDYNAMIC:
                {
                    var index = ReadUInt16(code, pc);
                    pc += 2;
                    pc += 2; // skip zero bytes
                    var insn = new InvokeDynamicInstruction(index);
                    body.Instructions.Add(insn);
                    break;
                }

                case OperationCode.NEW or OperationCode.ANEWARRAY
                    or OperationCode.CHECKCAST or OperationCode.INSTANCEOF:
                {
                    var index = ReadUInt16(code, pc);
                    pc += 2;
                    var typeName = ResolveClassName(constantPool, index);
                    body.Instructions.Add(new TypeInstruction(opc, typeName));
                    break;
                }

                case OperationCode.MULTIANEWARRAY:
                {
                    var index = ReadUInt16(code, pc);
                    pc += 2;
                    var dims = code[pc];
                    pc++;
                    var typeName = ResolveClassName(constantPool, index);
                    body.Instructions.Add(new MultiANewArrayInstruction(typeName, dims));
                    break;
                }

                // 3-byte branch instructions (opcode + 2-byte signed offset)
                case OperationCode.IFEQ or OperationCode.IFNE or OperationCode.IFLT
                    or OperationCode.IFGE or OperationCode.IFGT or OperationCode.IFLE
                    or OperationCode.IF_ICMPEQ or OperationCode.IF_ICMPNE
                    or OperationCode.IF_ICMPLT or OperationCode.IF_ICMPGE
                    or OperationCode.IF_ICMPGT or OperationCode.IF_ICMPLE
                    or OperationCode.IF_ACMPEQ or OperationCode.IF_ACMPNE
                    or OperationCode.GOTO or OperationCode.JSR
                    or OperationCode.IFNULL or OperationCode.IFNONNULL:
                {
                    var offset = ReadInt16(code, pc);
                    pc += 2;
                    var targetPc = insnStartPc + offset;
                    var label = GetOrCreateLabel(labelByPc, targetPc);
                    body.Instructions.Add(new JumpInstruction(opc, label));
                    break;
                }

                default:
                {
                    body.Instructions.Add(new InsnInstruction(opc));
                    break;
                }
            }
        }

        // Attach labels to their target instructions
        foreach (var (targetPc, label) in labelByPc)
        {
            if (pcToInstruction.TryGetValue(targetPc, out var insn))
            {
                insn.Labels.Add(label);
            }
        }

        // Parse exception table
        foreach (var exEntry in attr.ExceptionTable)
        {
            var startLabel = GetOrCreateLabel(labelByPc, exEntry.StartPc.Value);
            var endLabel = GetOrCreateLabel(labelByPc, exEntry.EndPc.Value);
            var handlerLabel = GetOrCreateLabel(labelByPc, exEntry.HandlerPc.Value);

            string? catchType = null;
            if (exEntry.CatchType.Value != 0)
            {
                catchType = ResolveClassName(constantPool, exEntry.CatchType.Value);
            }

            body.TryCatchBlocks.Add(new TryCatchBlock(startLabel, endLabel, handlerLabel, catchType));
        }

        return body;
    }

    private static Label GetOrCreateLabel(Dictionary<int, Label> map, int pc)
    {
        if (map.TryGetValue(pc, out var label))
        {
            return label;
        }

        label = new Label();
        map[pc] = label;
        return label;
    }

    private static Instruction CreateLdcInstruction(CpInfo[] constantPool, int index, bool isWide2)
    {
        if (index <= 0 || index >= constantPool.Length)
        {
            return new LdcInstruction(isWide2 ? OperationCode.LDC2_W : OperationCode.LDC_W, index);
        }

        var entry = constantPool[index];
        switch (entry)
        {
            case CpInteger intEntry:
                return new LdcInstruction(intEntry.Bytes.Value);

            case CpFloat floatEntry:
                return new LdcInstruction(floatEntry.Bytes.Value);

            case CpString strEntry:
            {
                var utf8 = (CpUtf8)constantPool[strEntry.StringIndex.Value];
                return new LdcInstruction(utf8.Value);
            }

            case CpLong longEntry:
                return new LdcInstruction(longEntry.Bytes.Value);

            case CpDouble doubleEntry:
                return new LdcInstruction(doubleEntry.Bytes.Value);

            default:
                return new LdcInstruction(isWide2 ? OperationCode.LDC2_W : OperationCode.LDC_W, index);
        }
    }

    private static string ResolveClassName(CpInfo[] constantPool, int classIndex)
    {
        if (classIndex <= 0 || classIndex >= constantPool.Length)
        {
            return $"<unknown #{classIndex}>";
        }

        var classEntry = (CpClass)constantPool[classIndex];
        var utf8 = (CpUtf8)constantPool[classEntry.NameIndex.Value];
        return utf8.Value;
    }

    private static (string Owner, string Name, string Descriptor) ResolveFieldRef(CpInfo[] constantPool, int index)
    {
        var fieldRef = (CpFieldRef)constantPool[index];
        var className = ResolveClassName(constantPool, fieldRef.ClassIndex.Value);
        var (name, descriptor) = ResolveNameAndType(constantPool, fieldRef.NameAndTypeIndex.Value);
        return (className, name, descriptor);
    }

    private static (string Owner, string Name, string Descriptor) ResolveMethodRef(CpInfo[] constantPool, int index)
    {
        var methodRef = (CpMethodRef)constantPool[index];
        var className = ResolveClassName(constantPool, methodRef.ClassIndex.Value);
        var (name, descriptor) = ResolveNameAndType(constantPool, methodRef.NameAndTypeIndex.Value);
        return (className, name, descriptor);
    }

    private static (string Owner, string Name, string Descriptor) ResolveInterfaceMethodRef(CpInfo[] constantPool, int index)
    {
        var methodRef = (CpInterfaceMethodRef)constantPool[index];
        var className = ResolveClassName(constantPool, methodRef.ClassIndex.Value);
        var (name, descriptor) = ResolveNameAndType(constantPool, methodRef.NameAndTypeIndex.Value);
        return (className, name, descriptor);
    }

    private static (string Name, string Descriptor) ResolveNameAndType(CpInfo[] constantPool, int index)
    {
        var nat = (CpNameAndType)constantPool[index];
        var name = ((CpUtf8)constantPool[nat.NameIndex.Value]).Value;
        var descriptor = ((CpUtf8)constantPool[nat.DescriptorIndex.Value]).Value;
        return (name, descriptor);
    }

    private static short ReadInt16(byte[] data, int offset)
    {
        return (short)((data[offset] << 8) | data[offset + 1]);
    }

    private static int ReadUInt16(byte[] data, int offset)
    {
        return (data[offset] << 8) | data[offset + 1];
    }

    private static int ReadInt32(byte[] data, int offset)
    {
        return (data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3];
    }

    private int GetLabelOffset(Label label)
    {
        if (_labelOffsets.TryGetValue(label, out var offset))
        {
            return offset;
        }

        if (label.Offset.HasValue)
        {
            return label.Offset.Value;
        }

        throw new InvalidOperationException($"Label '{label}' has not been resolved.");
    }
}
