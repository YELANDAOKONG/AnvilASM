using Anvil.Structures.Attributes;
using Anvil.Structures.Attributes.StackMap;
using Anvil.Structures.Attributes.StackMap.Frames;
using Anvil.Structures.Attributes.StackMap.Types;
using Anvil.Types;

namespace Anvil.Instructions;

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
        var targets = ComputeTargets();
        var worklist = new Queue<int>();
        var iteration = 0;
        const int maxIterations = 2000;

        SetFrame(0, _initial);
        worklist.Enqueue(0);

        foreach (var block in _body.TryCatchBlocks)
        {
            var handlerState = new FrameState();
            var catchType = block.CatchType ?? "java/lang/Throwable";
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
                return state.Pop();
            case OperationCode.LSTORE:
            case >= OperationCode.LSTORE_0 and <= OperationCode.LSTORE_3:
                return state.PopWide();
            case OperationCode.FSTORE:
            case >= OperationCode.FSTORE_0 and <= OperationCode.FSTORE_3:
                return state.Pop();
            case OperationCode.DSTORE:
            case >= OperationCode.DSTORE_0 and <= OperationCode.DSTORE_3:
                return state.PopWide();
            case OperationCode.ASTORE:
            case >= OperationCode.ASTORE_0 and <= OperationCode.ASTORE_3:
                return state.Pop();

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
                return state.Stack.Count >= 2 ? Effect.Continue : Effect.Continue;
            case OperationCode.DUP_X2:
                return state.Stack.Count >= 3 ? Effect.Continue : Effect.Continue;
            case OperationCode.DUP2:
                return Effect.Continue;
            case OperationCode.DUP2_X1:
                return Effect.Continue;
            case OperationCode.DUP2_X2:
                return Effect.Continue;
            case OperationCode.SWAP:
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
                var ti = (TypeInstruction)insn;
                return state.Push(new JvmUninitialized(insn.Offset!.Value));
            }
            case OperationCode.NEWARRAY:
                return state.Pop() && state.Push(new JvmObject("[I"));
            case OperationCode.ANEWARRAY:
                return state.Pop() && state.Push(new JvmObject("[Ljava/lang/Object;"));
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
        var idx = descriptor.IndexOf(')');
        var paramDesc = descriptor[1..idx];
        var pos = 0;
        while (pos < paramDesc.Length)
        {
            var ch = paramDesc[pos];
            switch (ch)
            {
                case 'B': case 'C': case 'S': case 'I': case 'Z': case 'F':
                    state.Pop();
                    break;
                case 'J': case 'D':
                    state.PopWide();
                    break;
                case 'L':
                    pos = paramDesc.IndexOf(';', pos);
                    state.Pop();
                    break;
                case '[':
                {
                    var end = pos + 1;
                    while (end < paramDesc.Length && paramDesc[end] == '[') end++;
                    if (end < paramDesc.Length && paramDesc[end] == 'L') end = paramDesc.IndexOf(';', end);
                    pos = end;
                    state.Pop();
                    break;
                }
            }

            pos++;
        }
    }

    private static Effect PushReturn(FrameState state, string descriptor)
    {
        var retType = descriptor[(descriptor.IndexOf(')') + 1)..];
        switch (retType)
        {
            case "V": return Effect.Continue;
            case "J": return state.PushWide(JvmType.Long);
            case "D": return state.PushWide(JvmType.Double);
            case "F": return state.Push(JvmType.Float);
            case "I": case "Z": case "B": case "C": case "S": return state.Push(JvmType.Int);
            default: return state.Push(JvmType.Top);
        }
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

    private static FrameState InitLocals(string descriptor, bool isStatic)
    {
        var state = new FrameState();
        var offset = 0;

        if (!isStatic)
        {
            state.Locals.Add(new JvmUninitializedThis());
            offset = 1;
        }

        var paramDesc = descriptor[1..descriptor.IndexOf(')')];
        var pos = 0;
        while (pos < paramDesc.Length)
        {
            var ch = paramDesc[pos];
            switch (ch)
            {
                case 'B': case 'C': case 'S': case 'I': case 'Z':
                    state.Locals.Add(JvmType.Int);
                    break;
                case 'F':
                    state.Locals.Add(JvmType.Float);
                    break;
                case 'J':
                    state.Locals.Add(JvmType.Long);
                    state.Locals.Add(JvmType.Top);
                    break;
                case 'D':
                    state.Locals.Add(JvmType.Double);
                    state.Locals.Add(JvmType.Top);
                    break;
                case 'L':
                {
                    var end = paramDesc.IndexOf(';', pos);
                    var typeName = paramDesc[(pos + 1)..end];
                    state.Locals.Add(new JvmObject(typeName));
                    pos = end;
                    break;
                }
                case '[':
                {
                    var end = pos + 1;
                    while (end < paramDesc.Length && paramDesc[end] == '[') end++;
                    if (end < paramDesc.Length && paramDesc[end] == 'L') end = paramDesc.IndexOf(';', end);
                    var typeName = paramDesc[pos..(end + 1)];
                    state.Locals.Add(new JvmObject(typeName));
                    pos = end;
                    break;
                }
            }

            pos++;
        }

        return state;
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
        if (target.Locals.Count != source.Locals.Count) return false;
        if (target.Stack.Count != source.Stack.Count) return false;

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

// ── Internal type system ──

internal enum JvmKind { Int, Float, Long, Double, Top, Null, UninitializedThis, Object, Uninitialized }

internal class JvmType
{
    public JvmKind Kind { get; }
    public virtual string? TypeName => null;
    public virtual int NewOffset => -1;

    protected JvmType(JvmKind kind) => Kind = kind;

    public static readonly JvmType Int = new(JvmKind.Int);
    public static readonly JvmType Float = new(JvmKind.Float);
    public static readonly JvmType Long = new(JvmKind.Long);
    public static readonly JvmType Double = new(JvmKind.Double);
    public static readonly JvmType Top = new(JvmKind.Top);
    public static readonly JvmType Null = new(JvmKind.Null);
}

internal class JvmObject : JvmType
{
    public override string TypeName { get; }

    public JvmObject(string typeName) : base(JvmKind.Object)
    {
        TypeName = typeName;
    }

    public override bool Equals(object? obj) =>
        obj is JvmObject other && other.TypeName == TypeName;

    public override int GetHashCode() => TypeName.GetHashCode();
}

internal class JvmUninitialized : JvmType
{
    public override int NewOffset { get; }

    public JvmUninitialized(int newOffset) : base(JvmKind.Uninitialized)
    {
        NewOffset = newOffset;
    }

    public override bool Equals(object? obj) =>
        obj is JvmUninitialized other && other.NewOffset == NewOffset;

    public override int GetHashCode() => NewOffset.GetHashCode();
}

internal class JvmUninitializedThis : JvmType
{
    public JvmUninitializedThis() : base(JvmKind.UninitializedThis) { }

    public override bool Equals(object? obj) => obj is JvmUninitializedThis;
    public override int GetHashCode() => 0xCAFE;
}

// ── Frame state ──

internal class FrameState
{
    public List<JvmType> Locals { get; } = new();
    public List<JvmType> Stack { get; } = new();

    public FrameState Clone()
    {
        var clone = new FrameState();
        clone.Locals.AddRange(Locals);
        clone.Stack.AddRange(Stack);
        return clone;
    }

    public Effect Push(JvmType type) { Stack.Add(type); return Effect.Continue; }

    public Effect PushWide(JvmType type) { Stack.Add(type); Stack.Add(JvmType.Top); return Effect.Continue; }

    public Effect Pop()
    {
        if (Stack.Count > 0) Stack.RemoveAt(Stack.Count - 1);
        return Effect.Continue;
    }

    public Effect PopN(int n)
    {
        for (var i = 0; i < n && Stack.Count > 0; i++) Stack.RemoveAt(Stack.Count - 1);
        return Effect.Continue;
    }

    public Effect PopWide() { return PopN(2); }

    public Effect PopWide2() { return PopN(4); }
}

internal struct Effect
{
    public bool KeepGoing { get; }
    public static Effect Continue => new(true);
    public static Effect Stop => new(false);

    private Effect(bool cont) => KeepGoing = cont;

    public static implicit operator Effect(bool value) => new(value);

    public static Effect operator &(Effect a, Effect b) => a.KeepGoing && b.KeepGoing ? Continue : Stop;
    public static bool operator true(Effect e) => e.KeepGoing;
    public static bool operator false(Effect e) => !e.KeepGoing;
}
