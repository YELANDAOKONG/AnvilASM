using Anvil.Instructions.ConstantPool;
using Anvil.Instructions.StackMap;
using Anvil.Structures;
using Anvil.Structures.Attributes;
using Anvil.Structures.Attributes.Code;
using Anvil.Structures.ConstantPool;
using Anvil.Structures.Attributes.TypeAnnotations;
using Anvil.Types;

namespace Anvil.Instructions;

public class MethodBody
{
    public int MaxStack { get; set; }
    public int MaxLocals { get; set; }
    public string? MethodDescriptor { get; set; }
    public string? MethodName { get; set; }
    public string? OwnerInternalName { get; set; }
    public Func<string, string, string>? CommonSuperTypeResolver { get; set; }
    public bool IsStatic { get; set; }

    public List<Instruction> Instructions { get; set; } = [];
    public List<TryCatchBlock> TryCatchBlocks { get; set; } = [];
    public List<BytecodeLineNumber> LineNumbers { get; set; } = [];
    public List<BytecodeLocalVariable> LocalVariables { get; set; } = [];
    public List<BytecodeTypeAnnotation> TypeAnnotations { get; set; } = [];
    public List<AttributeInfo> Attributes { get; set; } = [];
    public List<Label> EndLabels { get; } = [];

    private readonly Dictionary<Label, int> _labelOffsets = new();
    private readonly Dictionary<string, int> _namedLabelOffsets = new(StringComparer.Ordinal);

    public void MarkLabel(string name, Instruction instruction)
    {
        ArgumentNullException.ThrowIfNull(instruction);
        instruction.Labels.Add(new Label(name));
    }

    public void MarkEndLabel(string name)
    {
        EndLabels.Add(new Label(name));
    }

    public void Normalize()
    {
        for (var i = 0; i < Instructions.Count; i++)
        {
            var insn = Instructions[i];

            if (insn is not InsnInstruction si)
            {
                continue;
            }

            switch (si.OpCode)
            {
                case OperationCode.ILOAD_0 or OperationCode.ILOAD_1 or OperationCode.ILOAD_2 or OperationCode.ILOAD_3:
                {
                    var index = si.OpCode - OperationCode.ILOAD_0;
                    Instructions[i] = CopyLabels(new VarInstruction(OperationCode.ILOAD, index), si);
                    break;
                }
                case OperationCode.LLOAD_0 or OperationCode.LLOAD_1 or OperationCode.LLOAD_2 or OperationCode.LLOAD_3:
                {
                    var index = si.OpCode - OperationCode.LLOAD_0;
                    Instructions[i] = CopyLabels(new VarInstruction(OperationCode.LLOAD, index), si);
                    break;
                }
                case OperationCode.FLOAD_0 or OperationCode.FLOAD_1 or OperationCode.FLOAD_2 or OperationCode.FLOAD_3:
                {
                    var index = si.OpCode - OperationCode.FLOAD_0;
                    Instructions[i] = CopyLabels(new VarInstruction(OperationCode.FLOAD, index), si);
                    break;
                }
                case OperationCode.DLOAD_0 or OperationCode.DLOAD_1 or OperationCode.DLOAD_2 or OperationCode.DLOAD_3:
                {
                    var index = si.OpCode - OperationCode.DLOAD_0;
                    Instructions[i] = CopyLabels(new VarInstruction(OperationCode.DLOAD, index), si);
                    break;
                }
                case OperationCode.ALOAD_0 or OperationCode.ALOAD_1 or OperationCode.ALOAD_2 or OperationCode.ALOAD_3:
                {
                    var index = si.OpCode - OperationCode.ALOAD_0;
                    Instructions[i] = CopyLabels(new VarInstruction(OperationCode.ALOAD, index), si);
                    break;
                }
                case OperationCode.ISTORE_0 or OperationCode.ISTORE_1 or OperationCode.ISTORE_2 or OperationCode.ISTORE_3:
                {
                    var index = si.OpCode - OperationCode.ISTORE_0;
                    Instructions[i] = CopyLabels(new VarInstruction(OperationCode.ISTORE, index), si);
                    break;
                }
                case OperationCode.LSTORE_0 or OperationCode.LSTORE_1 or OperationCode.LSTORE_2 or OperationCode.LSTORE_3:
                {
                    var index = si.OpCode - OperationCode.LSTORE_0;
                    Instructions[i] = CopyLabels(new VarInstruction(OperationCode.LSTORE, index), si);
                    break;
                }
                case OperationCode.FSTORE_0 or OperationCode.FSTORE_1 or OperationCode.FSTORE_2 or OperationCode.FSTORE_3:
                {
                    var index = si.OpCode - OperationCode.FSTORE_0;
                    Instructions[i] = CopyLabels(new VarInstruction(OperationCode.FSTORE, index), si);
                    break;
                }
                case OperationCode.DSTORE_0 or OperationCode.DSTORE_1 or OperationCode.DSTORE_2 or OperationCode.DSTORE_3:
                {
                    var index = si.OpCode - OperationCode.DSTORE_0;
                    Instructions[i] = CopyLabels(new VarInstruction(OperationCode.DSTORE, index), si);
                    break;
                }
                case OperationCode.ASTORE_0 or OperationCode.ASTORE_1 or OperationCode.ASTORE_2 or OperationCode.ASTORE_3:
                {
                    var index = si.OpCode - OperationCode.ASTORE_0;
                    Instructions[i] = CopyLabels(new VarInstruction(OperationCode.ASTORE, index), si);
                    break;
                }
                case OperationCode.ICONST_M1:
                {
                    Instructions[i] = CopyLabels(new IntInstruction(OperationCode.BIPUSH, -1), si);
                    break;
                }
                case >= OperationCode.ICONST_0 and <= OperationCode.ICONST_5:
                {
                    var value = si.OpCode - OperationCode.ICONST_0;
                    Instructions[i] = CopyLabels(new IntInstruction(OperationCode.BIPUSH, value), si);
                    break;
                }
            }
        }
    }

    private static T CopyLabels<T>(T target, Instruction source) where T : Instruction
    {
        target.Labels.AddRange(source.Labels);
        return target;
    }

    public void ResolveLabels()
    {
        _labelOffsets.Clear();
        _namedLabelOffsets.Clear();

        while (true)
        {
            _labelOffsets.Clear();
            _namedLabelOffsets.Clear();
            var pc = 0;
            foreach (var instruction in Instructions)
            {
                instruction.Offset = pc;
                pc += instruction.GetSize();

                foreach (var label in instruction.Labels)
                {
                    AddLabelOffset(label, instruction.Offset.Value);
                }
            }

            foreach (var label in EndLabels)
            {
                AddLabelOffset(label, pc);
            }

            var needsWidening = false;

            for (var idx = 0; idx < Instructions.Count; idx++)
            {
                var instruction = Instructions[idx];
                switch (instruction)
                {
                    case JumpInstruction jump:
                    {
                        var targetOffset = GetLabelOffset(jump.Target);
                        jump.BranchOffset = targetOffset - jump.Offset!.Value;

                        if (jump.NeedsWidening)
                        {
                            if (jump.OpCode is OperationCode.GOTO or OperationCode.JSR)
                            {
                                jump.UpgradeToWide();
                            }
                            else
                            {
                                InvertConditionalJumpAndInsertGotoW(Instructions, idx, jump);
                                idx++;
                            }

                            needsWidening = true;
                        }

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

            foreach (var block in TryCatchBlocks)
            {
                block.Start.Offset = GetLabelOffset(block.Start);
                block.End.Offset = GetLabelOffset(block.End);
                block.Handler.Offset = GetLabelOffset(block.Handler);
            }

            foreach (var lineNumber in LineNumbers)
            {
                lineNumber.Start.Offset = GetLabelOffset(lineNumber.Start);
            }

            foreach (var localVariable in LocalVariables)
            {
                localVariable.Start.Offset = GetLabelOffset(localVariable.Start);
                localVariable.End.Offset = GetLabelOffset(localVariable.End);
            }

            foreach (var typeAnnotation in TypeAnnotations)
            {
                if (typeAnnotation.OffsetTarget is not null)
                {
                    typeAnnotation.OffsetTarget.Offset =
                        GetLabelOffset(typeAnnotation.OffsetTarget);
                }

                foreach (var (start, end, _) in typeAnnotation.LocalVariableTargets)
                {
                    start.Offset = GetLabelOffset(start);
                    end.Offset = GetLabelOffset(end);
                }
            }

            if (!needsWidening)
            {
                ValidateCodeLength(pc);
                ValidateControlFlowTargets(pc);
                break;
            }
        }
    }

    private void AddLabelOffset(Label label, int offset)
    {
        if (_namedLabelOffsets.TryGetValue(label.Name, out var existingOffset)
            && existingOffset != offset)
        {
            throw new InvalidOperationException(
                $"Label name '{label.Name}' is defined at both offset {existingOffset} and {offset}.");
        }

        label.Offset = offset;
        _labelOffsets[label] = offset;
        _namedLabelOffsets[label.Name] = offset;
    }

    private static void ValidateCodeLength(int codeLength)
    {
        if (codeLength is <= 0 or >= ushort.MaxValue + 1)
        {
            throw new InvalidOperationException(
                $"JVM method code length must be between 1 and 65535 bytes, but was {codeLength}.");
        }
    }

    private void ValidateControlFlowTargets(int codeLength)
    {
        foreach (var instruction in Instructions)
        {
            switch (instruction)
            {
                case JumpInstruction jump:
                    ValidateInstructionTarget(GetLabelOffset(jump.Target), codeLength, jump.OpCode);
                    break;
                case TableSwitchInstruction tableSwitch:
                    ValidateInstructionTarget(
                        GetLabelOffset(tableSwitch.DefaultTarget),
                        codeLength,
                        tableSwitch.OpCode);
                    foreach (var target in tableSwitch.Targets)
                    {
                        ValidateInstructionTarget(
                            GetLabelOffset(target),
                            codeLength,
                            tableSwitch.OpCode);
                    }

                    break;
                case LookupSwitchInstruction lookupSwitch:
                    ValidateInstructionTarget(
                        GetLabelOffset(lookupSwitch.DefaultTarget),
                        codeLength,
                        lookupSwitch.OpCode);
                    foreach (var (_, target) in lookupSwitch.Pairs)
                    {
                        ValidateInstructionTarget(
                            GetLabelOffset(target),
                            codeLength,
                            lookupSwitch.OpCode);
                    }

                    break;
            }
        }
    }

    private static void ValidateInstructionTarget(
        int targetOffset,
        int codeLength,
        OperationCode opCode)
    {
        if (targetOffset < 0 || targetOffset >= codeLength)
        {
            throw new InvalidOperationException(
                $"{opCode} target offset {targetOffset} must identify an instruction in the method.");
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
        if (MaxStack is < 0 or > ushort.MaxValue)
        {
            throw new InvalidOperationException(
                $"MaxStack must be between 0 and 65535, but was {MaxStack}.");
        }

        if (MaxLocals is < 0 or > ushort.MaxValue)
        {
            throw new InvalidOperationException(
                $"MaxLocals must be between 0 and 65535, but was {MaxLocals}.");
        }

        ResolveCpReferences(cp);
        ResolveLabels();

        using var codeStream = new MemoryStream();
        WriteBytecode(codeStream);
        var codeBytes = codeStream.ToArray();

        var exceptionTable = new ExceptionTableEntry[TryCatchBlocks.Count];
        for (var i = 0; i < TryCatchBlocks.Count; i++)
        {
            var block = TryCatchBlocks[i];
            var startOffset = GetLabelOffset(block.Start);
            var endOffset = GetLabelOffset(block.End);
            var handlerOffset = GetLabelOffset(block.Handler);
            if (startOffset < 0
                || startOffset >= endOffset
                || endOffset > codeBytes.Length
                || handlerOffset < 0
                || handlerOffset >= codeBytes.Length)
            {
                throw new InvalidOperationException(
                    $"Invalid exception handler range [{startOffset}, {endOffset}) -> {handlerOffset}.");
            }

            var catchTypeIndex = block.CatchType != null
                ? cp.AddClass(block.CatchType)
                : 0;
            exceptionTable[i] = new ExceptionTableEntry
            {
                StartPc = new TUShort((ushort)startOffset),
                EndPc = new TUShort((ushort)endOffset),
                HandlerPc = new TUShort((ushort)handlerOffset),
                CatchType = new TUShort((ushort)catchTypeIndex)
            };
        }

        var codeAttr = new CodeAttribute
        {
            MaxStack = new TUShort((ushort)MaxStack),
            MaxLocals = new TUShort((ushort)MaxLocals),
            Code = codeBytes,
            ExceptionTable = exceptionTable,
            Attributes = Attributes.ToArray()
        };

        AddPositionDependentAttributes(codeAttr, cp, codeBytes.Length);

        if (MethodDescriptor != null)
        {
            var sfc = new StackFrameCalculator(this, MethodDescriptor, IsStatic, cp);
            var smt = sfc.Compute();
            var smtAttr = AttributeInfo.CreateFromAttribute("StackMapTable", smt, cp);
            var attrs = new List<AttributeInfo>(codeAttr.Attributes) { smtAttr };
            codeAttr.Attributes = attrs.ToArray();
        }

        return codeAttr;
    }

    private void AddPositionDependentAttributes(
        CodeAttribute codeAttribute,
        ConstantPoolBuilder cp,
        int codeLength)
    {
        var attributes = new List<AttributeInfo>(codeAttribute.Attributes);
        if (LineNumbers.Count > 0)
        {
            foreach (var lineNumber in LineNumbers)
            {
                var startOffset = GetLabelOffset(lineNumber.Start);
                if (startOffset < 0 || startOffset >= codeLength)
                {
                    throw new InvalidOperationException(
                        $"Line number target {startOffset} must identify an instruction.");
                }
            }

            var table = new LineNumberTableAttribute
            {
                LineNumberTable =
                [
                    .. LineNumbers.Select(lineNumber => new LineNumberTableEntry
                    {
                        StartPc = new TUShort((ushort)GetLabelOffset(lineNumber.Start)),
                        LineNumber = new TUShort((ushort)lineNumber.LineNumber)
                    })
                ]
            };
            attributes.Add(AttributeInfo.CreateFromAttribute("LineNumberTable", table, cp));
        }

        var descriptorVariables = LocalVariables
            .Where(variable => variable.Descriptor is not null)
            .ToList();
        ValidateLocalVariableRanges(LocalVariables, codeLength);
        if (descriptorVariables.Count > 0)
        {
            var table = new LocalVariableTableAttribute
            {
                LocalVariableTable =
                [
                    .. descriptorVariables.Select(variable =>
                        new LocalVariableTableEntry
                        {
                            StartPc = new TUShort((ushort)GetLabelOffset(variable.Start)),
                            Length = new TUShort((ushort)(
                                GetLabelOffset(variable.End)
                                - GetLabelOffset(variable.Start))),
                            NameIndex = new TUShort((ushort)cp.AddUtf8(variable.Name)),
                            DescriptorIndex = new TUShort((ushort)cp.AddUtf8(variable.Descriptor!)),
                            Index = new TUShort((ushort)variable.Index)
                        })
                ]
            };
            attributes.Add(AttributeInfo.CreateFromAttribute("LocalVariableTable", table, cp));
        }

        var signatureVariables = LocalVariables
            .Where(variable => variable.Signature is not null)
            .ToList();
        if (signatureVariables.Count > 0)
        {
            var table = new LocalVariableTypeTableAttribute
            {
                LocalVariableTypeTable =
                [
                    .. signatureVariables.Select(variable =>
                        new LocalVariableTypeTableEntry
                        {
                            StartPc = new TUShort((ushort)GetLabelOffset(variable.Start)),
                            Length = new TUShort((ushort)(
                                GetLabelOffset(variable.End)
                                - GetLabelOffset(variable.Start))),
                            NameIndex = new TUShort((ushort)cp.AddUtf8(variable.Name)),
                            SignatureIndex = new TUShort((ushort)cp.AddUtf8(variable.Signature!)),
                            Index = new TUShort((ushort)variable.Index)
                        })
                ]
            };
            attributes.Add(AttributeInfo.CreateFromAttribute("LocalVariableTypeTable", table, cp));
        }

        AddTypeAnnotationAttributes(attributes, cp, codeLength);
        codeAttribute.Attributes = attributes.ToArray();
    }

    private void AddTypeAnnotationAttributes(
        List<AttributeInfo> attributes,
        ConstantPoolBuilder cp,
        int codeLength)
    {
        foreach (var typeAnnotation in TypeAnnotations)
        {
            RelocateTypeAnnotation(typeAnnotation, codeLength);
        }

        var visible = TypeAnnotations
            .Where(typeAnnotation => typeAnnotation.IsVisible)
            .Select(typeAnnotation => typeAnnotation.Annotation)
            .ToArray();
        if (visible.Length > 0)
        {
            attributes.Add(AttributeInfo.CreateFromAttribute(
                "RuntimeVisibleTypeAnnotations",
                new RuntimeVisibleTypeAnnotationsAttribute { Annotations = visible },
                cp));
        }

        var invisible = TypeAnnotations
            .Where(typeAnnotation => !typeAnnotation.IsVisible)
            .Select(typeAnnotation => typeAnnotation.Annotation)
            .ToArray();
        if (invisible.Length > 0)
        {
            attributes.Add(AttributeInfo.CreateFromAttribute(
                "RuntimeInvisibleTypeAnnotations",
                new RuntimeInvisibleTypeAnnotationsAttribute { Annotations = invisible },
                cp));
        }
    }

    private void RelocateTypeAnnotation(
        BytecodeTypeAnnotation typeAnnotation,
        int codeLength)
    {
        switch (typeAnnotation.Annotation.TargetInfo)
        {
            case OffsetTarget offsetTarget:
            {
                var offset = GetRequiredInstructionOffset(
                    typeAnnotation.OffsetTarget,
                    codeLength,
                    "type annotation");
                offsetTarget.Offset = new TUShort((ushort)offset);
                break;
            }
            case TypeArgumentTarget typeArgumentTarget:
            {
                var offset = GetRequiredInstructionOffset(
                    typeAnnotation.OffsetTarget,
                    codeLength,
                    "type argument annotation");
                typeArgumentTarget.Offset = new TUShort((ushort)offset);
                break;
            }
            case LocalvarTarget localVariableTarget:
            {
                localVariableTarget.Table =
                [
                    .. typeAnnotation.LocalVariableTargets.Select(target =>
                    {
                        var startOffset = GetLabelOffset(target.Start);
                        var endOffset = GetLabelOffset(target.End);
                        if (startOffset < 0
                            || startOffset >= codeLength
                            || endOffset < startOffset
                            || endOffset > codeLength)
                        {
                            throw new InvalidOperationException(
                                $"Invalid type annotation local variable range [{startOffset}, {endOffset}).");
                        }

                        return new LocalvarTableEntry
                        {
                            StartPc = new TUShort((ushort)startOffset),
                            Length = new TUShort((ushort)(endOffset - startOffset)),
                            Index = new TUShort((ushort)target.Index)
                        };
                    })
                ];
                break;
            }
            case CatchTarget catchTarget:
            {
                var index = typeAnnotation.CatchTarget is null
                    ? -1
                    : TryCatchBlocks.IndexOf(typeAnnotation.CatchTarget);
                if (index < 0 || index > ushort.MaxValue)
                {
                    throw new InvalidOperationException(
                        "Type annotation catch target no longer belongs to this method.");
                }

                catchTarget.ExceptionTableIndex = new TUShort((ushort)index);
                break;
            }
        }
    }

    private int GetRequiredInstructionOffset(
        Label? label,
        int codeLength,
        string context)
    {
        if (label is null)
        {
            throw new InvalidOperationException($"{context} has no target label.");
        }

        var offset = GetLabelOffset(label);
        if (offset < 0 || offset >= codeLength)
        {
            throw new InvalidOperationException(
                $"{context} target {offset} must identify an instruction.");
        }

        return offset;
    }

    private void ValidateLocalVariableRanges(
        IEnumerable<BytecodeLocalVariable> variables,
        int codeLength)
    {
        foreach (var variable in variables)
        {
            var startOffset = GetLabelOffset(variable.Start);
            var endOffset = GetLabelOffset(variable.End);
            if (startOffset < 0
                || startOffset >= codeLength
                || endOffset < startOffset
                || endOffset > codeLength)
            {
                throw new InvalidOperationException(
                    $"Invalid local variable range [{startOffset}, {endOffset}).");
            }
        }
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
                case InvokeDynamicInstruction indy:
                    indy.Resolve(cp);
                    break;
            }
        }
    }

    private static InvokeDynamicInstruction ResolveInvokeDynamic(CpInfo?[] constantPool, int index)
    {
        if (index <= 0 || index >= constantPool.Length)
        {
            return new InvokeDynamicInstruction(index);
        }

        if (constantPool[index] is CpInvokeDynamic indyEntry)
        {
            var natIndex = indyEntry.NameAndTypeIndex.Value;
            if (natIndex > 0 && natIndex < constantPool.Length
                && constantPool[natIndex] is CpNameAndType nat)
            {
                var name = ((CpUtf8)constantPool[nat.NameIndex.Value]!).Value;
                var descriptor = ((CpUtf8)constantPool[nat.DescriptorIndex.Value]!).Value;
                return new InvokeDynamicInstruction(
                    indyEntry.BootstrapMethodAttrIndex.Value,
                    name,
                    descriptor);
            }
        }

        return new InvokeDynamicInstruction(index);
    }

    private int GetLabelOffset(Label label)
    {
        if (_labelOffsets.TryGetValue(label, out var offset))
        {
            return offset;
        }

        if (_namedLabelOffsets.TryGetValue(label.Name, out offset))
        {
            label.Offset = offset;
            return offset;
        }

        throw new InvalidOperationException($"Label '{label}' has not been resolved.");
    }

    private static void InvertConditionalJumpAndInsertGotoW(
        List<Instruction> instructions,
        int jumpIndex,
        JumpInstruction jump)
    {
        if (jumpIndex + 1 >= instructions.Count)
        {
            throw new InvalidOperationException(
                $"Cannot widen terminal conditional branch {jump.OpCode}; it has no fall-through instruction.");
        }

        var originalTarget = jump.Target;
        var skipLabel = new Label();
        var fallThrough = instructions[jumpIndex + 1];
        fallThrough.Labels.Add(skipLabel);
        jump.OpCode = InvertBranchOpcode(jump.OpCode);
        jump.Target = skipLabel;

        var gotoInsn = new JumpInstruction(OperationCode.GOTO_W, originalTarget);
        instructions.Insert(jumpIndex + 1, gotoInsn);
    }

    private static OperationCode InvertBranchOpcode(OperationCode op)
    {
        return op switch
        {
            OperationCode.IFEQ => OperationCode.IFNE,
            OperationCode.IFNE => OperationCode.IFEQ,
            OperationCode.IFLT => OperationCode.IFGE,
            OperationCode.IFGE => OperationCode.IFLT,
            OperationCode.IFGT => OperationCode.IFLE,
            OperationCode.IFLE => OperationCode.IFGT,
            OperationCode.IF_ICMPEQ => OperationCode.IF_ICMPNE,
            OperationCode.IF_ICMPNE => OperationCode.IF_ICMPEQ,
            OperationCode.IF_ICMPLT => OperationCode.IF_ICMPGE,
            OperationCode.IF_ICMPGE => OperationCode.IF_ICMPLT,
            OperationCode.IF_ICMPGT => OperationCode.IF_ICMPLE,
            OperationCode.IF_ICMPLE => OperationCode.IF_ICMPGT,
            OperationCode.IF_ACMPEQ => OperationCode.IF_ACMPNE,
            OperationCode.IF_ACMPNE => OperationCode.IF_ACMPEQ,
            OperationCode.IFNULL => OperationCode.IFNONNULL,
            OperationCode.IFNONNULL => OperationCode.IFNULL,
            _ => op
        };
    }

    public static MethodBody FromCodeAttribute(CodeAttribute attr, CpInfo?[] constantPool)
    {
        ArgumentNullException.ThrowIfNull(attr);
        ArgumentNullException.ThrowIfNull(constantPool);

        var body = new MethodBody
        {
            MaxStack = attr.MaxStack.Value,
            MaxLocals = attr.MaxLocals.Value,
            Attributes =
            [
                .. attr.Attributes.Where(attribute =>
                    !IsPositionDependentAttribute(attribute, constantPool))
            ]
        };

        var code = attr.Code;
        if (code.Length is <= 0 or > ushort.MaxValue)
        {
            throw new FormatException(
                $"JVM method code length must be between 1 and 65535 bytes, but was {code.Length}.");
        }

        var pcToInstruction = new Dictionary<int, Instruction>();
        var labelByPc = new Dictionary<int, Label>();
        var branchTargetPcs = new HashSet<int>();

        void TrackInstruction(Instruction insn, int startPc)
        {
            insn.Offset = startPc;
            body.Instructions.Add(insn);
            pcToInstruction[startPc] = insn;
        }

        var pc = 0;
        while (pc < code.Length)
        {
            var insnStartPc = pc;
            var opc = (OperationCode)ReadByte(code, ref pc, "opcode");
            Instruction instruction;

            switch (opc)
            {
                case OperationCode.TABLESWITCH:
                {
                    var padding = (4 - pc % 4) % 4;
                    SkipBytes(code, ref pc, padding, "tableswitch padding");

                    var defaultOffset = ReadInt32(code, ref pc, "tableswitch default");
                    var low = ReadInt32(code, ref pc, "tableswitch low");
                    var high = ReadInt32(code, ref pc, "tableswitch high");

                    var countValue = (long)high - low + 1;
                    if (countValue <= 0 || countValue > int.MaxValue)
                    {
                        throw InvalidBytecode(insnStartPc, "tableswitch has an invalid low/high range.");
                    }

                    var count = (int)countValue;
                    var targetByteCount = (long)count * sizeof(int);
                    if (targetByteCount > int.MaxValue)
                    {
                        throw InvalidBytecode(
                            insnStartPc,
                            "tableswitch target table is too large.");
                    }

                    EnsureAvailable(
                        code,
                        pc,
                        (int)targetByteCount,
                        "tableswitch targets");
                    var targetOffsets = new int[count];
                    for (var i = 0; i < count; i++)
                    {
                        targetOffsets[i] = ReadInt32(code, ref pc, "tableswitch target");
                    }

                    var defaultPc = AddBranchOffset(insnStartPc, defaultOffset);
                    branchTargetPcs.Add(defaultPc);
                    var defaultLabel = GetOrCreateLabel(labelByPc, defaultPc);
                    var targetLabels = targetOffsets
                        .Select(offset =>
                        {
                            var targetPc = AddBranchOffset(insnStartPc, offset);
                            branchTargetPcs.Add(targetPc);
                            return GetOrCreateLabel(labelByPc, targetPc);
                        })
                        .ToList();

                    instruction = new TableSwitchInstruction(low, high, defaultLabel, targetLabels);
                    break;
                }

                case OperationCode.LOOKUPSWITCH:
                {
                    var padding = (4 - pc % 4) % 4;
                    SkipBytes(code, ref pc, padding, "lookupswitch padding");

                    var defaultOffset = ReadInt32(code, ref pc, "lookupswitch default");
                    var pairCount = ReadInt32(code, ref pc, "lookupswitch pair count");
                    if (pairCount < 0)
                    {
                        throw InvalidBytecode(insnStartPc, "lookupswitch pair count cannot be negative.");
                    }

                    var pairByteCount = (long)pairCount * 2 * sizeof(int);
                    if (pairByteCount > int.MaxValue)
                    {
                        throw InvalidBytecode(
                            insnStartPc,
                            "lookupswitch pair table is too large.");
                    }

                    EnsureAvailable(code, pc, (int)pairByteCount, "lookupswitch pairs");
                    var pairs = new List<(int Key, Label Target)>(pairCount);
                    var previousKey = int.MinValue;
                    for (var i = 0; i < pairCount; i++)
                    {
                        var key = ReadInt32(code, ref pc, "lookupswitch key");
                        var offset = ReadInt32(code, ref pc, "lookupswitch target");
                        if (i > 0 && key <= previousKey)
                        {
                            throw InvalidBytecode(
                                insnStartPc,
                                "lookupswitch keys must be strictly increasing.");
                        }

                        previousKey = key;
                        var targetPc = AddBranchOffset(insnStartPc, offset);
                        branchTargetPcs.Add(targetPc);
                        pairs.Add((key, GetOrCreateLabel(labelByPc, targetPc)));
                    }

                    var defaultPc = AddBranchOffset(insnStartPc, defaultOffset);
                    branchTargetPcs.Add(defaultPc);
                    var defaultLabel = GetOrCreateLabel(labelByPc, defaultPc);
                    instruction = new LookupSwitchInstruction(defaultLabel, pairs);
                    break;
                }

                case OperationCode.WIDE:
                {
                    var wideOpc = (OperationCode)ReadByte(code, ref pc, "wide opcode");

                    if (wideOpc == OperationCode.IINC)
                    {
                        var index = ReadUInt16(code, ref pc, "wide iinc index");
                        var increment = ReadInt16(code, ref pc, "wide iinc increment");
                        instruction = new IincInstruction(index, increment);
                    }
                    else
                    {
                        if (!IsWideVariableOpCode(wideOpc))
                        {
                            throw InvalidBytecode(
                                insnStartPc,
                                $"wide cannot modify opcode {wideOpc}.");
                        }

                        var index = ReadUInt16(code, ref pc, "wide variable index");
                        instruction = new VarInstruction(wideOpc, index);
                    }

                    break;
                }

                case OperationCode.GOTO_W or OperationCode.JSR_W:
                {
                    var offset = ReadInt32(code, ref pc, "wide branch offset");
                    var targetPc = AddBranchOffset(insnStartPc, offset);
                    branchTargetPcs.Add(targetPc);
                    var label = GetOrCreateLabel(labelByPc, targetPc);
                    instruction = new JumpInstruction(opc, label);
                    break;
                }

                case OperationCode.BIPUSH:
                {
                    var value = (sbyte)ReadByte(code, ref pc, "bipush value");
                    instruction = new IntInstruction(opc, value);
                    break;
                }

                case OperationCode.NEWARRAY:
                {
                    var arrayType = ReadByte(code, ref pc, "newarray type");
                    instruction = new IntInstruction(opc, arrayType);
                    break;
                }

                case OperationCode.LDC:
                {
                    var index = ReadByte(code, ref pc, "ldc constant index");
                    instruction = CreateLdcInstruction(constantPool, index, false);
                    break;
                }

                case OperationCode.SIPUSH:
                {
                    var value = ReadInt16(code, ref pc, "sipush value");
                    instruction = new IntInstruction(opc, value);
                    break;
                }

                case OperationCode.LDC_W or OperationCode.LDC2_W:
                {
                    var index = ReadUInt16(code, ref pc, "ldc constant index");
                    var isWide2 = opc == OperationCode.LDC2_W;
                    instruction = CreateLdcInstruction(constantPool, index, isWide2);
                    break;
                }

                case OperationCode.ILOAD or OperationCode.LLOAD or OperationCode.FLOAD
                    or OperationCode.DLOAD or OperationCode.ALOAD
                    or OperationCode.ISTORE or OperationCode.LSTORE or OperationCode.FSTORE
                    or OperationCode.DSTORE or OperationCode.ASTORE
                    or OperationCode.RET:
                {
                    var index = ReadByte(code, ref pc, "local variable index");
                    instruction = new VarInstruction(opc, index);
                    break;
                }

                case OperationCode.IINC:
                {
                    var index = ReadByte(code, ref pc, "iinc variable index");
                    var increment = (sbyte)ReadByte(code, ref pc, "iinc increment");
                    instruction = new IincInstruction(index, increment);
                    break;
                }

                case OperationCode.GETSTATIC or OperationCode.PUTSTATIC
                    or OperationCode.GETFIELD or OperationCode.PUTFIELD:
                {
                    var index = ReadUInt16(code, ref pc, "field reference index");
                    var (owner, name, desc) = ResolveFieldRef(constantPool, index);
                    instruction = new FieldInstruction(opc, owner, name, desc);
                    break;
                }

                case OperationCode.INVOKEVIRTUAL or OperationCode.INVOKESPECIAL
                    or OperationCode.INVOKESTATIC:
                {
                    var index = ReadUInt16(code, ref pc, "method reference index");
                    var (owner, name, desc) = ResolveMethodRef(constantPool, index);
                    instruction = new MethodInstruction(opc, owner, name, desc);
                    break;
                }

                case OperationCode.INVOKEINTERFACE:
                {
                    var index = ReadUInt16(code, ref pc, "interface method reference index");
                    var count = ReadByte(code, ref pc, "invokeinterface count");
                    var zero = ReadByte(code, ref pc, "invokeinterface reserved byte");
                    if (count == 0 || zero != 0)
                    {
                        throw InvalidBytecode(
                            insnStartPc,
                            "invokeinterface must have a nonzero count and a zero reserved byte.");
                    }

                    var (owner, name, desc) = ResolveInterfaceMethodRef(constantPool, index);
                    instruction = new MethodInstruction(opc, owner, name, desc) { Count = count };
                    break;
                }

                case OperationCode.INVOKEDYNAMIC:
                {
                    var index = ReadUInt16(code, ref pc, "invokedynamic constant index");
                    var zero1 = ReadByte(code, ref pc, "invokedynamic reserved byte");
                    var zero2 = ReadByte(code, ref pc, "invokedynamic reserved byte");
                    if (zero1 != 0 || zero2 != 0)
                    {
                        throw InvalidBytecode(
                            insnStartPc,
                            "invokedynamic reserved bytes must both be zero.");
                    }

                    instruction = ResolveInvokeDynamic(constantPool, index);
                    break;
                }

                case OperationCode.NEW or OperationCode.ANEWARRAY
                    or OperationCode.CHECKCAST or OperationCode.INSTANCEOF:
                {
                    var index = ReadUInt16(code, ref pc, "class reference index");
                    var typeName = ResolveClassName(constantPool, index);
                    instruction = new TypeInstruction(opc, typeName);
                    break;
                }

                case OperationCode.MULTIANEWARRAY:
                {
                    var index = ReadUInt16(code, ref pc, "multianewarray class index");
                    var dimensions = ReadByte(code, ref pc, "multianewarray dimensions");
                    if (dimensions == 0)
                    {
                        throw InvalidBytecode(
                            insnStartPc,
                            "multianewarray dimensions must be greater than zero.");
                    }

                    var typeName = ResolveClassName(constantPool, index);
                    instruction = new MultiANewArrayInstruction(typeName, dimensions);
                    break;
                }

                case OperationCode.IFEQ or OperationCode.IFNE or OperationCode.IFLT
                    or OperationCode.IFGE or OperationCode.IFGT or OperationCode.IFLE
                    or OperationCode.IF_ICMPEQ or OperationCode.IF_ICMPNE
                    or OperationCode.IF_ICMPLT or OperationCode.IF_ICMPGE
                    or OperationCode.IF_ICMPGT or OperationCode.IF_ICMPLE
                    or OperationCode.IF_ACMPEQ or OperationCode.IF_ACMPNE
                    or OperationCode.GOTO or OperationCode.JSR
                    or OperationCode.IFNULL or OperationCode.IFNONNULL:
                {
                    var offset = ReadInt16(code, ref pc, "branch offset");
                    var targetPc = AddBranchOffset(insnStartPc, offset);
                    branchTargetPcs.Add(targetPc);
                    var label = GetOrCreateLabel(labelByPc, targetPc);
                    instruction = new JumpInstruction(opc, label);
                    break;
                }

                default:
                {
                    if (!Enum.IsDefined(opc)
                        || opc is OperationCode.BREAKPOINT or OperationCode.IMPDEP1 or OperationCode.IMPDEP2)
                    {
                        throw InvalidBytecode(
                            insnStartPc,
                            $"reserved or unknown opcode 0x{(byte)opc:X2}.");
                    }

                    instruction = new InsnInstruction(opc);
                    break;
                }
            }

            TrackInstruction(instruction, insnStartPc);
        }

        foreach (var exEntry in attr.ExceptionTable)
        {
            ValidateExceptionTableEntry(exEntry, code.Length, pcToInstruction);
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

        ParsePositionDependentAttributes(
            body,
            attr.Attributes,
            constantPool,
            labelByPc);

        foreach (var instructionPc in pcToInstruction.Keys)
        {
            GetOrCreateLabel(labelByPc, instructionPc);
        }

        foreach (var targetPc in branchTargetPcs)
        {
            if (!pcToInstruction.ContainsKey(targetPc))
            {
                throw new FormatException(
                    $"Branch target {targetPc} is not the start of an instruction.");
            }
        }

        foreach (var (targetPc, label) in labelByPc)
        {
            if (pcToInstruction.TryGetValue(targetPc, out var targetInstruction))
            {
                targetInstruction.Labels.Add(label);
            }
            else if (targetPc == code.Length)
            {
                body.EndLabels.Add(label);
            }
            else
            {
                throw new FormatException(
                    $"Label target {targetPc} is not the start of an instruction.");
            }
        }

        return body;
    }

    private static Label GetOrCreateLabel(Dictionary<int, Label> map, int pc)
    {
        if (map.TryGetValue(pc, out var label))
        {
            return label;
        }

        label = new Label($"L{pc:X4}") { Offset = pc };
        map[pc] = label;
        return label;
    }

    private static bool IsAttributeNamed(
        AttributeInfo attribute,
        CpInfo?[] constantPool,
        string expectedName)
    {
        var index = attribute.AttributeNameIndex.Value;
        return index > 0
            && index < constantPool.Length
            && constantPool[index] is CpUtf8 utf8
            && string.Equals(utf8.Value, expectedName, StringComparison.Ordinal);
    }

    private static bool IsPositionDependentAttribute(
        AttributeInfo attribute,
        CpInfo?[] constantPool)
    {
        return IsAttributeNamed(attribute, constantPool, "StackMapTable")
            || IsAttributeNamed(attribute, constantPool, "LineNumberTable")
            || IsAttributeNamed(attribute, constantPool, "LocalVariableTable")
            || IsAttributeNamed(attribute, constantPool, "LocalVariableTypeTable")
            || IsAttributeNamed(attribute, constantPool, "RuntimeVisibleTypeAnnotations")
            || IsAttributeNamed(attribute, constantPool, "RuntimeInvisibleTypeAnnotations");
    }

    private static void ParsePositionDependentAttributes(
        MethodBody body,
        IEnumerable<AttributeInfo> attributes,
        CpInfo?[] constantPool,
        Dictionary<int, Label> labelByPc)
    {
        foreach (var attribute in attributes)
        {
            switch (attribute.ResolveBody(constantPool))
            {
                case LineNumberTableAttribute lineNumberTable:
                    foreach (var entry in lineNumberTable.LineNumberTable)
                    {
                        body.LineNumbers.Add(new BytecodeLineNumber(
                            GetOrCreateLabel(labelByPc, entry.StartPc.Value),
                            entry.LineNumber.Value));
                    }

                    break;

                case LocalVariableTableAttribute localVariableTable:
                    foreach (var entry in localVariableTable.LocalVariableTable)
                    {
                        var startPc = entry.StartPc.Value;
                        var endPc = startPc + entry.Length.Value;
                        body.LocalVariables.Add(new BytecodeLocalVariable(
                            GetOrCreateLabel(labelByPc, startPc),
                            GetOrCreateLabel(labelByPc, endPc),
                            ResolveUtf8(constantPool, entry.NameIndex.Value),
                            ResolveUtf8(constantPool, entry.DescriptorIndex.Value),
                            entry.Index.Value));
                    }

                    break;

                case LocalVariableTypeTableAttribute localVariableTypeTable:
                    foreach (var entry in localVariableTypeTable.LocalVariableTypeTable)
                    {
                        var startPc = entry.StartPc.Value;
                        var endPc = startPc + entry.Length.Value;
                        var name = ResolveUtf8(constantPool, entry.NameIndex.Value);
                        var existing = body.LocalVariables.FirstOrDefault(variable =>
                            variable.Start.Name == GetOrCreateLabel(labelByPc, startPc).Name
                            && variable.End.Name == GetOrCreateLabel(labelByPc, endPc).Name
                            && variable.Name == name
                            && variable.Index == entry.Index.Value);

                        if (existing is not null)
                        {
                            existing.Signature = ResolveUtf8(
                                constantPool,
                                entry.SignatureIndex.Value);
                        }
                        else
                        {
                            body.LocalVariables.Add(new BytecodeLocalVariable(
                                GetOrCreateLabel(labelByPc, startPc),
                                GetOrCreateLabel(labelByPc, endPc),
                                name,
                                null,
                                entry.Index.Value,
                                ResolveUtf8(constantPool, entry.SignatureIndex.Value)));
                        }
                    }

                    break;

                case RuntimeVisibleTypeAnnotationsAttribute visibleTypeAnnotations:
                    ParseTypeAnnotations(
                        body,
                        visibleTypeAnnotations.Annotations,
                        isVisible: true,
                        labelByPc);
                    break;

                case RuntimeInvisibleTypeAnnotationsAttribute invisibleTypeAnnotations:
                    ParseTypeAnnotations(
                        body,
                        invisibleTypeAnnotations.Annotations,
                        isVisible: false,
                        labelByPc);
                    break;
            }
        }
    }

    private static void ParseTypeAnnotations(
        MethodBody body,
        IEnumerable<TypeAnnotation> annotations,
        bool isVisible,
        Dictionary<int, Label> labelByPc)
    {
        foreach (var annotation in annotations)
        {
            var bytecodeAnnotation = new BytecodeTypeAnnotation(isVisible, annotation);
            switch (annotation.TargetInfo)
            {
                case OffsetTarget offsetTarget:
                    bytecodeAnnotation.OffsetTarget =
                        GetOrCreateLabel(labelByPc, offsetTarget.Offset.Value);
                    break;
                case TypeArgumentTarget typeArgumentTarget:
                    bytecodeAnnotation.OffsetTarget =
                        GetOrCreateLabel(labelByPc, typeArgumentTarget.Offset.Value);
                    break;
                case LocalvarTarget localVariableTarget:
                    foreach (var entry in localVariableTarget.Table)
                    {
                        var startPc = entry.StartPc.Value;
                        bytecodeAnnotation.LocalVariableTargets.Add((
                            GetOrCreateLabel(labelByPc, startPc),
                            GetOrCreateLabel(labelByPc, startPc + entry.Length.Value),
                            entry.Index.Value));
                    }

                    break;
                case CatchTarget catchTarget:
                    if (catchTarget.ExceptionTableIndex.Value >= body.TryCatchBlocks.Count)
                    {
                        throw new FormatException(
                            $"Type annotation catch target {catchTarget.ExceptionTableIndex.Value} is outside the exception table.");
                    }

                    bytecodeAnnotation.CatchTarget =
                        body.TryCatchBlocks[catchTarget.ExceptionTableIndex.Value];
                    break;
            }

            body.TypeAnnotations.Add(bytecodeAnnotation);
        }
    }

    private static string ResolveUtf8(CpInfo?[] constantPool, int index)
    {
        return constantPool[index] is CpUtf8 utf8
            ? utf8.Value
            : throw new FormatException(
                $"Constant pool entry {index} must be a CONSTANT_Utf8_info.");
    }

    private static void ValidateExceptionTableEntry(
        ExceptionTableEntry entry,
        int codeLength,
        IReadOnlyDictionary<int, Instruction> instructionByPc)
    {
        var startPc = entry.StartPc.Value;
        var endPc = entry.EndPc.Value;
        var handlerPc = entry.HandlerPc.Value;

        if (startPc >= endPc)
        {
            throw new FormatException(
                $"Exception table range [{startPc}, {endPc}) must not be empty.");
        }

        if (!instructionByPc.ContainsKey(startPc))
        {
            throw new FormatException(
                $"Exception table start_pc {startPc} is not the start of an instruction.");
        }

        if (endPc != codeLength && !instructionByPc.ContainsKey(endPc))
        {
            throw new FormatException(
                $"Exception table end_pc {endPc} is not an instruction boundary or code_length.");
        }

        if (!instructionByPc.ContainsKey(handlerPc))
        {
            throw new FormatException(
                $"Exception table handler_pc {handlerPc} is not the start of an instruction.");
        }
    }

    private static bool IsWideVariableOpCode(OperationCode opCode)
    {
        return opCode is OperationCode.ILOAD
            or OperationCode.LLOAD
            or OperationCode.FLOAD
            or OperationCode.DLOAD
            or OperationCode.ALOAD
            or OperationCode.ISTORE
            or OperationCode.LSTORE
            or OperationCode.FSTORE
            or OperationCode.DSTORE
            or OperationCode.ASTORE
            or OperationCode.RET;
    }

    private static int AddBranchOffset(int instructionPc, int branchOffset)
    {
        try
        {
            return checked(instructionPc + branchOffset);
        }
        catch (OverflowException exception)
        {
            throw new FormatException(
                $"Branch at offset {instructionPc} has an overflowing target.",
                exception);
        }
    }

    private static byte ReadByte(byte[] data, ref int offset, string context)
    {
        EnsureAvailable(data, offset, 1, context);
        return data[offset++];
    }

    private static void SkipBytes(byte[] data, ref int offset, int count, string context)
    {
        EnsureAvailable(data, offset, count, context);
        offset += count;
    }

    private static void EnsureAvailable(byte[] data, int offset, int count, string context)
    {
        if (offset < 0 || count < 0 || offset > data.Length - count)
        {
            throw new FormatException(
                $"Unexpected end of bytecode while reading {context} at offset {offset}.");
        }
    }

    private static FormatException InvalidBytecode(int offset, string message)
    {
        return new FormatException($"Invalid bytecode at offset {offset}: {message}");
    }

    private static Instruction CreateLdcInstruction(CpInfo?[] constantPool, int index, bool isWide2)
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
                var utf8 = (CpUtf8)constantPool[strEntry.StringIndex.Value]!;
                return new LdcInstruction(utf8.Value);
            }

            case CpLong longEntry:
                return new LdcInstruction(longEntry.Bytes.Value);

            case CpDouble doubleEntry:
                return new LdcInstruction(doubleEntry.Bytes.Value);

            case CpClass classEntry:
            {
                var className = ((CpUtf8)constantPool[classEntry.NameIndex.Value]!).Value;
                return LdcInstruction.CreateSymbolic(
                    isWide2 ? OperationCode.LDC2_W : OperationCode.LDC_W,
                    "Ljava/lang/Class;",
                    builder => builder.AddClass(className));
            }

            case CpMethodType methodTypeEntry:
            {
                var descriptor =
                    ((CpUtf8)constantPool[methodTypeEntry.DescriptorIndex.Value]!).Value;
                return LdcInstruction.CreateSymbolic(
                    isWide2 ? OperationCode.LDC2_W : OperationCode.LDC_W,
                    "Ljava/lang/invoke/MethodType;",
                    builder => builder.AddMethodType(descriptor));
            }

            case CpMethodHandle methodHandleEntry:
            {
                var referenceKind = methodHandleEntry.ReferenceKind.Value;
                var reference = ResolveMethodHandleReference(
                    constantPool,
                    methodHandleEntry.ReferenceIndex.Value);
                return LdcInstruction.CreateSymbolic(
                    isWide2 ? OperationCode.LDC2_W : OperationCode.LDC_W,
                    "Ljava/lang/invoke/MethodHandle;",
                    builder => builder.AddMethodHandle(
                        referenceKind,
                        reference(builder)));
            }

            case CpDynamic dynamicEntry:
            {
                var (_, descriptor) = ResolveNameAndType(
                    constantPool,
                    dynamicEntry.NameAndTypeIndex.Value);
                var (name, _) = ResolveNameAndType(
                    constantPool,
                    dynamicEntry.NameAndTypeIndex.Value);
                return LdcInstruction.CreateSymbolic(
                    isWide2 ? OperationCode.LDC2_W : OperationCode.LDC_W,
                    descriptor,
                    builder => builder.AddDynamic(
                        dynamicEntry.BootstrapMethodAttrIndex.Value,
                        name,
                        descriptor));
            }

            default:
                return new LdcInstruction(isWide2 ? OperationCode.LDC2_W : OperationCode.LDC_W, index);
        }
    }

    private static string ResolveClassName(CpInfo?[] constantPool, int classIndex)
    {
        if (classIndex <= 0 || classIndex >= constantPool.Length)
        {
            return $"<unknown #{classIndex}>";
        }

        var classEntry = (CpClass)constantPool[classIndex]!;
        var utf8 = (CpUtf8)constantPool[classEntry.NameIndex.Value]!;
        return utf8.Value;
    }

    private static (string Owner, string Name, string Descriptor) ResolveFieldRef(CpInfo?[] constantPool, int index)
    {
        var fieldRef = (CpFieldRef)constantPool[index]!;
        var className = ResolveClassName(constantPool, fieldRef.ClassIndex.Value);
        var (name, descriptor) = ResolveNameAndType(constantPool, fieldRef.NameAndTypeIndex.Value);
        return (className, name, descriptor);
    }

    private static (string Owner, string Name, string Descriptor) ResolveMethodRef(CpInfo?[] constantPool, int index)
    {
        var methodRef = (CpMethodRef)constantPool[index]!;
        var className = ResolveClassName(constantPool, methodRef.ClassIndex.Value);
        var (name, descriptor) = ResolveNameAndType(constantPool, methodRef.NameAndTypeIndex.Value);
        return (className, name, descriptor);
    }

    private static (string Owner, string Name, string Descriptor) ResolveInterfaceMethodRef(CpInfo?[] constantPool, int index)
    {
        var methodRef = (CpInterfaceMethodRef)constantPool[index]!;
        var className = ResolveClassName(constantPool, methodRef.ClassIndex.Value);
        var (name, descriptor) = ResolveNameAndType(constantPool, methodRef.NameAndTypeIndex.Value);
        return (className, name, descriptor);
    }

    private static (string Name, string Descriptor) ResolveNameAndType(CpInfo?[] constantPool, int index)
    {
        var nat = (CpNameAndType)constantPool[index]!;
        var name = ((CpUtf8)constantPool[nat.NameIndex.Value]!).Value;
        var descriptor = ((CpUtf8)constantPool[nat.DescriptorIndex.Value]!).Value;
        return (name, descriptor);
    }

    private static Func<ConstantPoolBuilder, int> ResolveMethodHandleReference(
        CpInfo?[] constantPool,
        int index)
    {
        return constantPool[index] switch
        {
            CpFieldRef fieldReference =>
                CreateFieldReferenceResolver(constantPool, fieldReference),
            CpMethodRef methodReference =>
                CreateMethodReferenceResolver(constantPool, methodReference),
            CpInterfaceMethodRef interfaceMethodReference =>
                CreateInterfaceMethodReferenceResolver(
                    constantPool,
                    interfaceMethodReference),
            _ => throw new FormatException(
                $"Method handle reference index {index} has an invalid constant pool type.")
        };
    }

    private static Func<ConstantPoolBuilder, int> CreateFieldReferenceResolver(
        CpInfo?[] constantPool,
        CpFieldRef reference)
    {
        var owner = ResolveClassName(constantPool, reference.ClassIndex.Value);
        var (name, descriptor) = ResolveNameAndType(
            constantPool,
            reference.NameAndTypeIndex.Value);
        return builder => builder.AddFieldRef(owner, name, descriptor);
    }

    private static Func<ConstantPoolBuilder, int> CreateMethodReferenceResolver(
        CpInfo?[] constantPool,
        CpMethodRef reference)
    {
        var owner = ResolveClassName(constantPool, reference.ClassIndex.Value);
        var (name, descriptor) = ResolveNameAndType(
            constantPool,
            reference.NameAndTypeIndex.Value);
        return builder => builder.AddMethodRef(owner, name, descriptor);
    }

    private static Func<ConstantPoolBuilder, int> CreateInterfaceMethodReferenceResolver(
        CpInfo?[] constantPool,
        CpInterfaceMethodRef reference)
    {
        var owner = ResolveClassName(constantPool, reference.ClassIndex.Value);
        var (name, descriptor) = ResolveNameAndType(
            constantPool,
            reference.NameAndTypeIndex.Value);
        return builder => builder.AddInterfaceMethodRef(owner, name, descriptor);
    }

    private static short ReadInt16(byte[] data, ref int offset, string context)
    {
        EnsureAvailable(data, offset, sizeof(short), context);
        var value = (short)((data[offset] << 8) | data[offset + 1]);
        offset += sizeof(short);
        return value;
    }

    private static int ReadUInt16(byte[] data, ref int offset, string context)
    {
        EnsureAvailable(data, offset, sizeof(ushort), context);
        var value = (data[offset] << 8) | data[offset + 1];
        offset += sizeof(ushort);
        return value;
    }

    private static int ReadInt32(byte[] data, ref int offset, string context)
    {
        EnsureAvailable(data, offset, sizeof(int), context);
        var value = (data[offset] << 24)
            | (data[offset + 1] << 16)
            | (data[offset + 2] << 8)
            | data[offset + 3];
        offset += sizeof(int);
        return value;
    }
}
