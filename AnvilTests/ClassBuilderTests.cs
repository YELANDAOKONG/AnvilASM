using Anvil.Core;
using Anvil.Instructions;
using Anvil.Instructions.ConstantPool;
using Anvil.Structures.Attributes;

namespace AnvilTests;

public class ClassBuilderTests
{
    [Fact]
    public void ToCodeAttribute_WithMethodDescriptor_GeneratesStackMapTable()
    {
        var body = new MethodBody
        {
            MethodDescriptor = "()V",
            IsStatic = true,
            MaxStack = 1,
            MaxLocals = 0,
            Instructions = [new InsnInstruction(OperationCode.RETURN)]
        };

        var cp = new ConstantPoolBuilder();
        var codeAttr = body.ToCodeAttribute(cp);

        Assert.NotEmpty(codeAttr.Code);
        Assert.NotEmpty(codeAttr.Attributes);
    }

    [Fact]
    public void ToCodeAttribute_WithExceptionTable_ResolvesCatchType()
    {
        var start = new Label();
        var end = new Label();
        var handler = new Label();

        var returnInsn = new InsnInstruction(OperationCode.RETURN);
        returnInsn.Labels.Add(start);

        var endNop = new InsnInstruction(OperationCode.NOP);
        endNop.Labels.Add(end);

        var handlerNop = new InsnInstruction(OperationCode.NOP);
        handlerNop.Labels.Add(handler);

        var body = new MethodBody
        {
            MethodDescriptor = "()V",
            IsStatic = true,
            MaxStack = 1,
            MaxLocals = 0,
            Instructions =
            {
                returnInsn,
                endNop,
                handlerNop,
                new InsnInstruction(OperationCode.RETURN)
            },
            TryCatchBlocks =
            {
                new TryCatchBlock(start, end, handler, "java/io/IOException")
            }
        };

        var cp = new ConstantPoolBuilder();
        var codeAttr = body.ToCodeAttribute(cp);

        Assert.Single(codeAttr.ExceptionTable);
        Assert.NotEqual(0, codeAttr.ExceptionTable[0].CatchType.Value);
    }

    [Fact]
    public void ToCodeAttribute_CatchAllBlock_HasCatchTypeZero()
    {
        var start = new Label();
        var end = new Label();
        var handler = new Label();

        var returnInsn = new InsnInstruction(OperationCode.RETURN);
        returnInsn.Labels.Add(start);

        var endNop = new InsnInstruction(OperationCode.NOP);
        endNop.Labels.Add(end);

        var handlerNop = new InsnInstruction(OperationCode.NOP);
        handlerNop.Labels.Add(handler);

        var body = new MethodBody
        {
            MethodDescriptor = "()V",
            IsStatic = true,
            MaxStack = 1,
            MaxLocals = 0,
            Instructions =
            {
                returnInsn,
                endNop,
                handlerNop,
                new InsnInstruction(OperationCode.RETURN)
            },
            TryCatchBlocks =
            {
                new TryCatchBlock(start, end, handler, null)
            }
        };

        var cp = new ConstantPoolBuilder();
        var codeAttr = body.ToCodeAttribute(cp);

        Assert.Single(codeAttr.ExceptionTable);
        Assert.Equal(0, codeAttr.ExceptionTable[0].CatchType.Value);
    }

    [Fact]
    public void ToCodeAttribute_WithoutMethodDescriptor_NoStackMapTable()
    {
        var body = new MethodBody
        {
            MethodDescriptor = null,
            MaxStack = 1,
            MaxLocals = 0,
            Instructions = [new InsnInstruction(OperationCode.RETURN)]
        };

        var cp = new ConstantPoolBuilder();
        var codeAttr = body.ToCodeAttribute(cp);

        Assert.NotEmpty(codeAttr.Code);
        Assert.Empty(codeAttr.Attributes);
    }

    [Fact]
    public void ToCodeAttribute_WithJumpAndLabel_ResolvesBranchOffsets()
    {
        var target = new Label();
        var retInsn = new InsnInstruction(OperationCode.RETURN);
        retInsn.Labels.Add(target);

        var body = new MethodBody
        {
            MethodDescriptor = "()V",
            IsStatic = true,
            MaxStack = 2,
            MaxLocals = 1,
            Instructions =
            {
                new IntInstruction(OperationCode.BIPUSH, 1),
                new VarInstruction(OperationCode.ISTORE, 0),
                new VarInstruction(OperationCode.ILOAD, 0),
                new JumpInstruction(OperationCode.IFEQ, target),
                retInsn
            }
        };

        var cp = new ConstantPoolBuilder();
        var codeAttr = body.ToCodeAttribute(cp);

        Assert.NotEmpty(codeAttr.Code);
        Assert.NotEmpty(codeAttr.Attributes);
    }
}
