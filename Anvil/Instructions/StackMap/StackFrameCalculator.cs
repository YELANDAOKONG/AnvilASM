using Anvil.Descriptors;
using Anvil.Instructions.ConstantPool;
using Anvil.Structures.Attributes;
using Anvil.Structures.Attributes.StackMap;
using Anvil.Structures.Attributes.StackMap.Frames;
using Anvil.Structures.Attributes.StackMap.Types;
using Anvil.Types;

namespace Anvil.Instructions.StackMap;

public class StackFrameCalculator
{
    private readonly MethodBody _body;
    private readonly ConstantPoolBuilder _constantPool;
    private readonly FrameState _initialState;
    private readonly Dictionary<int, FrameState> _frames = new();
    private readonly Dictionary<string, JvmObject> _handlerTypes = new(StringComparer.Ordinal);

    public int MaxStack { get; private set; }
    public int MaxLocals { get; private set; }

    public StackFrameCalculator(
        MethodBody body,
        string methodDescriptor,
        bool isStatic,
        ConstantPoolBuilder constantPool)
    {
        _body = body ?? throw new ArgumentNullException(nameof(body));
        _constantPool = constantPool ?? throw new ArgumentNullException(nameof(constantPool));
        _initialState = CreateInitialState(methodDescriptor, isStatic);
    }

    public StackMapTableAttribute Compute()
    {
        _body.ResolveLabels();
        _frames.Clear();
        _handlerTypes.Clear();
        MaxStack = 0;
        MaxLocals = ComputeMaxLocals();

        var instructionIndex = BuildInstructionIndex();
        if (_body.Instructions.Count == 0)
        {
            throw new InvalidOperationException("Cannot compute stack frames for an empty method body.");
        }

        var worklist = new Queue<int>();
        MergeFrame(0, _initialState, worklist);

        while (worklist.TryDequeue(out var pc))
        {
            var index = instructionIndex[pc];
            var instruction = _body.Instructions[index];
            var state = _frames[pc].Clone();

            TrackMaxStack(state);
            PropagateExceptionHandlers(pc, state, worklist);
            ApplyEffect(instruction, state);
            TrackMaxStack(state);

            foreach (var successor in GetSuccessors(instruction, index))
            {
                MergeFrame(successor, state, worklist);
            }
        }

        return new StackMapTableAttribute(BuildFrames());
    }

    private StackMapFrame[] BuildFrames()
    {
        var frames = new List<StackMapFrame>();
        var previousPc = -1;

        foreach (var (pc, state) in _frames.OrderBy(pair => pair.Key))
        {
            if (pc == 0)
            {
                continue;
            }

            var offsetDelta = pc - previousPc - 1;
            frames.Add(BuildFullFrame(offsetDelta, state));
            previousPc = pc;
        }

        return frames.ToArray();
    }

    private IEnumerable<int> GetSuccessors(Instruction instruction, int index)
    {
        var nextOffset = index + 1 < _body.Instructions.Count
            ? _body.Instructions[index + 1].Offset
            : null;

        switch (instruction)
        {
            case JumpInstruction jump
                when jump.OpCode is OperationCode.GOTO or OperationCode.GOTO_W:
                yield return GetResolvedOffset(jump.Target);
                yield break;

            case JumpInstruction jump
                when jump.OpCode is OperationCode.JSR or OperationCode.JSR_W:
                throw new NotSupportedException(
                    "Stack map calculation does not support legacy JSR/RET subroutines.");

            case JumpInstruction jump:
                yield return GetResolvedOffset(jump.Target);
                if (nextOffset.HasValue)
                {
                    yield return nextOffset.Value;
                }

                yield break;

            case TableSwitchInstruction tableSwitch:
                yield return GetResolvedOffset(tableSwitch.DefaultTarget);
                foreach (var target in tableSwitch.Targets)
                {
                    yield return GetResolvedOffset(target);
                }

                yield break;

            case LookupSwitchInstruction lookupSwitch:
                yield return GetResolvedOffset(lookupSwitch.DefaultTarget);
                foreach (var (_, target) in lookupSwitch.Pairs)
                {
                    yield return GetResolvedOffset(target);
                }

                yield break;
        }

        if (instruction.OpCode is OperationCode.IRETURN
            or OperationCode.LRETURN
            or OperationCode.FRETURN
            or OperationCode.DRETURN
            or OperationCode.ARETURN
            or OperationCode.RETURN
            or OperationCode.ATHROW)
        {
            yield break;
        }

        if (instruction.OpCode == OperationCode.RET)
        {
            throw new NotSupportedException(
                "Stack map calculation does not support legacy JSR/RET subroutines.");
        }

        if (nextOffset.HasValue)
        {
            yield return nextOffset.Value;
        }
    }

    private void PropagateExceptionHandlers(int pc, FrameState state, Queue<int> worklist)
    {
        foreach (var block in _body.TryCatchBlocks)
        {
            var startPc = GetResolvedOffset(block.Start);
            var endPc = GetResolvedOffset(block.End);
            if (pc < startPc || pc >= endPc)
            {
                continue;
            }

            var handlerState = state.Clone();
            handlerState.Stack.Clear();
            handlerState.Push(GetHandlerType(block.Handler));
            MergeFrame(GetResolvedOffset(block.Handler), handlerState, worklist);
        }
    }

    private JvmObject GetHandlerType(Label handler)
    {
        if (_handlerTypes.TryGetValue(handler.Name, out var type))
        {
            return type;
        }

        var catchTypes = _body.TryCatchBlocks
            .Where(block => block.Handler.Name == handler.Name)
            .Select(block => block.CatchType)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        string typeName;
        if (catchTypes.Count == 1 && catchTypes[0] is not null)
        {
            typeName = catchTypes[0]!;
        }
        else if (catchTypes.Any(catchType => catchType is null)
            || catchTypes.Any(catchType =>
                catchType == "java/lang/Throwable"
                || catchType?.EndsWith("Error", StringComparison.Ordinal) == true))
        {
            typeName = "java/lang/Throwable";
        }
        else
        {
            // Multi-catch alternatives are disjoint subclasses. Java source multi-catch
            // types conventionally meet at Exception; Error alternatives meet at Throwable.
            typeName = "java/lang/Exception";
        }

        type = new JvmObject(typeName);
        _handlerTypes[handler.Name] = type;
        return type;
    }

    private void MergeFrame(int pc, FrameState incoming, Queue<int> worklist)
    {
        TrackMaxStack(incoming);

        if (!_frames.TryGetValue(pc, out var current))
        {
            _frames[pc] = incoming.Clone();
            worklist.Enqueue(pc);
            return;
        }

        if (MergeInto(current, incoming))
        {
            worklist.Enqueue(pc);
        }
    }

    private bool MergeInto(FrameState target, FrameState source)
    {
        if (target.Stack.Count != source.Stack.Count)
        {
            throw new InvalidOperationException(
                "Control-flow paths reach the same instruction with different stack heights.");
        }

        var localCount = Math.Max(target.Locals.Count, source.Locals.Count);
        while (target.Locals.Count < localCount)
        {
            target.Locals.Add(JvmType.Top);
        }

        var changed = false;
        for (var i = 0; i < localCount; i++)
        {
            var sourceType = i < source.Locals.Count ? source.Locals[i] : JvmType.Top;
            var merged = MergeTypes(target.Locals[i], sourceType);
            if (!merged.Equals(target.Locals[i]))
            {
                target.Locals[i] = merged;
                changed = true;
            }
        }

        for (var i = 0; i < target.Stack.Count; i++)
        {
            var merged = MergeTypes(target.Stack[i], source.Stack[i]);
            if (!merged.Equals(target.Stack[i]))
            {
                target.Stack[i] = merged;
                changed = true;
            }
        }

        return changed;
    }

    private void TrackMaxStack(FrameState state)
    {
        var slots = state.Stack.Sum(GetSlotWidth);
        MaxStack = Math.Max(MaxStack, slots);
    }

    private int ComputeMaxLocals()
    {
        var maximum = _initialState.Locals.Count;

        foreach (var instruction in _body.Instructions)
        {
            maximum = Math.Max(maximum, GetRequiredLocalCount(instruction));
        }

        foreach (var variable in _body.LocalVariables)
        {
            var width = variable.Descriptor is null
                ? 1
                : GetSlotWidth(DescriptorParser.ParseType(variable.Descriptor));
            maximum = Math.Max(maximum, checked(variable.Index + width));
        }

        foreach (var typeAnnotation in _body.TypeAnnotations)
        {
            foreach (var (_, _, index) in typeAnnotation.LocalVariableTargets)
            {
                maximum = Math.Max(maximum, checked(index + 1));
            }
        }

        return maximum;
    }

    private static int GetRequiredLocalCount(Instruction instruction)
    {
        if (instruction is IincInstruction increment)
        {
            return checked(increment.VarIndex + 1);
        }

        if (instruction is VarInstruction variable)
        {
            return checked(variable.VarIndex + GetLocalWidth(variable.OpCode));
        }

        if (IsImplicitVariableOpCode(instruction.OpCode))
        {
            return checked(
                GetVariableIndex(instruction) + GetLocalWidth(instruction.OpCode));
        }

        return 0;
    }

    private static bool IsImplicitVariableOpCode(OperationCode opCode)
    {
        return opCode is >= OperationCode.ILOAD_0 and <= OperationCode.ALOAD_3
            or >= OperationCode.ISTORE_0 and <= OperationCode.ASTORE_3;
    }

    private static int GetLocalWidth(OperationCode opCode)
    {
        return opCode is OperationCode.LLOAD
            or OperationCode.DLOAD
            or OperationCode.LSTORE
            or OperationCode.DSTORE
            or >= OperationCode.LLOAD_0 and <= OperationCode.LLOAD_3
            or >= OperationCode.DLOAD_0 and <= OperationCode.DLOAD_3
            or >= OperationCode.LSTORE_0 and <= OperationCode.LSTORE_3
            or >= OperationCode.DSTORE_0 and <= OperationCode.DSTORE_3
            ? 2
            : 1;
    }

    private static int GetSlotWidth(JvmType type)
    {
        return IsCategory2(type) ? 2 : 1;
    }

    private static int GetSlotWidth(TypeDescriptor type)
    {
        return type.Tag is DescriptorTag.Long or DescriptorTag.Double ? 2 : 1;
    }

    private JvmType MergeTypes(JvmType first, JvmType second)
    {
        if (first.Equals(second))
        {
            return first;
        }

        if (first.Kind == JvmKind.Top || second.Kind == JvmKind.Top)
        {
            return JvmType.Top;
        }

        if (first.Kind == JvmKind.Null && second is JvmObject)
        {
            return second;
        }

        if (second.Kind == JvmKind.Null && first is JvmObject)
        {
            return first;
        }

        if (first is JvmObject firstObject && second is JvmObject secondObject)
        {
            if (firstObject.TypeName == secondObject.TypeName)
            {
                return first;
            }

            var commonType = _body.CommonSuperTypeResolver?.Invoke(
                firstObject.TypeName,
                secondObject.TypeName);
            return new JvmObject(commonType ?? "java/lang/Object");
        }

        return JvmType.Top;
    }

    private void ApplyEffect(Instruction instruction, FrameState state)
    {
        switch (instruction.OpCode)
        {
            case OperationCode.NOP:
            case OperationCode.IINC:
            case OperationCode.GOTO:
            case OperationCode.GOTO_W:
                return;

            case OperationCode.ACONST_NULL:
                state.Push(JvmType.Null);
                return;

            case OperationCode.ICONST_M1:
            case >= OperationCode.ICONST_0 and <= OperationCode.ICONST_5:
            case OperationCode.BIPUSH:
            case OperationCode.SIPUSH:
                state.Push(JvmType.Int);
                return;

            case OperationCode.LCONST_0:
            case OperationCode.LCONST_1:
                state.Push(JvmType.Long);
                return;

            case OperationCode.FCONST_0:
            case OperationCode.FCONST_1:
            case OperationCode.FCONST_2:
                state.Push(JvmType.Float);
                return;

            case OperationCode.DCONST_0:
            case OperationCode.DCONST_1:
                state.Push(JvmType.Double);
                return;

            case OperationCode.LDC:
            case OperationCode.LDC_W:
            case OperationCode.LDC2_W:
                ApplyLdc((LdcInstruction)instruction, state);
                return;

            case OperationCode.ILOAD:
            case >= OperationCode.ILOAD_0 and <= OperationCode.ILOAD_3:
                state.Push(JvmType.Int);
                return;

            case OperationCode.LLOAD:
            case >= OperationCode.LLOAD_0 and <= OperationCode.LLOAD_3:
                state.Push(JvmType.Long);
                return;

            case OperationCode.FLOAD:
            case >= OperationCode.FLOAD_0 and <= OperationCode.FLOAD_3:
                state.Push(JvmType.Float);
                return;

            case OperationCode.DLOAD:
            case >= OperationCode.DLOAD_0 and <= OperationCode.DLOAD_3:
                state.Push(JvmType.Double);
                return;

            case OperationCode.ALOAD:
            case >= OperationCode.ALOAD_0 and <= OperationCode.ALOAD_3:
                state.Push(GetLocal(state, GetVariableIndex(instruction)));
                return;

            case OperationCode.IALOAD:
            case OperationCode.BALOAD:
            case OperationCode.CALOAD:
            case OperationCode.SALOAD:
                state.Pop(2);
                state.Push(JvmType.Int);
                return;

            case OperationCode.LALOAD:
                state.Pop(2);
                state.Push(JvmType.Long);
                return;

            case OperationCode.FALOAD:
                state.Pop(2);
                state.Push(JvmType.Float);
                return;

            case OperationCode.DALOAD:
                state.Pop(2);
                state.Push(JvmType.Double);
                return;

            case OperationCode.AALOAD:
            {
                state.Pop();
                var array = state.Pop();
                state.Push(GetArrayComponentType(array));
                return;
            }

            case OperationCode.ISTORE:
            case >= OperationCode.ISTORE_0 and <= OperationCode.ISTORE_3:
                state.Pop();
                SetLocal(state, GetVariableIndex(instruction), JvmType.Int);
                return;

            case OperationCode.LSTORE:
            case >= OperationCode.LSTORE_0 and <= OperationCode.LSTORE_3:
                state.Pop();
                SetWideLocal(state, GetVariableIndex(instruction), JvmType.Long);
                return;

            case OperationCode.FSTORE:
            case >= OperationCode.FSTORE_0 and <= OperationCode.FSTORE_3:
                state.Pop();
                SetLocal(state, GetVariableIndex(instruction), JvmType.Float);
                return;

            case OperationCode.DSTORE:
            case >= OperationCode.DSTORE_0 and <= OperationCode.DSTORE_3:
                state.Pop();
                SetWideLocal(state, GetVariableIndex(instruction), JvmType.Double);
                return;

            case OperationCode.ASTORE:
            case >= OperationCode.ASTORE_0 and <= OperationCode.ASTORE_3:
                SetLocal(state, GetVariableIndex(instruction), state.Pop());
                return;

            case OperationCode.IASTORE:
            case OperationCode.LASTORE:
            case OperationCode.FASTORE:
            case OperationCode.DASTORE:
            case OperationCode.AASTORE:
            case OperationCode.BASTORE:
            case OperationCode.CASTORE:
            case OperationCode.SASTORE:
                state.Pop(3);
                return;

            case OperationCode.POP:
                RequireCategory1(state.Pop(), instruction.OpCode);
                return;

            case OperationCode.POP2:
                ApplyPop2(state);
                return;

            case OperationCode.DUP:
                ApplyDup(state);
                return;

            case OperationCode.DUP_X1:
                ApplyDupX1(state);
                return;

            case OperationCode.DUP_X2:
                ApplyDupX2(state);
                return;

            case OperationCode.DUP2:
                ApplyDup2(state);
                return;

            case OperationCode.DUP2_X1:
                ApplyDup2X1(state);
                return;

            case OperationCode.DUP2_X2:
                ApplyDup2X2(state);
                return;

            case OperationCode.SWAP:
                ApplySwap(state);
                return;

            case OperationCode.IADD:
            case OperationCode.ISUB:
            case OperationCode.IMUL:
            case OperationCode.IDIV:
            case OperationCode.IREM:
            case OperationCode.ISHL:
            case OperationCode.ISHR:
            case OperationCode.IUSHR:
            case OperationCode.IAND:
            case OperationCode.IOR:
            case OperationCode.IXOR:
                PopAndPush(state, 2, JvmType.Int);
                return;

            case OperationCode.LADD:
            case OperationCode.LSUB:
            case OperationCode.LMUL:
            case OperationCode.LDIV:
            case OperationCode.LREM:
            case OperationCode.LAND:
            case OperationCode.LOR:
            case OperationCode.LXOR:
                PopAndPush(state, 2, JvmType.Long);
                return;

            case OperationCode.LSHL:
            case OperationCode.LSHR:
            case OperationCode.LUSHR:
                PopAndPush(state, 2, JvmType.Long);
                return;

            case OperationCode.FADD:
            case OperationCode.FSUB:
            case OperationCode.FMUL:
            case OperationCode.FDIV:
            case OperationCode.FREM:
                PopAndPush(state, 2, JvmType.Float);
                return;

            case OperationCode.DADD:
            case OperationCode.DSUB:
            case OperationCode.DMUL:
            case OperationCode.DDIV:
            case OperationCode.DREM:
                PopAndPush(state, 2, JvmType.Double);
                return;

            case OperationCode.INEG:
            case OperationCode.I2B:
            case OperationCode.I2C:
            case OperationCode.I2S:
                PopAndPush(state, 1, JvmType.Int);
                return;

            case OperationCode.LNEG:
                PopAndPush(state, 1, JvmType.Long);
                return;

            case OperationCode.FNEG:
                PopAndPush(state, 1, JvmType.Float);
                return;

            case OperationCode.DNEG:
                PopAndPush(state, 1, JvmType.Double);
                return;

            case OperationCode.I2L:
            case OperationCode.F2L:
            case OperationCode.D2L:
                PopAndPush(state, 1, JvmType.Long);
                return;

            case OperationCode.I2F:
            case OperationCode.L2F:
            case OperationCode.D2F:
                PopAndPush(state, 1, JvmType.Float);
                return;

            case OperationCode.I2D:
            case OperationCode.L2D:
            case OperationCode.F2D:
                PopAndPush(state, 1, JvmType.Double);
                return;

            case OperationCode.L2I:
            case OperationCode.F2I:
            case OperationCode.D2I:
                PopAndPush(state, 1, JvmType.Int);
                return;

            case OperationCode.LCMP:
            case OperationCode.FCMPL:
            case OperationCode.FCMPG:
            case OperationCode.DCMPL:
            case OperationCode.DCMPG:
                PopAndPush(state, 2, JvmType.Int);
                return;

            case OperationCode.IFEQ:
            case OperationCode.IFNE:
            case OperationCode.IFLT:
            case OperationCode.IFGE:
            case OperationCode.IFGT:
            case OperationCode.IFLE:
            case OperationCode.IFNULL:
            case OperationCode.IFNONNULL:
                state.Pop();
                return;

            case OperationCode.IF_ICMPEQ:
            case OperationCode.IF_ICMPNE:
            case OperationCode.IF_ICMPLT:
            case OperationCode.IF_ICMPGE:
            case OperationCode.IF_ICMPGT:
            case OperationCode.IF_ICMPLE:
            case OperationCode.IF_ACMPEQ:
            case OperationCode.IF_ACMPNE:
                state.Pop(2);
                return;

            case OperationCode.TABLESWITCH:
            case OperationCode.LOOKUPSWITCH:
                state.Pop();
                return;

            case OperationCode.IRETURN:
            case OperationCode.LRETURN:
            case OperationCode.FRETURN:
            case OperationCode.DRETURN:
            case OperationCode.ARETURN:
            case OperationCode.ATHROW:
                state.Pop();
                return;

            case OperationCode.RETURN:
                return;

            case OperationCode.GETSTATIC:
                state.Push(FieldTypeToJvm(((FieldInstruction)instruction).Descriptor));
                return;

            case OperationCode.PUTSTATIC:
                state.Pop();
                return;

            case OperationCode.GETFIELD:
            {
                var field = (FieldInstruction)instruction;
                state.Pop();
                state.Push(FieldTypeToJvm(field.Descriptor));
                return;
            }

            case OperationCode.PUTFIELD:
                state.Pop(2);
                return;

            case OperationCode.INVOKEVIRTUAL:
            case OperationCode.INVOKESPECIAL:
            case OperationCode.INVOKESTATIC:
            case OperationCode.INVOKEINTERFACE:
                ApplyMethodInvocation((MethodInstruction)instruction, state);
                return;

            case OperationCode.INVOKEDYNAMIC:
                ApplyInvokeDynamic((InvokeDynamicInstruction)instruction, state);
                return;

            case OperationCode.NEW:
                state.Push(new JvmUninitialized(instruction.Offset!.Value));
                return;

            case OperationCode.NEWARRAY:
            {
                state.Pop();
                var arrayType = ((IntInstruction)instruction).Value switch
                {
                    4 => "[Z",
                    5 => "[C",
                    6 => "[F",
                    7 => "[D",
                    8 => "[B",
                    9 => "[S",
                    10 => "[I",
                    11 => "[J",
                    _ => throw new InvalidOperationException("Invalid NEWARRAY type code.")
                };
                state.Push(new JvmObject(arrayType));
                return;
            }

            case OperationCode.ANEWARRAY:
            {
                state.Pop();
                var type = ((TypeInstruction)instruction).Type
                    ?? throw new InvalidOperationException("ANEWARRAY type is unresolved.");
                var arrayType = type.StartsWith("[", StringComparison.Ordinal)
                    ? $"[{type}"
                    : $"[L{type};";
                state.Push(new JvmObject(arrayType));
                return;
            }

            case OperationCode.ARRAYLENGTH:
                PopAndPush(state, 1, JvmType.Int);
                return;

            case OperationCode.CHECKCAST:
            {
                state.Pop();
                var type = ((TypeInstruction)instruction).Type
                    ?? throw new InvalidOperationException("CHECKCAST type is unresolved.");
                state.Push(new JvmObject(type));
                return;
            }

            case OperationCode.INSTANCEOF:
                PopAndPush(state, 1, JvmType.Int);
                return;

            case OperationCode.MONITORENTER:
            case OperationCode.MONITOREXIT:
                state.Pop();
                return;

            case OperationCode.MULTIANEWARRAY:
            {
                var multiArray = (MultiANewArrayInstruction)instruction;
                state.Pop(multiArray.Dimensions);
                state.Push(new JvmObject(
                    multiArray.Type
                    ?? throw new InvalidOperationException("MULTIANEWARRAY type is unresolved.")));
                return;
            }

            case OperationCode.JSR:
            case OperationCode.JSR_W:
            case OperationCode.RET:
                throw new NotSupportedException(
                    "Stack map calculation does not support legacy JSR/RET subroutines.");

            default:
                throw new NotSupportedException(
                    $"Stack effect for opcode {instruction.OpCode} is not implemented.");
        }
    }

    private static void ApplyLdc(LdcInstruction instruction, FrameState state)
    {
        var type = instruction.Value switch
        {
            int => JvmType.Int,
            float => JvmType.Float,
            long => JvmType.Long,
            double => JvmType.Double,
            string => new JvmObject("java/lang/String"),
            _ when instruction.StackDescriptor is not null =>
                FieldTypeToJvm(instruction.StackDescriptor),
            _ when instruction.OpCode == OperationCode.LDC2_W => JvmType.Long,
            _ => new JvmObject("java/lang/Object")
        };
        state.Push(type);
    }

    private void ApplyMethodInvocation(MethodInstruction instruction, FrameState state)
    {
        var descriptor = instruction.Descriptor
            ?? throw new InvalidOperationException("Method descriptor is unresolved.");
        var parsedDescriptor = DescriptorParser.ParseMethod(descriptor);
        state.Pop(parsedDescriptor.Parameters.Length);

        JvmType? receiver = null;
        if (instruction.OpCode != OperationCode.INVOKESTATIC)
        {
            receiver = state.Pop();
        }

        if (instruction.OpCode == OperationCode.INVOKESPECIAL
            && instruction.Name == "<init>"
            && receiver is not null)
        {
            var initializedName = receiver is JvmUninitializedThis
                ? _body.OwnerInternalName
                : instruction.Owner;
            var initializedType = new JvmObject(
                initializedName
                ?? throw new InvalidOperationException("Constructor owner is unresolved."));
            state.Replace(receiver, initializedType);
        }

        PushReturnType(state, parsedDescriptor.ReturnType);
    }

    private static void ApplyInvokeDynamic(
        InvokeDynamicInstruction instruction,
        FrameState state)
    {
        var descriptor = instruction.Descriptor
            ?? throw new InvalidOperationException("InvokeDynamic descriptor is unresolved.");
        var parsedDescriptor = DescriptorParser.ParseMethod(descriptor);
        state.Pop(parsedDescriptor.Parameters.Length);
        PushReturnType(state, parsedDescriptor.ReturnType);
    }

    private static void PushReturnType(FrameState state, TypeDescriptor returnType)
    {
        if (returnType.Tag != DescriptorTag.Void)
        {
            state.Push(TypeDescriptorToJvm(returnType));
        }
    }

    private static JvmType GetLocal(FrameState state, int index)
    {
        if (index < 0 || index >= state.Locals.Count)
        {
            throw new InvalidOperationException($"Local variable {index} has no frame type.");
        }

        return state.Locals[index];
    }

    private static void SetLocal(FrameState state, int index, JvmType type)
    {
        InvalidateOverlappingWideLocal(state, index);
        state.SetLocal(index, type);
    }

    private static void SetWideLocal(FrameState state, int index, JvmType type)
    {
        InvalidateOverlappingWideLocal(state, index);
        state.SetLocal(index, type);
        state.SetLocal(index + 1, JvmType.Top);
    }

    private static void InvalidateOverlappingWideLocal(FrameState state, int index)
    {
        if (index > 0
            && index - 1 < state.Locals.Count
            && IsCategory2(state.Locals[index - 1]))
        {
            state.Locals[index - 1] = JvmType.Top;
        }
    }

    private static void PopAndPush(FrameState state, int popCount, JvmType type)
    {
        state.Pop(popCount);
        state.Push(type);
    }

    private static void ApplyPop2(FrameState state)
    {
        var first = state.Pop();
        if (!IsCategory2(first))
        {
            RequireCategory1(state.Pop(), OperationCode.POP2);
        }
    }

    private static void ApplyDup(FrameState state)
    {
        var value = state.Peek();
        RequireCategory1(value, OperationCode.DUP);
        state.Push(value);
    }

    private static void ApplyDupX1(FrameState state)
    {
        var value1 = state.Pop();
        var value2 = state.Pop();
        RequireCategory1(value1, OperationCode.DUP_X1);
        RequireCategory1(value2, OperationCode.DUP_X1);
        state.Push(value1);
        state.Push(value2);
        state.Push(value1);
    }

    private static void ApplyDupX2(FrameState state)
    {
        var value1 = state.Pop();
        RequireCategory1(value1, OperationCode.DUP_X2);
        var value2 = state.Pop();

        if (IsCategory2(value2))
        {
            state.Push(value1);
            state.Push(value2);
            state.Push(value1);
            return;
        }

        var value3 = state.Pop();
        RequireCategory1(value2, OperationCode.DUP_X2);
        RequireCategory1(value3, OperationCode.DUP_X2);
        state.Push(value1);
        state.Push(value3);
        state.Push(value2);
        state.Push(value1);
    }

    private static void ApplyDup2(FrameState state)
    {
        var value1 = state.Pop();
        if (IsCategory2(value1))
        {
            state.Push(value1);
            state.Push(value1);
            return;
        }

        var value2 = state.Pop();
        RequireCategory1(value2, OperationCode.DUP2);
        state.Push(value2);
        state.Push(value1);
        state.Push(value2);
        state.Push(value1);
    }

    private static void ApplyDup2X1(FrameState state)
    {
        var value1 = state.Pop();
        if (IsCategory2(value1))
        {
            var value2 = state.Pop();
            RequireCategory1(value2, OperationCode.DUP2_X1);
            state.Push(value1);
            state.Push(value2);
            state.Push(value1);
            return;
        }

        var value2Category1 = state.Pop();
        var value3 = state.Pop();
        RequireCategory1(value1, OperationCode.DUP2_X1);
        RequireCategory1(value2Category1, OperationCode.DUP2_X1);
        RequireCategory1(value3, OperationCode.DUP2_X1);
        state.Push(value2Category1);
        state.Push(value1);
        state.Push(value3);
        state.Push(value2Category1);
        state.Push(value1);
    }

    private static void ApplyDup2X2(FrameState state)
    {
        var value1 = state.Pop();
        if (IsCategory2(value1))
        {
            var value2 = state.Pop();
            if (IsCategory2(value2))
            {
                state.Push(value1);
                state.Push(value2);
                state.Push(value1);
                return;
            }

            var value3 = state.Pop();
            RequireCategory1(value2, OperationCode.DUP2_X2);
            RequireCategory1(value3, OperationCode.DUP2_X2);
            state.Push(value1);
            state.Push(value3);
            state.Push(value2);
            state.Push(value1);
            return;
        }

        var value2Category1 = state.Pop();
        RequireCategory1(value1, OperationCode.DUP2_X2);
        RequireCategory1(value2Category1, OperationCode.DUP2_X2);
        var value3Category = state.Pop();
        if (IsCategory2(value3Category))
        {
            state.Push(value2Category1);
            state.Push(value1);
            state.Push(value3Category);
            state.Push(value2Category1);
            state.Push(value1);
            return;
        }

        var value4 = state.Pop();
        RequireCategory1(value3Category, OperationCode.DUP2_X2);
        RequireCategory1(value4, OperationCode.DUP2_X2);
        state.Push(value2Category1);
        state.Push(value1);
        state.Push(value4);
        state.Push(value3Category);
        state.Push(value2Category1);
        state.Push(value1);
    }

    private static void ApplySwap(FrameState state)
    {
        var value1 = state.Pop();
        var value2 = state.Pop();
        RequireCategory1(value1, OperationCode.SWAP);
        RequireCategory1(value2, OperationCode.SWAP);
        state.Push(value1);
        state.Push(value2);
    }

    private static void RequireCategory1(JvmType type, OperationCode opCode)
    {
        if (IsCategory2(type))
        {
            throw new InvalidOperationException($"{opCode} requires category 1 stack values.");
        }
    }

    private static bool IsCategory2(JvmType type)
    {
        return type.Kind is JvmKind.Long or JvmKind.Double;
    }

    private static JvmType GetArrayComponentType(JvmType array)
    {
        if (array is not JvmObject { TypeName: ['[', ..] } arrayType)
        {
            return new JvmObject("java/lang/Object");
        }

        var componentDescriptor = arrayType.TypeName[1..];
        return componentDescriptor[0] switch
        {
            '[' => new JvmObject(componentDescriptor),
            'L' => new JvmObject(componentDescriptor[1..^1]),
            'F' => JvmType.Float,
            'J' => JvmType.Long,
            'D' => JvmType.Double,
            _ => JvmType.Int
        };
    }

    private static JvmType FieldTypeToJvm(string? descriptor)
    {
        if (string.IsNullOrEmpty(descriptor))
        {
            return JvmType.Top;
        }

        return TypeDescriptorToJvm(DescriptorParser.ParseType(descriptor));
    }

    private static JvmType TypeDescriptorToJvm(TypeDescriptor descriptor)
    {
        if (descriptor.IsArray)
        {
            return new JvmObject(descriptor.ToString());
        }

        if (descriptor.IsObject)
        {
            return new JvmObject(descriptor.InternalName!);
        }

        return descriptor.Tag switch
        {
            DescriptorTag.Boolean
                or DescriptorTag.Byte
                or DescriptorTag.Char
                or DescriptorTag.Int
                or DescriptorTag.Short => JvmType.Int,
            DescriptorTag.Float => JvmType.Float,
            DescriptorTag.Long => JvmType.Long,
            DescriptorTag.Double => JvmType.Double,
            _ => JvmType.Top
        };
    }

    private FrameState CreateInitialState(string descriptor, bool isStatic)
    {
        var state = new FrameState();
        if (!isStatic)
        {
            state.Locals.Add(_body.MethodName == "<init>"
                ? new JvmUninitializedThis()
                : new JvmObject(_body.OwnerInternalName ?? "java/lang/Object"));
        }

        foreach (var parameter in DescriptorParser.ParseMethod(descriptor).Parameters)
        {
            var type = TypeDescriptorToJvm(parameter);
            state.Locals.Add(type);
            if (IsCategory2(type))
            {
                state.Locals.Add(JvmType.Top);
            }
        }

        return state;
    }

    private static int GetVariableIndex(Instruction instruction)
    {
        if (instruction is VarInstruction variableInstruction)
        {
            return variableInstruction.VarIndex;
        }

        return instruction.OpCode switch
        {
            >= OperationCode.ILOAD_0 and <= OperationCode.ILOAD_3 =>
                instruction.OpCode - OperationCode.ILOAD_0,
            >= OperationCode.LLOAD_0 and <= OperationCode.LLOAD_3 =>
                instruction.OpCode - OperationCode.LLOAD_0,
            >= OperationCode.FLOAD_0 and <= OperationCode.FLOAD_3 =>
                instruction.OpCode - OperationCode.FLOAD_0,
            >= OperationCode.DLOAD_0 and <= OperationCode.DLOAD_3 =>
                instruction.OpCode - OperationCode.DLOAD_0,
            >= OperationCode.ALOAD_0 and <= OperationCode.ALOAD_3 =>
                instruction.OpCode - OperationCode.ALOAD_0,
            >= OperationCode.ISTORE_0 and <= OperationCode.ISTORE_3 =>
                instruction.OpCode - OperationCode.ISTORE_0,
            >= OperationCode.LSTORE_0 and <= OperationCode.LSTORE_3 =>
                instruction.OpCode - OperationCode.LSTORE_0,
            >= OperationCode.FSTORE_0 and <= OperationCode.FSTORE_3 =>
                instruction.OpCode - OperationCode.FSTORE_0,
            >= OperationCode.DSTORE_0 and <= OperationCode.DSTORE_3 =>
                instruction.OpCode - OperationCode.DSTORE_0,
            >= OperationCode.ASTORE_0 and <= OperationCode.ASTORE_3 =>
                instruction.OpCode - OperationCode.ASTORE_0,
            _ => throw new InvalidOperationException(
                $"Opcode {instruction.OpCode} does not reference a local variable.")
        };
    }

    private int GetResolvedOffset(Label label)
    {
        if (label.Offset.HasValue)
        {
            return label.Offset.Value;
        }

        throw new InvalidOperationException($"Label '{label.Name}' is unresolved.");
    }

    private Dictionary<int, int> BuildInstructionIndex()
    {
        var result = new Dictionary<int, int>();
        for (var i = 0; i < _body.Instructions.Count; i++)
        {
            var offset = _body.Instructions[i].Offset
                ?? throw new InvalidOperationException("Instruction offset is unresolved.");
            result.Add(offset, i);
        }

        return result;
    }

    private FullFrame BuildFullFrame(int offsetDelta, FrameState state)
    {
        if (offsetDelta is < 0 or > ushort.MaxValue)
        {
            throw new InvalidOperationException(
                $"Stack map offset_delta {offsetDelta} is outside the u2 range.");
        }

        return new FullFrame(
            new TUShort((ushort)offsetDelta),
            ConvertLocals(state.Locals),
            state.Stack.Select(ConvertType).ToArray());
    }

    private VerificationTypeInfo[] ConvertLocals(IReadOnlyList<JvmType> locals)
    {
        var lastIndex = locals.Count - 1;
        while (lastIndex >= 0 && locals[lastIndex].Kind == JvmKind.Top)
        {
            lastIndex--;
        }

        var result = new List<VerificationTypeInfo>();
        for (var i = 0; i <= lastIndex; i++)
        {
            var type = locals[i];
            result.Add(ConvertType(type));
            if (IsCategory2(type)
                && i + 1 <= lastIndex
                && locals[i + 1].Kind == JvmKind.Top)
            {
                i++;
            }
        }

        return result.ToArray();
    }

    private VerificationTypeInfo ConvertType(JvmType type)
    {
        return type switch
        {
            JvmObject objectType => new ObjectVariableInfo(
                new TUShort((ushort)_constantPool.AddClass(objectType.TypeName))),
            JvmUninitialized uninitialized => new UninitializedVariableInfo(
                new TUShort((ushort)uninitialized.NewOffset)),
            _ => type.Kind switch
            {
                JvmKind.Int => new IntegerVariableInfo(),
                JvmKind.Float => new FloatVariableInfo(),
                JvmKind.Long => new LongVariableInfo(),
                JvmKind.Double => new DoubleVariableInfo(),
                JvmKind.Null => new NullVariableInfo(),
                JvmKind.UninitializedThis => new UninitializedThisVariableInfo(),
                _ => new TopVariableInfo()
            }
        };
    }
}
