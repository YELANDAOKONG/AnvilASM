using Anvil.Instructions;
using Anvil.Instructions.ConstantPool;

namespace AnvilTests;

public class InstructionTests
{
    [Fact]
    public void JumpInstruction_GetSize_NormalIs3()
    {
        var jump = new JumpInstruction(OperationCode.GOTO, new Label());
        Assert.Equal(3, jump.GetSize());
    }

    [Fact]
    public void JumpInstruction_UpgradeToWide_GetSizeBecomes5()
    {
        var jump = new JumpInstruction(OperationCode.GOTO, new Label());
        jump.UpgradeToWide();
        Assert.Equal(5, jump.GetSize());
    }

    [Fact]
    public void JumpInstruction_NeedsWidening_WithinShortRange_ReturnsFalse()
    {
        var jump = new JumpInstruction(OperationCode.IFEQ, new Label())
        {
            Offset = 0,
            BranchOffset = 100
        };
        Assert.False(jump.NeedsWidening);
    }

    [Fact]
    public void JumpInstruction_NeedsWidening_OutsideShortRange_ReturnsTrue()
    {
        var jump = new JumpInstruction(OperationCode.IFEQ, new Label())
        {
            Offset = 0,
            BranchOffset = short.MaxValue + 1
        };
        Assert.True(jump.NeedsWidening);
    }

    [Fact]
    public void JumpInstruction_GotoW_NotNeedsWidening()
    {
        var jump = new JumpInstruction(OperationCode.GOTO_W, new Label())
        {
            Offset = 0,
            BranchOffset = int.MaxValue
        };
        Assert.False(jump.NeedsWidening);
    }

    [Fact]
    public void InvokeDynamicInstruction_ResolvesViaToCodeAttribute()
    {
        var cp = new ConstantPoolBuilder();
        var body = new MethodBody
        {
            MethodDescriptor = "()V",
            IsStatic = true,
            MaxStack = 1,
            MaxLocals = 0,
            Instructions =
            {
                new InvokeDynamicInstruction(0, "run", "()Ljava/lang/Runnable;"),
                new InsnInstruction(OperationCode.RETURN)
            }
        };

        var codeAttr = body.ToCodeAttribute(cp);
        Assert.NotNull(codeAttr);
        Assert.NotEmpty(codeAttr.Code);
    }

    [Fact]
    public void InvokeDynamicInstruction_GetSize_Always5()
    {
        var insn = new InvokeDynamicInstruction(1);
        Assert.Equal(5, insn.GetSize());
    }

    [Fact]
    public void TableSwitchInstruction_GetSize_IncludesAlignmentAndTargets()
    {
        var def = new Label();
        var targets = new List<Label> { new(), new(), new() };
        var insn = new TableSwitchInstruction(0, 2, def, targets) { Offset = 0 };

        var size = insn.GetSize();
        Assert.Equal(28, size);
    }

    [Fact]
    public void TableSwitchInstruction_GetSize_NullOffset_HandledGracefully()
    {
        var def = new Label();
        var targets = new List<Label> { new() };
        var insn = new TableSwitchInstruction(0, 0, def, targets);

        var size = insn.GetSize();
        Assert.True(size > 0);
    }

    [Fact]
    public void LookupSwitchInstruction_GetSize_NullOffset_HandledGracefully()
    {
        var def = new Label();
        var pairs = new List<(int, Label)> { (1, new()) };
        var insn = new LookupSwitchInstruction(def, pairs);

        var size = insn.GetSize();
        Assert.True(size > 0);
    }

    [Fact]
    public void TypeInstruction_ResolvesViaToCodeAttribute()
    {
        var cp = new ConstantPoolBuilder();
        var body = new MethodBody
        {
            MethodDescriptor = "()V",
            IsStatic = true,
            MaxStack = 1,
            MaxLocals = 0,
            Instructions =
            {
                new TypeInstruction(OperationCode.NEW, "java/lang/Object"),
                new InsnInstruction(OperationCode.RETURN)
            }
        };

        var codeAttr = body.ToCodeAttribute(cp);
        Assert.NotEmpty(codeAttr.Code);
    }

    [Fact]
    public void FieldInstruction_ResolvesViaToCodeAttribute()
    {
        var cp = new ConstantPoolBuilder();
        var body = new MethodBody
        {
            MethodDescriptor = "()V",
            IsStatic = true,
            MaxStack = 1,
            MaxLocals = 0,
            Instructions =
            {
                new FieldInstruction(OperationCode.GETSTATIC, "java/lang/System", "out", "Ljava/io/PrintStream;"),
                new InsnInstruction(OperationCode.RETURN)
            }
        };

        var codeAttr = body.ToCodeAttribute(cp);
        Assert.NotEmpty(codeAttr.Code);
    }

    [Fact]
    public void MethodInstruction_ResolvesViaToCodeAttribute()
    {
        var cp = new ConstantPoolBuilder();
        var body = new MethodBody
        {
            MethodDescriptor = "()V",
            IsStatic = true,
            MaxStack = 1,
            MaxLocals = 0,
            Instructions =
            {
                new IntInstruction(OperationCode.BIPUSH, -1),
                new MethodInstruction(OperationCode.INVOKESTATIC, "java/lang/Math", "abs", "(I)I"),
                new InsnInstruction(OperationCode.POP),
                new InsnInstruction(OperationCode.RETURN)
            }
        };

        var codeAttr = body.ToCodeAttribute(cp);
        Assert.NotEmpty(codeAttr.Code);
    }

    [Fact]
    public void LdcInstruction_ResolvesViaToCodeAttribute()
    {
        var cp = new ConstantPoolBuilder();
        var body = new MethodBody
        {
            MethodDescriptor = "()V",
            IsStatic = true,
            MaxStack = 1,
            MaxLocals = 0,
            Instructions =
            {
                new LdcInstruction("hello"),
                new InsnInstruction(OperationCode.RETURN)
            }
        };

        var codeAttr = body.ToCodeAttribute(cp);
        Assert.NotEmpty(codeAttr.Code);
    }

    [Fact]
    public void MultiANewArrayInstruction_ResolvesViaToCodeAttribute()
    {
        var cp = new ConstantPoolBuilder();
        var body = new MethodBody
        {
            MethodDescriptor = "()V",
            IsStatic = true,
            MaxStack = 2,
            MaxLocals = 0,
            Instructions =
            {
                new IntInstruction(OperationCode.BIPUSH, 3),
                new IntInstruction(OperationCode.BIPUSH, 5),
                new MultiANewArrayInstruction("[[I", 2),
                new InsnInstruction(OperationCode.RETURN)
            }
        };

        var codeAttr = body.ToCodeAttribute(cp);
        Assert.NotEmpty(codeAttr.Code);
    }
}
