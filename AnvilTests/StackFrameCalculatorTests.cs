using Anvil.Instructions;
using Anvil.Instructions.ConstantPool;
using Anvil.Instructions.StackMap;
using Anvil.Structures.Attributes;
using Anvil.Structures.Attributes.StackMap;
using Anvil.Structures.Attributes.StackMap.Frames;
using Anvil.Structures.Attributes.StackMap.Types;

namespace AnvilTests;

public class StackFrameCalculatorTests
{
    private static ConstantPoolBuilder MakeCp() => new();

    [Fact]
    public void Compute_EmptyVoidMethod_ProducesSingleFrame()
    {
        var body = new MethodBody
        {
            MethodDescriptor = "()V",
            IsStatic = true,
            Instructions = [new InsnInstruction(OperationCode.RETURN)]
        };

        var attr = new StackFrameCalculator(body, "()V", true, MakeCp()).Compute();
        Assert.NotEmpty(attr.Entries);
    }

    [Fact]
    public void Compute_StoreThenLoad_TracksLocalType()
    {
        var label = new Label();
        var storeInsn = new VarInstruction(OperationCode.ISTORE, 0);
        storeInsn.Labels.Add(label);

        var body = new MethodBody
        {
            MethodDescriptor = "()V",
            IsStatic = true,
            Instructions =
            {
                new IntInstruction(OperationCode.BIPUSH, 42),
                storeInsn,
                new VarInstruction(OperationCode.ILOAD, 0),
                new InsnInstruction(OperationCode.RETURN)
            }
        };

        var cp = MakeCp();
        var attr = new StackFrameCalculator(body, "()V", true, cp).Compute();
        var fullFrames = attr.Entries.OfType<FullFrame>().ToList();

        var frameAtStore = fullFrames.FirstOrDefault(f => f.OffsetDelta.Value == 2);
        Assert.NotNull(frameAtStore);
        var local0 = frameAtStore!.Locals.FirstOrDefault() as IntegerVariableInfo;
        Assert.NotNull(local0);
    }

    [Fact]
    public void Compute_AStore_PreservesReferenceTypeToLocal()
    {
        var label = new Label();
        var storeInsn = new VarInstruction(OperationCode.ASTORE, 0);
        storeInsn.Labels.Add(label);

        var body = new MethodBody
        {
            MethodDescriptor = "()V",
            IsStatic = true,
            Instructions =
            {
                new InsnInstruction(OperationCode.ACONST_NULL),
                storeInsn,
                new VarInstruction(OperationCode.ALOAD, 0),
                new InsnInstruction(OperationCode.RETURN)
            }
        };

        var cp = MakeCp();
        var attr = new StackFrameCalculator(body, "()V", true, cp).Compute();
        var fullFrames = attr.Entries.OfType<FullFrame>().ToList();

        var frameAtStore = fullFrames.FirstOrDefault(f => f.OffsetDelta.Value == 1);
        Assert.NotNull(frameAtStore);
        var local0 = frameAtStore!.Locals.FirstOrDefault() as NullVariableInfo;
        Assert.NotNull(local0);
    }

    [Fact]
    public void Compute_LStore_UpdatesTwoLocalSlots()
    {
        var label = new Label();
        var storeInsn = new VarInstruction(OperationCode.LSTORE, 0);
        storeInsn.Labels.Add(label);

        var body = new MethodBody
        {
            MethodDescriptor = "()V",
            IsStatic = true,
            Instructions =
            {
                new InsnInstruction(OperationCode.LCONST_0),
                storeInsn,
                new InsnInstruction(OperationCode.RETURN)
            }
        };

        var cp = MakeCp();
        var attr = new StackFrameCalculator(body, "()V", true, cp).Compute();
        var fullFrames = attr.Entries.OfType<FullFrame>().ToList();

        var frameAfterStore = fullFrames.FirstOrDefault(f => f.OffsetDelta.Value == 1);
        Assert.NotNull(frameAfterStore);
        Assert.True(frameAfterStore!.Locals.Length >= 2);
        Assert.IsType<LongVariableInfo>(frameAfterStore.Locals[0]);
        Assert.IsType<TopVariableInfo>(frameAfterStore.Locals[1]);
    }

    [Fact]
    public void Compute_Dup_DuplicatesTopOfStack()
    {
        var label = new Label();
        var dupInsn = new InsnInstruction(OperationCode.DUP);
        dupInsn.Labels.Add(label);

        var body = new MethodBody
        {
            MethodDescriptor = "()V",
            IsStatic = true,
            Instructions =
            {
                new IntInstruction(OperationCode.BIPUSH, 1),
                dupInsn,
                new InsnInstruction(OperationCode.RETURN)
            }
        };

        var cp = MakeCp();
        var attr = new StackFrameCalculator(body, "()V", true, cp).Compute();
        var fullFrames = attr.Entries.OfType<FullFrame>().ToList();

        var frameAfterDup = fullFrames.FirstOrDefault();
        Assert.NotNull(frameAfterDup);
        Assert.Equal(2, frameAfterDup!.Stack.Length);
        Assert.IsType<IntegerVariableInfo>(frameAfterDup.Stack[0]);
        Assert.IsType<IntegerVariableInfo>(frameAfterDup.Stack[1]);
    }

    [Fact]
    public void Compute_Swap_SwapsTopTwo()
    {
        var label = new Label();
        var swapInsn = new InsnInstruction(OperationCode.SWAP);
        swapInsn.Labels.Add(label);

        var body = new MethodBody
        {
            MethodDescriptor = "()V",
            IsStatic = true,
            Instructions =
            {
                new InsnInstruction(OperationCode.ACONST_NULL),
                new IntInstruction(OperationCode.BIPUSH, 1),
                swapInsn,
                new InsnInstruction(OperationCode.RETURN)
            }
        };

        var cp = MakeCp();
        var attr = new StackFrameCalculator(body, "()V", true, cp).Compute();
        var fullFrames = attr.Entries.OfType<FullFrame>().ToList();

        var frameAfterSwap = fullFrames.FirstOrDefault();
        Assert.NotNull(frameAfterSwap);
        Assert.Equal(2, frameAfterSwap!.Stack.Length);
        Assert.IsType<IntegerVariableInfo>(frameAfterSwap.Stack[0]);
        Assert.IsType<NullVariableInfo>(frameAfterSwap.Stack[1]);
    }

    [Fact]
    public void Compute_ExceptionHandler_HasSameLocalsAsTryEntry()
    {
        var tryStart = new Label();
        var tryEnd = new Label();
        var handler = new Label();

        var pushInsn = new IntInstruction(OperationCode.BIPUSH, 0);
        var storeInsn = new VarInstruction(OperationCode.ISTORE, 0);
        var tryStartRet = new InsnInstruction(OperationCode.RETURN);
        tryStartRet.Labels.Add(tryStart);
        var tryEndNop = new InsnInstruction(OperationCode.NOP);
        tryEndNop.Labels.Add(tryEnd);
        var handlerNop = new InsnInstruction(OperationCode.NOP);
        handlerNop.Labels.Add(handler);

        var body = new MethodBody
        {
            MethodDescriptor = "()V",
            IsStatic = true,
            Instructions =
            {
                pushInsn,
                storeInsn,
                tryStartRet,
                tryEndNop,
                handlerNop,
                new InsnInstruction(OperationCode.RETURN)
            },
            TryCatchBlocks =
            {
                new TryCatchBlock(tryStart, tryEnd, handler, "java/lang/Exception")
            }
        };

        var cp = MakeCp();
        var attr = new StackFrameCalculator(body, "()V", true, cp).Compute();
        var fullFrames = attr.Entries.OfType<FullFrame>().ToList();

        Assert.NotEmpty(fullFrames);
    }

    [Fact]
    public void Compute_AnNewArray_UsesCorrectArrayType()
    {
        var label = new Label();
        var arrayInsn = new TypeInstruction(OperationCode.ANEWARRAY, "java/lang/String");
        arrayInsn.Labels.Add(label);

        var body = new MethodBody
        {
            MethodDescriptor = "()V",
            IsStatic = true,
            Instructions =
            {
                new IntInstruction(OperationCode.BIPUSH, 5),
                arrayInsn,
                new InsnInstruction(OperationCode.RETURN)
            }
        };

        var cp = MakeCp();
        var attr = new StackFrameCalculator(body, "()V", true, cp).Compute();
        var fullFrames = attr.Entries.OfType<FullFrame>().ToList();

        var frame = fullFrames.FirstOrDefault();
        Assert.NotNull(frame);
        Assert.Single(frame!.Stack);
        Assert.IsType<ObjectVariableInfo>(frame.Stack[0]);
    }

    [Fact]
    public void Compute_InstanceMethod_HasUninitializedThis()
    {
        var body = new MethodBody
        {
            MethodDescriptor = "()V",
            IsStatic = false,
            Instructions = [new InsnInstruction(OperationCode.RETURN)]
        };

        var attr = new StackFrameCalculator(body, "()V", false, MakeCp()).Compute();
        var fullFrames = attr.Entries.OfType<FullFrame>().ToList();

        Assert.NotEmpty(fullFrames);
        Assert.NotEmpty(fullFrames[0].Locals);
        Assert.IsType<UninitializedThisVariableInfo>(fullFrames[0].Locals[0]);
    }

    [Fact]
    public void Compute_ParameterTypes_ArePreserved()
    {
        var body = new MethodBody
        {
            MethodDescriptor = "(IJLjava/lang/String;)V",
            IsStatic = true,
            Instructions = [new InsnInstruction(OperationCode.RETURN)]
        };

        var attr = new StackFrameCalculator(body, "(IJLjava/lang/String;)V", true, MakeCp()).Compute();
        var fullFrames = attr.Entries.OfType<FullFrame>().ToList();

        Assert.NotEmpty(fullFrames);
        var locals = fullFrames[0].Locals;
        Assert.Equal(4, locals.Length);
        Assert.IsType<IntegerVariableInfo>(locals[0]);
        Assert.IsType<LongVariableInfo>(locals[1]);
        Assert.IsType<TopVariableInfo>(locals[2]);
        Assert.IsType<ObjectVariableInfo>(locals[3]);
    }

    [Fact]
    public void Compute_NewInstruction_PushesUninitializedType()
    {
        var label = new Label();
        var newInsn = new TypeInstruction(OperationCode.NEW, "java/lang/Object");
        newInsn.Labels.Add(label);

        var body = new MethodBody
        {
            MethodDescriptor = "()V",
            IsStatic = true,
            Instructions =
            {
                newInsn,
                new InsnInstruction(OperationCode.RETURN)
            }
        };

        var cp = MakeCp();
        var attr = new StackFrameCalculator(body, "()V", true, cp).Compute();
        var fullFrames = attr.Entries.OfType<FullFrame>().ToList();

        var frame = fullFrames.FirstOrDefault();
        Assert.NotNull(frame);
        Assert.Single(frame!.Stack);
        Assert.IsType<UninitializedVariableInfo>(frame.Stack[0]);
    }

    [Fact]
    public void Compute_AutoResolvesOffsets_WhenManuallyConstructed()
    {
        var body = new MethodBody
        {
            MethodDescriptor = "()V",
            IsStatic = true,
            Instructions =
            {
                new IntInstruction(OperationCode.BIPUSH, 1),
                new IntInstruction(OperationCode.BIPUSH, 2),
                new InsnInstruction(OperationCode.IADD),
                new InsnInstruction(OperationCode.RETURN)
            }
        };

        var attr = new StackFrameCalculator(body, "()V", true, MakeCp()).Compute();
        Assert.NotEmpty(attr.Entries);
    }
}
