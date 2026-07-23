using Anvil.Instructions;
using Anvil.Instructions.ConstantPool;
using Anvil.Instructions.StackMap;
using Anvil.Structures.Attributes;
using Anvil.Structures.Attributes.StackMap;
using Anvil.Structures.Attributes.StackMap.Frames;
using Anvil.Structures.Attributes.StackMap.Types;
using Anvil.Structures.ConstantPool;

namespace AnvilTests;

public class StackFrameCalculatorTests
{
    [Fact]
    public void Compute_SingleReturn_UsesImplicitInitialFrame()
    {
        var body = new MethodBody
        {
            MethodDescriptor = "()V",
            IsStatic = true,
            Instructions = [new InsnInstruction(OperationCode.RETURN)]
        };

        var attribute = Compute(body);

        Assert.Empty(attribute.Entries);
    }

    [Fact]
    public void Compute_StoreThenLoad_TracksPreInstructionLocalType()
    {
        var body = new MethodBody
        {
            MethodDescriptor = "()V",
            IsStatic = true,
            Instructions =
            {
                new IntInstruction(OperationCode.BIPUSH, 42),
                new VarInstruction(OperationCode.ISTORE, 0),
                new VarInstruction(OperationCode.ILOAD, 0),
                new InsnInstruction(OperationCode.RETURN)
            }
        };

        var frame = GetFrameAtOffset(Compute(body), 3);

        Assert.IsType<IntegerVariableInfo>(Assert.Single(frame.Locals));
        Assert.Empty(frame.Stack);
    }

    [Fact]
    public void Compute_AStore_PreservesReferenceTypeToLocal()
    {
        var body = new MethodBody
        {
            MethodDescriptor = "()V",
            IsStatic = true,
            Instructions =
            {
                new InsnInstruction(OperationCode.ACONST_NULL),
                new VarInstruction(OperationCode.ASTORE, 0),
                new VarInstruction(OperationCode.ALOAD, 0),
                new InsnInstruction(OperationCode.ARETURN)
            }
        };

        var frame = GetFrameAtOffset(Compute(body), 2);

        Assert.IsType<NullVariableInfo>(Assert.Single(frame.Locals));
    }

    [Fact]
    public void Compute_LStore_EncodesLongAsOneVerificationEntry()
    {
        var body = new MethodBody
        {
            MethodDescriptor = "()V",
            IsStatic = true,
            Instructions =
            {
                new InsnInstruction(OperationCode.LCONST_0),
                new VarInstruction(OperationCode.LSTORE, 0),
                new InsnInstruction(OperationCode.RETURN)
            }
        };

        var frame = GetFrameAtOffset(Compute(body), 2);

        Assert.IsType<LongVariableInfo>(Assert.Single(frame.Locals));
    }

    [Fact]
    public void Compute_Dup_DuplicatesTopOfStack()
    {
        var body = new MethodBody
        {
            MethodDescriptor = "()I",
            IsStatic = true,
            Instructions =
            {
                new IntInstruction(OperationCode.BIPUSH, 1),
                new InsnInstruction(OperationCode.DUP),
                new InsnInstruction(OperationCode.IRETURN)
            }
        };

        var frame = GetFrameAtOffset(Compute(body), 2);

        Assert.Equal(2, frame.Stack.Length);
        Assert.All(frame.Stack, item => Assert.IsType<IntegerVariableInfo>(item));
    }

    [Fact]
    public void Compute_Swap_SwapsTopTwoValues()
    {
        var body = new MethodBody
        {
            MethodDescriptor = "()V",
            IsStatic = true,
            Instructions =
            {
                new InsnInstruction(OperationCode.ACONST_NULL),
                new IntInstruction(OperationCode.BIPUSH, 1),
                new InsnInstruction(OperationCode.SWAP),
                new InsnInstruction(OperationCode.POP),
                new InsnInstruction(OperationCode.POP),
                new InsnInstruction(OperationCode.RETURN)
            }
        };

        var frame = GetFrameAtOffset(Compute(body), 3);

        Assert.IsType<IntegerVariableInfo>(frame.Stack[0]);
        Assert.IsType<NullVariableInfo>(frame.Stack[1]);
    }

    [Fact]
    public void Compute_ExceptionHandler_UsesProtectedLocalsAndExceptionStack()
    {
        var tryStart = new Label("tryStart");
        var tryEnd = new Label("tryEnd");
        var handler = new Label("handler");
        var protectedReturn = new InsnInstruction(OperationCode.RETURN);
        protectedReturn.Labels.Add(tryStart);
        var handlerPop = new InsnInstruction(OperationCode.POP);
        handlerPop.Labels.Add(handler);

        var body = new MethodBody
        {
            MethodDescriptor = "()V",
            IsStatic = true,
            Instructions =
            {
                new IntInstruction(OperationCode.BIPUSH, 0),
                new VarInstruction(OperationCode.ISTORE, 0),
                protectedReturn,
                handlerPop,
                new InsnInstruction(OperationCode.RETURN)
            },
            TryCatchBlocks =
            {
                new TryCatchBlock(tryStart, tryEnd, handler, "java/lang/Exception")
            }
        };
        body.MarkLabel("tryEnd", handlerPop);

        var frame = GetFrameAtOffset(Compute(body), 3);

        Assert.IsType<IntegerVariableInfo>(Assert.Single(frame.Locals));
        Assert.IsType<ObjectVariableInfo>(Assert.Single(frame.Stack));
    }

    [Fact]
    public void Compute_ANewArray_PushesArrayReference()
    {
        var body = new MethodBody
        {
            MethodDescriptor = "()[Ljava/lang/String;",
            IsStatic = true,
            Instructions =
            {
                new IntInstruction(OperationCode.BIPUSH, 5),
                new TypeInstruction(OperationCode.ANEWARRAY, "java/lang/String"),
                new InsnInstruction(OperationCode.ARETURN)
            }
        };

        var frame = GetFrameAtOffset(Compute(body), 4);

        Assert.IsType<ObjectVariableInfo>(Assert.Single(frame.Stack));
    }

    [Fact]
    public void Compute_RegularInstanceMethod_HasInitializedThis()
    {
        var body = new MethodBody
        {
            MethodName = "run",
            OwnerInternalName = "example/Owner",
            MethodDescriptor = "()V",
            IsStatic = false,
            Instructions =
            {
                new InsnInstruction(OperationCode.NOP),
                new InsnInstruction(OperationCode.RETURN)
            }
        };

        var frame = GetFrameAtOffset(Compute(body), 1);

        Assert.IsType<ObjectVariableInfo>(Assert.Single(frame.Locals));
    }

    [Fact]
    public void Compute_Constructor_HasUninitializedThis()
    {
        var body = new MethodBody
        {
            MethodName = "<init>",
            OwnerInternalName = "example/Owner",
            MethodDescriptor = "()V",
            IsStatic = false,
            Instructions =
            {
                new InsnInstruction(OperationCode.NOP),
                new InsnInstruction(OperationCode.RETURN)
            }
        };

        var frame = GetFrameAtOffset(Compute(body), 1);

        Assert.IsType<UninitializedThisVariableInfo>(Assert.Single(frame.Locals));
    }

    [Fact]
    public void Compute_ParameterTypes_UseVerificationEntriesNotLocalSlots()
    {
        var body = new MethodBody
        {
            MethodDescriptor = "(IJLjava/lang/String;)V",
            IsStatic = true,
            Instructions =
            {
                new InsnInstruction(OperationCode.NOP),
                new InsnInstruction(OperationCode.RETURN)
            }
        };

        var frame = GetFrameAtOffset(Compute(body), 1);

        Assert.Equal(3, frame.Locals.Length);
        Assert.IsType<IntegerVariableInfo>(frame.Locals[0]);
        Assert.IsType<LongVariableInfo>(frame.Locals[1]);
        Assert.IsType<ObjectVariableInfo>(frame.Locals[2]);
    }

    [Fact]
    public void Compute_NewInstruction_PushesUninitializedType()
    {
        var body = new MethodBody
        {
            MethodDescriptor = "()Ljava/lang/Object;",
            IsStatic = true,
            Instructions =
            {
                new TypeInstruction(OperationCode.NEW, "java/lang/Object"),
                new InsnInstruction(OperationCode.ARETURN)
            }
        };

        var frame = GetFrameAtOffset(Compute(body), 3);

        Assert.IsType<UninitializedVariableInfo>(Assert.Single(frame.Stack));
    }

    [Fact]
    public void Compute_Goto_PropagatesOnlyToTarget()
    {
        var target = new Label("target");
        var targetReturn = new InsnInstruction(OperationCode.IRETURN);
        targetReturn.Labels.Add(target);
        var body = new MethodBody
        {
            MethodDescriptor = "()I",
            IsStatic = true,
            Instructions =
            {
                new IntInstruction(OperationCode.BIPUSH, 1),
                new JumpInstruction(OperationCode.GOTO, new Label("target")),
                new InsnInstruction(OperationCode.ACONST_NULL),
                targetReturn
            }
        };

        var attribute = Compute(body);
        var targetFrame = GetFrameAtOffset(attribute, 5);

        Assert.IsType<IntegerVariableInfo>(Assert.Single(targetFrame.Stack));
        Assert.DoesNotContain(
            GetFramesWithOffsets(attribute),
            pair => pair.Offset == 4);
    }

    [Fact]
    public void Compute_ObjectMerge_UsesConfiguredCommonSuperTypeResolver()
    {
        var left = new InsnInstruction(OperationCode.ACONST_NULL);
        var merge = new InsnInstruction(OperationCode.ARETURN);
        var body = new MethodBody
        {
            MethodDescriptor = "()Ljava/util/List;",
            IsStatic = true,
            CommonSuperTypeResolver = (_, _) => "java/util/List",
            Instructions =
            {
                new InsnInstruction(OperationCode.ICONST_0),
                new JumpInstruction(OperationCode.IFEQ, "left"),
                new InsnInstruction(OperationCode.ACONST_NULL),
                new TypeInstruction(OperationCode.CHECKCAST, "java/util/ArrayList"),
                new JumpInstruction(OperationCode.GOTO, "merge"),
                left,
                new TypeInstruction(OperationCode.CHECKCAST, "java/util/LinkedList"),
                merge
            }
        };
        body.MarkLabel("left", left);
        body.MarkLabel("merge", merge);
        var constantPool = new ConstantPoolBuilder();

        var attribute = new StackFrameCalculator(
            body,
            body.MethodDescriptor,
            body.IsStatic,
            constantPool).Compute();
        var frame = GetFrameAtOffset(attribute, 15);
        var objectInfo = Assert.IsType<ObjectVariableInfo>(Assert.Single(frame.Stack));
        var entries = constantPool.Build();
        var classInfo = Assert.IsType<CpClass>(entries[objectInfo.CPoolIndex.Value]);
        var name = Assert.IsType<CpUtf8>(entries[classInfo.NameIndex.Value]).Value;

        Assert.Equal("java/util/List", name);
    }

    private static StackMapTableAttribute Compute(MethodBody body)
    {
        return new StackFrameCalculator(
            body,
            body.MethodDescriptor!,
            body.IsStatic,
            new ConstantPoolBuilder()).Compute();
    }

    private static FullFrame GetFrameAtOffset(
        StackMapTableAttribute attribute,
        int expectedOffset)
    {
        return GetFramesWithOffsets(attribute)
            .Single(pair => pair.Offset == expectedOffset)
            .Frame;
    }

    private static IReadOnlyList<(int Offset, FullFrame Frame)> GetFramesWithOffsets(
        StackMapTableAttribute attribute)
    {
        var result = new List<(int Offset, FullFrame Frame)>();
        var previousOffset = -1;
        foreach (var frame in attribute.Entries.Cast<FullFrame>())
        {
            var offset = previousOffset + frame.OffsetDelta.Value + 1;
            result.Add((offset, frame));
            previousOffset = offset;
        }

        return result;
    }
}
