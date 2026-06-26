using Anvil.Descriptors;
using Anvil.Instructions;
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
    private readonly FrameState _initial;
    private readonly Dictionary<int, FrameState> _frames = new();
    private readonly ConstantPoolBuilder _cp;

    public StackFrameCalculator(MethodBody body, string methodDescriptor, bool isStatic, ConstantPoolBuilder cp)
    {
        _body = body;
        _cp = cp;
        _initial = InitLocals(methodDescriptor, isStatic);
    }

    public StackMapTableAttribute Compute()
    {
        if (_body.Instructions.Any(insn => insn.Offset == null))
        {
            _body.ResolveLabels();
        }

        var targets = ComputeTargets();
        var worklist = new Queue<int>();
        var iteration = 0;
        const int maxIterations = 2000;

        SetFrame(0, _initial);
        worklist.Enqueue(0);

        foreach (var block in _body.TryCatchBlocks)
        {
            var handlerState = _initial.Clone();
            var catchType = block.CatchType ?? "java/lang/Throwable";
            handlerState.Stack.Clear();
            handlerState.Stack.Add(new JvmObject(catchType));
            SetFrame(block.Handler.Offset!.Value, handlerState);
            worklist.Enqueue(block.Handler.Offset!.Value);
        }

        var insnList = _body.Instructions;
        var insnIndex = BuildInstructionIndex();

        while (worklist.Count > 0 && iteration++ < maxIterations)
        {
            var pc = worklist.Dequeue();
            if (!insnIndex.TryGetValue(pc, out var idx))
            {
                continue;
            }

            if (!_frames.TryGetValue(pc, out var state))
            {
                continue;
            }

            state = state.Clone();
            var i = idx;
            var keepGoing = true;

            while (keepGoing && i < insnList.Count)
            {
                var insn = insnList[i];
                var offset = insn.Offset!.Value;

                if (offset != pc && _frames.ContainsKey(offset))
                {
                    if (MergeInto(_frames[offset], state))
                    {
                        worklist.Enqueue(offset);
                    }

                    keepGoing = false;
                    break;
                }

                var result = ApplyEffect(insn, state);
                if (!result.KeepGoing)
                {
                    keepGoing = false;
                }

                i++;
            }
        }

        var entries = new List<StackMapFrame>();
        var sortedTargets = targets.Where(t => _frames.ContainsKey(t)).OrderBy(t => t).ToList();
        var prevPc = -1;

        foreach (var targetPc in sortedTargets)
        {
            var frameState = _frames[targetPc];
            var delta = prevPc < 0 ? targetPc : targetPc - prevPc;
            prevPc = targetPc;
            entries.Add(BuildFullFrame(delta, frameState));
        }

        return new StackMapTableAttribute(entries.ToArray());
    }

    private HashSet<int> ComputeTargets()
    {
        var targets = new HashSet<int>();
        foreach (var insn in _body.Instructions)
        {
            switch (insn)
            {
                case JumpInstruction ji:
                    targets.Add(ji.Target.Offset!.Value);
                    break;
                case TableSwitchInstruction ts:
                    targets.Add(ts.DefaultTarget.Offset!.Value);
                    foreach (var t in ts.Targets) targets.Add(t.Offset!.Value);
                    break;
                case LookupSwitchInstruction ls:
                    targets.Add(ls.DefaultTarget.Offset!.Value);
                    foreach (var (_, label) in ls.Pairs) targets.Add(label.Offset!.Value);
                    break;
            }
        }

        foreach (var block in _body.TryCatchBlocks)
        {
            targets.Add(block.Start.Offset!.Value);
            targets.Add(block.Handler.Offset!.Value);
        }

        return targets;
    }

    private static Effect ApplyEffect(Instruction insn, FrameState state)
    {
        var op = insn.OpCode;

        switch (op)
        {
            // No-op
            case OperationCode.NOP:
                return Effect.Continue;

            // Constants
            case OperationCode.ACONST_NULL:
                return state.Push(JvmType.Null);
            case OperationCode.ICONST_M1:
            case >= OperationCode.ICONST_0 and <= OperationCode.ICONST_5:
            case OperationCode.BIPUSH:
            case OperationCode.SIPUSH:
                return state.Push(JvmType.Int);
            case OperationCode.LCONST_0:
            case OperationCode.LCONST_1:
                return state.PushWide(JvmType.Long);
            case OperationCode.FCONST_0:
            case OperationCode.FCONST_1:
            case OperationCode.FCONST_2:
                return state.Push(JvmType.Float);
            case OperationCode.DCONST_0:
            case OperationCode.DCONST_1:
                return state.PushWide(JvmType.Double);

            // LDC
            case OperationCode.LDC:
            case OperationCode.LDC_W:
            {
                var ldc = (LdcInstruction)insn;
                var type = ldc.Value switch
                {
                    string => new JvmObject("java/lang/String"),
                    int => JvmType.Int,
                    float => JvmType.Float,
                    null => JvmType.Top,
                    _ => new JvmObject("java/lang/Class")
                };
                return state.Push(type);
            }
            case OperationCode.LDC2_W:
            {
                var ldc = (LdcInstruction)insn;
                var type = ldc.Value is double ? JvmType.Double : JvmType.Long;
                return state.PushWide(type);
            }

            // Loads
            case OperationCode.ILOAD:
            case >= OperationCode.ILOAD_0 and <= OperationCode.ILOAD_3:
                return state.Push(JvmType.Int);
            case OperationCode.LLOAD:
            case >= OperationCode.LLOAD_0 and <= OperationCode.LLOAD_3:
                return state.PushWide(JvmType.Long);
            case OperationCode.FLOAD:
            case >= OperationCode.FLOAD_0 and <= OperationCode.FLOAD_3:
                return state.Push(JvmType.Float);
            case OperationCode.DLOAD:
            case >= OperationCode.DLOAD_0 and <= OperationCode.DLOAD_3:
                return state.PushWide(JvmType.Double);
            case OperationCode.ALOAD:
            case >= OperationCode.ALOAD_0 and <= OperationCode.ALOAD_3:
            {
                var idx = GetVarIndex(insn);
                var local = idx < state.Locals.Count ? state.Locals[idx] : JvmType.Top;
                return state.Push(local);
            }

            // Array loads
            case OperationCode.IALOAD:
                return state.PopN(2) && state.Push(JvmType.Int);
            case OperationCode.LALOAD:
                return state.PopN(2) && state.PushWide(JvmType.Long);
            case OperationCode.FALOAD:
                return state.PopN(2) && state.Push(JvmType.Float);
            case OperationCode.DALOAD:
                return state.PopN(2) && state.PushWide(JvmType.Double);
            case OperationCode.AALOAD:
                return state.PopN(2) && state.Push(JvmType.Top);
            case OperationCode.BALOAD:
            case OperationCode.CALOAD:
            case OperationCode.SALOAD:
                return state.PopN(2) && state.Push(JvmType.Int);

            // Stores
            case OperationCode.ISTORE:
            case >= OperationCode.ISTORE_0 and <= OperationCode.ISTORE_3:
            {
                var idx = GetStoreVarIndex(insn);
                state.Pop();
                state.SetLocal(idx, JvmType.Int);
                return Effect.Continue;
            }
            case OperationCode.LSTORE:
            case >= OperationCode.LSTORE_0 and <= OperationCode.LSTORE_3:
            {
                var idx = GetStoreVarIndex(insn);
                state.PopWide();
                state.SetLocal(idx, JvmType.Long);
                state.SetLocal(idx + 1, JvmType.Top);
                return Effect.Continue;
            }
            case OperationCode.FSTORE:
            case >= OperationCode.FSTORE_0 and <= OperationCode.FSTORE_3:
            {
                var idx = GetStoreVarIndex(insn);
                state.Pop();
                state.SetLocal(idx, JvmType.Float);
                return Effect.Continue;
            }
            case OperationCode.DSTORE:
            case >= OperationCode.DSTORE_0 and <= OperationCode.DSTORE_3:
            {
                var idx = GetStoreVarIndex(insn);
                state.PopWide();
                state.SetLocal(idx, JvmType.Double);
                state.SetLocal(idx + 1, JvmType.Top);
                return Effect.Continue;
            }
            case OperationCode.ASTORE:
            case >= OperationCode.ASTORE_0 and <= OperationCode.ASTORE_3:
            {
                var idx = GetStoreVarIndex(insn);
                var stackType = state.Stack.Count > 0 ? state.Stack[^1] : JvmType.Top;
                state.Pop();
                state.SetLocal(idx, stackType);
                return Effect.Continue;
            }

            // Array stores
            case OperationCode.IASTORE:
            case OperationCode.FASTORE:
            case OperationCode.AASTORE:
            case OperationCode.BASTORE:
            case OperationCode.CASTORE:
            case OperationCode.SASTORE:
                return state.PopN(3);
            case OperationCode.LASTORE:
            case OperationCode.DASTORE:
                return state.PopN(4);

            // Stack ops
            case OperationCode.POP:
                return state.Pop();
            case OperationCode.POP2:
                return state.PopN(2);
            case OperationCode.DUP:
                if (state.Stack.Count == 0) return Effect.Continue;
                return state.Push(state.Stack[^1]);
            case OperationCode.DUP_X1:
                return ApplyDupX1(state);
            case OperationCode.DUP_X2:
                return ApplyDupX2(state);
            case OperationCode.DUP2:
                return ApplyDup2(state);
            case OperationCode.DUP2_X1:
                return ApplyDup2X1(state);
            case OperationCode.DUP2_X2:
                return ApplyDup2X2(state);
            case OperationCode.SWAP:
                if (state.Stack.Count < 2) return Effect.Continue;
                var top = state.Stack[^1]; state.Stack.RemoveAt(state.Stack.Count - 1);
                var second = state.Stack[^1]; state.Stack.RemoveAt(state.Stack.Count - 1);
                state.Stack.Add(top);
                state.Stack.Add(second);
                return Effect.Continue;

            // Arithmetic
            case OperationCode.IADD: case OperationCode.ISUB: case OperationCode.IMUL:
            case OperationCode.IDIV: case OperationCode.IREM:
            case OperationCode.ISHL: case OperationCode.ISHR: case OperationCode.IUSHR:
            case OperationCode.IAND: case OperationCode.IOR: case OperationCode.IXOR:
                return state.PopN(2) && state.Push(JvmType.Int);
            case OperationCode.LADD: case OperationCode.LSUB: case OperationCode.LMUL:
            case OperationCode.LDIV: case OperationCode.LREM:
            case OperationCode.LSHL: case OperationCode.LSHR: case OperationCode.LUSHR:
            case OperationCode.LAND: case OperationCode.LOR: case OperationCode.LXOR:
                return state.PopWide2() && state.PushWide(JvmType.Long);
            case OperationCode.FADD: case OperationCode.FSUB: case OperationCode.FMUL:
            case OperationCode.FDIV: case OperationCode.FREM:
                return state.PopN(2) && state.Push(JvmType.Float);
            case OperationCode.DADD: case OperationCode.DSUB: case OperationCode.DMUL:
            case OperationCode.DDIV: case OperationCode.DREM:
                return state.PopWide2() && state.PushWide(JvmType.Double);
            case OperationCode.INEG:
                return state.Pop() && state.Push(JvmType.Int);
            case OperationCode.LNEG:
                return state.PopWide() && state.PushWide(JvmType.Long);
            case OperationCode.FNEG:
                return state.Pop() && state.Push(JvmType.Float);
            case OperationCode.DNEG:
                return state.PopWide() && state.PushWide(JvmType.Double);

            case OperationCode.IINC:
                return Effect.Continue;

            // Conversions
            case OperationCode.I2L: return state.Pop() && state.PushWide(JvmType.Long);
            case OperationCode.I2F: return state.Pop() && state.Push(JvmType.Float);
            case OperationCode.I2D: return state.Pop() && state.PushWide(JvmType.Double);
            case OperationCode.L2I: return state.PopWide() && state.Push(JvmType.Int);
            case OperationCode.L2F: return state.PopWide() && state.Push(JvmType.Float);
            case OperationCode.L2D: return state.PopWide() && state.PushWide(JvmType.Double);
            case OperationCode.F2I: return state.Pop() && state.Push(JvmType.Int);
            case OperationCode.F2L: return state.Pop() && state.PushWide(JvmType.Long);
            case OperationCode.F2D: return state.Pop() && state.PushWide(JvmType.Double);
            case OperationCode.D2I: return state.PopWide() && state.Push(JvmType.Int);
            case OperationCode.D2L: return state.PopWide() && state.PushWide(JvmType.Long);
            case OperationCode.D2F: return state.PopWide() && state.Push(JvmType.Float);
            case OperationCode.I2B: case OperationCode.I2C: case OperationCode.I2S:
                return state.Pop() && state.Push(JvmType.Int);

            // Comparisons
            case OperationCode.LCMP:
                return state.PopWide2() && state.Push(JvmType.Int);
            case OperationCode.FCMPL: case OperationCode.FCMPG:
            case OperationCode.DCMPL: case OperationCode.DCMPG:
                return state.PopN(2) && state.Push(JvmType.Int);

            // Branches
            case OperationCode.IFEQ: case OperationCode.IFNE:
            case OperationCode.IFLT: case OperationCode.IFGE:
            case OperationCode.IFGT: case OperationCode.IFLE:
            case OperationCode.IFNULL: case OperationCode.IFNONNULL:
                state.Pop();
                return Effect.Continue;
            case OperationCode.IF_ICMPEQ: case OperationCode.IF_ICMPNE:
            case OperationCode.IF_ICMPLT: case OperationCode.IF_ICMPGE:
            case OperationCode.IF_ICMPGT: case OperationCode.IF_ICMPLE:
            case OperationCode.IF_ACMPEQ: case OperationCode.IF_ACMPNE:
                state.PopN(2);
                return Effect.Continue;
            case OperationCode.GOTO:
            case OperationCode.GOTO_W:
                return Effect.Stop;
            case OperationCode.JSR:
            case OperationCode.JSR_W:
                return Effect.Continue;
            case OperationCode.RET:
                return Effect.Continue;
            case OperationCode.TABLESWITCH:
            case OperationCode.LOOKUPSWITCH:
                state.Pop();
                return Effect.Stop;

            // Returns
            case OperationCode.IRETURN: case OperationCode.FRETURN:
            case OperationCode.ARETURN: case OperationCode.LRETURN:
            case OperationCode.DRETURN: case OperationCode.RETURN:
                return Effect.Stop;

            // Field
            case OperationCode.GETSTATIC:
            {
                var fi = (FieldInstruction)insn;
                var type = FieldTypeToJvm(fi.Descriptor);
                return state.Push(type);
            }
            case OperationCode.PUTSTATIC:
            {
                var fi = (FieldInstruction)insn;
                return IsWideDesc(fi.Descriptor) ? state.PopWide() : state.Pop();
            }
            case OperationCode.GETFIELD:
                return state.Pop() && state.Push(JvmType.Top);
            case OperationCode.PUTFIELD:
                return state.PopN(2);

            // Method invocation
            case OperationCode.INVOKEVIRTUAL: case OperationCode.INVOKESPECIAL:
            case OperationCode.INVOKESTATIC: case OperationCode.INVOKEINTERFACE:
            {
                var mi = (MethodInstruction)insn;
                PopArgs(state, mi.Descriptor ?? "()V");
                return PushReturn(state, mi.Descriptor ?? "()V");
            }
            case OperationCode.INVOKEDYNAMIC:
                return state.Push(JvmType.Top);

            // Object/Array
            case OperationCode.NEW:
            {
                return state.Push(new JvmUninitialized(insn.Offset!.Value));
            }
            case OperationCode.NEWARRAY:
                return state.Pop() && state.Push(new JvmObject("[I"));
            case OperationCode.ANEWARRAY:
            {
                var ti = (TypeInstruction)insn;
                var arrayType = ti.Type != null ? $"[L{ti.Type};" : "[Ljava/lang/Object;";
                return state.Pop() && state.Push(new JvmObject(arrayType));
            }
            case OperationCode.ARRAYLENGTH:
                return state.Pop() && state.Push(JvmType.Int);
            case OperationCode.ATHROW:
                return state.Pop() && Effect.Stop;
            case OperationCode.CHECKCAST:
                return Effect.Continue;
            case OperationCode.INSTANCEOF:
                return state.Pop() && state.Push(JvmType.Int);
            case OperationCode.MONITORENTER: case OperationCode.MONITOREXIT:
                return state.Pop();
            case OperationCode.MULTIANEWARRAY:
            {
                var mana = (MultiANewArrayInstruction)insn;
                for (var d = 0; d < mana.Dimensions; d++) state.Pop();
                return state.Push(new JvmObject(mana.Type ?? "Ljava/lang/Object;"));
            }

            default:
                return Effect.Continue;
        }
    }

    private static Effect ApplyDupX1(FrameState state)
    {
        if (state.Stack.Count < 2) return Effect.Continue;
        var v1 = PopLast(state);
        var v2 = PopLast(state);
        state.Stack.Add(v1);
        state.Stack.Add(v2);
        state.Stack.Add(v1);
        return Effect.Continue;
    }

    private static Effect ApplyDupX2(FrameState state)
    {
        if (state.Stack.Count >= 2 && IsStackTopWide(state))
        {
            var v1w = PopLast(state);
            var v2w = PopLast(state);
            state.Stack.Add(v1w);
            state.Stack.Add(v2w);
            state.Stack.Add(v1w);
            return Effect.Continue;
        }

        if (state.Stack.Count >= 3)
        {
            var v1 = PopLast(state);
            var v2 = PopLast(state);
            var v3 = PopLast(state);
            state.Stack.Add(v1);
            state.Stack.Add(v3);
            state.Stack.Add(v2);
            state.Stack.Add(v1);
        }

        return Effect.Continue;
    }

    private static Effect ApplyDup2(FrameState state)
    {
        if (state.Stack.Count >= 1 && IsStackTopWide(state))
        {
            var v1 = PopLast(state);
            var v2 = PopLast(state);
            state.Stack.Add(v2);
            state.Stack.Add(v1);
            state.Stack.Add(v2);
            state.Stack.Add(v1);
            return Effect.Continue;
        }

        if (state.Stack.Count >= 2)
        {
            var v2 = PopLast(state);
            var v1 = PopLast(state);
            state.Stack.Add(v1);
            state.Stack.Add(v2);
            state.Stack.Add(v1);
            state.Stack.Add(v2);
        }

        return Effect.Continue;
    }

    private static Effect ApplyDup2X1(FrameState state)
    {
        if (state.Stack.Count >= 1 && IsStackTopWide(state))
        {
            if (state.Stack.Count < 3) return Effect.Continue;
            var v1 = PopLast(state);
            var v2 = PopLast(state);
            var v3 = PopLast(state);
            state.Stack.Add(v2);
            state.Stack.Add(v1);
            state.Stack.Add(v3);
            state.Stack.Add(v2);
            state.Stack.Add(v1);
            return Effect.Continue;
        }

        if (state.Stack.Count >= 3)
        {
            var v2 = PopLast(state);
            var v1 = PopLast(state);
            var v3 = PopLast(state);
            state.Stack.Add(v1);
            state.Stack.Add(v2);
            state.Stack.Add(v3);
            state.Stack.Add(v1);
            state.Stack.Add(v2);
        }

        return Effect.Continue;
    }

    private static Effect ApplyDup2X2(FrameState state)
    {
        if (state.Stack.Count >= 1 && IsStackTopWide(state))
        {
            if (state.Stack.Count < 3) return Effect.Continue;
            if (state.Stack.Count >= 3 && IsSecondStackTopWide(state))
            {
                var v1 = PopLast(state);
                var v2 = PopLast(state);
                var v3 = PopLast(state);
                var v4 = PopLast(state);
                state.Stack.Add(v2);
                state.Stack.Add(v1);
                state.Stack.Add(v4);
                state.Stack.Add(v3);
                state.Stack.Add(v2);
                state.Stack.Add(v1);
                return Effect.Continue;
            }

            var v1a = PopLast(state);
            var v2a = PopLast(state);
            var v3a = PopLast(state);
            state.Stack.Add(v2a);
            state.Stack.Add(v1a);
            state.Stack.Add(v3a);
            state.Stack.Add(v2a);
            state.Stack.Add(v1a);
            return Effect.Continue;
        }

        if (state.Stack.Count >= 3 && IsStackTopWideAtDepth(state, 3))
        {
            if (state.Stack.Count < 4) return Effect.Continue;
            var v1 = PopLast(state);
            var v2 = PopLast(state);
            var v3 = PopLast(state);
            var v4 = PopLast(state);
            state.Stack.Add(v1);
            state.Stack.Add(v2);
            state.Stack.Add(v4);
            state.Stack.Add(v3);
            state.Stack.Add(v1);
            state.Stack.Add(v2);
            return Effect.Continue;
        }

        if (state.Stack.Count >= 4)
        {
            var v2 = PopLast(state);
            var v1 = PopLast(state);
            var v4 = PopLast(state);
            var v3 = PopLast(state);
            state.Stack.Add(v1);
            state.Stack.Add(v2);
            state.Stack.Add(v3);
            state.Stack.Add(v4);
            state.Stack.Add(v1);
            state.Stack.Add(v2);
        }

        return Effect.Continue;
    }

    private static JvmType PopLast(FrameState state)
    {
        var v = state.Stack[^1];
        state.Stack.RemoveAt(state.Stack.Count - 1);
        return v;
    }

    private static bool IsStackTopWide(FrameState state)
    {
        return state.Stack.Count >= 2
            && state.Stack[^2] is { Kind: JvmKind.Long or JvmKind.Double }
            && state.Stack[^1].Kind == JvmKind.Top;
    }

    private static bool IsSecondStackTopWide(FrameState state)
    {
        return state.Stack.Count >= 4
            && state.Stack[^4] is { Kind: JvmKind.Long or JvmKind.Double }
            && state.Stack[^3].Kind == JvmKind.Top;
    }

    private static bool IsStackTopWideAtDepth(FrameState state, int depth)
    {
        var baseIndex = state.Stack.Count - depth;
        return baseIndex >= 1
            && state.Stack[baseIndex - 1] is { Kind: JvmKind.Long or JvmKind.Double }
            && state.Stack[baseIndex].Kind == JvmKind.Top;
    }

    private static JvmType FieldTypeToJvm(string? descriptor)
    {
        if (descriptor == null) return JvmType.Top;
        return descriptor switch
        {
            "J" => JvmType.Long,
            "D" => JvmType.Double,
            "F" => JvmType.Float,
            "I" or "Z" or "B" or "C" or "S" => JvmType.Int,
            _ => JvmType.Top
        };
    }

    private static void PopArgs(FrameState state, string descriptor)
    {
        var methodDesc = DescriptorParser.ParseMethod(descriptor);
        for (var i = methodDesc.Parameters.Length - 1; i >= 0; i--)
        {
            var param = methodDesc.Parameters[i];
            if (param.Tag == DescriptorTag.Long || param.Tag == DescriptorTag.Double)
            {
                state.PopWide();
            }
            else
            {
                state.Pop();
            }
        }
    }

    private static Effect PushReturn(FrameState state, string descriptor)
    {
        var methodDesc = DescriptorParser.ParseMethod(descriptor);
        var retType = methodDesc.ReturnType;

        if (retType.Tag == DescriptorTag.Void) return Effect.Continue;

        var jvmType = TypeDescriptorToJvm(retType);
        if (retType.Tag == DescriptorTag.Long || retType.Tag == DescriptorTag.Double)
        {
            return state.PushWide(jvmType);
        }

        return state.Push(jvmType);
    }

    private static bool IsWideDesc(string? desc)
    {
        return desc == "J" || desc == "D";
    }

    private static int GetVarIndex(Instruction insn)
    {
        if (insn is VarInstruction v) return v.VarIndex;
        if (insn.OpCode >= OperationCode.ILOAD_0 && insn.OpCode <= OperationCode.ILOAD_3) return insn.OpCode - OperationCode.ILOAD_0;
        if (insn.OpCode >= OperationCode.LLOAD_0 && insn.OpCode <= OperationCode.LLOAD_3) return insn.OpCode - OperationCode.LLOAD_0;
        if (insn.OpCode >= OperationCode.FLOAD_0 && insn.OpCode <= OperationCode.FLOAD_3) return insn.OpCode - OperationCode.FLOAD_0;
        if (insn.OpCode >= OperationCode.DLOAD_0 && insn.OpCode <= OperationCode.DLOAD_3) return insn.OpCode - OperationCode.DLOAD_0;
        if (insn.OpCode >= OperationCode.ALOAD_0 && insn.OpCode <= OperationCode.ALOAD_3) return insn.OpCode - OperationCode.ALOAD_0;
        return 0;
    }

    private static int GetStoreVarIndex(Instruction insn)
    {
        if (insn is VarInstruction v) return v.VarIndex;
        if (insn.OpCode >= OperationCode.ISTORE_0 && insn.OpCode <= OperationCode.ISTORE_3) return insn.OpCode - OperationCode.ISTORE_0;
        if (insn.OpCode >= OperationCode.LSTORE_0 && insn.OpCode <= OperationCode.LSTORE_3) return insn.OpCode - OperationCode.LSTORE_0;
        if (insn.OpCode >= OperationCode.FSTORE_0 && insn.OpCode <= OperationCode.FSTORE_3) return insn.OpCode - OperationCode.FSTORE_0;
        if (insn.OpCode >= OperationCode.DSTORE_0 && insn.OpCode <= OperationCode.DSTORE_3) return insn.OpCode - OperationCode.DSTORE_0;
        if (insn.OpCode >= OperationCode.ASTORE_0 && insn.OpCode <= OperationCode.ASTORE_3) return insn.OpCode - OperationCode.ASTORE_0;
        return 0;
    }

    private static FrameState InitLocals(string descriptor, bool isStatic)
    {
        var state = new FrameState();
        var methodDesc = DescriptorParser.ParseMethod(descriptor);

        if (!isStatic)
        {
            state.Locals.Add(new JvmUninitializedThis());
        }

        foreach (var param in methodDesc.Parameters)
        {
            var type = TypeDescriptorToJvm(param);
            state.Locals.Add(type);

            if (param.Tag == DescriptorTag.Long || param.Tag == DescriptorTag.Double)
            {
                state.Locals.Add(JvmType.Top);
            }
        }

        return state;
    }

    private static JvmType TypeDescriptorToJvm(TypeDescriptor td)
    {
        if (td.IsArray)
        {
            return new JvmObject(td.ToString());
        }

        if (td.IsObject)
        {
            return new JvmObject(td.InternalName!);
        }

        return td.Tag switch
        {
            DescriptorTag.Boolean or DescriptorTag.Byte or DescriptorTag.Char
                or DescriptorTag.Int or DescriptorTag.Short => JvmType.Int,
            DescriptorTag.Float => JvmType.Float,
            DescriptorTag.Long => JvmType.Long,
            DescriptorTag.Double => JvmType.Double,
            _ => JvmType.Top
        };
    }

    private void SetFrame(int pc, FrameState state)
    {
        if (!_frames.ContainsKey(pc))
        {
            _frames[pc] = state.Clone();
        }
    }

    private static bool MergeInto(FrameState target, FrameState source)
    {
        if (target.Stack.Count != source.Stack.Count) return false;

        while (target.Locals.Count < source.Locals.Count)
        {
            target.Locals.Add(JvmType.Top);
        }

        while (source.Locals.Count < target.Locals.Count)
        {
            source.Locals.Add(JvmType.Top);
        }

        var changed = false;
        for (var i = 0; i < target.Locals.Count; i++)
        {
            var merged = MergeTypes(target.Locals[i], source.Locals[i]);
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

    private static JvmType MergeTypes(JvmType a, JvmType b)
    {
        if (a.Equals(b)) return a;
        if (a == JvmType.Top) return b;
        if (b == JvmType.Top) return a;
        if (a == JvmType.Null && b is JvmObject) return b;
        if (b == JvmType.Null && a is JvmObject) return a;
        if (a is JvmObject oa && b is JvmObject ob)
        {
            if (oa.TypeName == ob.TypeName) return a;
            return new JvmObject("java/lang/Object");
        }

        return JvmType.Top;
    }

    private Dictionary<int, int> BuildInstructionIndex()
    {
        var index = new Dictionary<int, int>();
        for (var i = 0; i < _body.Instructions.Count; i++)
        {
            var insn = _body.Instructions[i];
            if (insn.Offset.HasValue)
            {
                index[insn.Offset.Value] = i;
            }
        }

        return index;
    }

    private FullFrame BuildFullFrame(int offsetDelta, FrameState state)
    {
        var locals = state.Locals.Select(ConvertType).ToArray();
        var stack = state.Stack.Select(ConvertType).ToArray();
        return new FullFrame(
            new TUShort((ushort)offsetDelta),
            locals,
            stack);
    }

    private VerificationTypeInfo ConvertType(JvmType type)
    {
        return type switch
        {
            JvmObject obj => new ObjectVariableInfo(new TUShort((ushort)_cp.AddClass(obj.TypeName))),
            JvmUninitialized uninit => new UninitializedVariableInfo(new TUShort((ushort)uninit.NewOffset)),
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

