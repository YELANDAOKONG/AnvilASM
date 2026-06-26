using Anvil.Instructions;
using Anvil.Instructions.ConstantPool;

namespace AnvilTests;

public class MethodBodyTests
{
    [Fact]
    public void Normalize_ILoad0_ConvertsToVarInstruction()
    {
        var body = new MethodBody();
        body.Instructions.Add(new InsnInstruction(OperationCode.ILOAD_0));
        body.Normalize();

        var insn = body.Instructions[0] as VarInstruction;
        Assert.NotNull(insn);
        Assert.Equal(OperationCode.ILOAD, insn!.OpCode);
        Assert.Equal(0, insn.VarIndex);
    }

    [Fact]
    public void Normalize_ALoad3_ConvertsToVarInstruction()
    {
        var body = new MethodBody();
        body.Instructions.Add(new InsnInstruction(OperationCode.ALOAD_3));
        body.Normalize();

        var insn = body.Instructions[0] as VarInstruction;
        Assert.NotNull(insn);
        Assert.Equal(OperationCode.ALOAD, insn!.OpCode);
        Assert.Equal(3, insn.VarIndex);
    }

    [Fact]
    public void Normalize_IStore1_ConvertsToVarInstruction()
    {
        var body = new MethodBody();
        body.Instructions.Add(new InsnInstruction(OperationCode.ISTORE_1));
        body.Normalize();

        var insn = body.Instructions[0] as VarInstruction;
        Assert.NotNull(insn);
        Assert.Equal(OperationCode.ISTORE, insn!.OpCode);
        Assert.Equal(1, insn.VarIndex);
    }

    [Fact]
    public void Normalize_IConst5_ConvertsToIntInstruction()
    {
        var body = new MethodBody();
        body.Instructions.Add(new InsnInstruction(OperationCode.ICONST_5));
        body.Normalize();

        var insn = body.Instructions[0] as IntInstruction;
        Assert.NotNull(insn);
        Assert.Equal(OperationCode.BIPUSH, insn!.OpCode);
    }

    [Fact]
    public void Normalize_IConstM1_ConvertsToIntInstruction()
    {
        var body = new MethodBody();
        body.Instructions.Add(new InsnInstruction(OperationCode.ICONST_M1));
        body.Normalize();

        var insn = body.Instructions[0] as IntInstruction;
        Assert.NotNull(insn);
        Assert.Equal(OperationCode.BIPUSH, insn!.OpCode);
    }

    [Fact]
    public void ResolveLabels_SetsOffsetsOnAllInstructions()
    {
        var body = new MethodBody
        {
            Instructions =
            {
                new InsnInstruction(OperationCode.NOP),
                new InsnInstruction(OperationCode.NOP),
                new InsnInstruction(OperationCode.RETURN)
            }
        };
        body.ResolveLabels();

        Assert.Equal(3, body.Instructions.Count);
        Assert.NotNull(body.Instructions[0].Offset);
        Assert.NotNull(body.Instructions[1].Offset);
        Assert.NotNull(body.Instructions[2].Offset);
        Assert.Equal(0, body.Instructions[0].Offset!.Value);
        Assert.Equal(1, body.Instructions[1].Offset!.Value);
        Assert.Equal(2, body.Instructions[2].Offset!.Value);
    }

    [Fact]
    public void ResolveCpReferences_RunsViaToCodeAttribute()
    {
        var body = new MethodBody
        {
            MethodDescriptor = "()V",
            IsStatic = true,
            MaxStack = 1,
            MaxLocals = 0,
            Instructions =
            {
                new TypeInstruction(OperationCode.NEW, "java/lang/Object"),
                new FieldInstruction(OperationCode.GETSTATIC, "java/lang/System", "out", "Ljava/io/PrintStream;"),
                new MethodInstruction(OperationCode.INVOKESTATIC, "java/lang/Math", "abs", "(I)I"),
                new LdcInstruction("hello"),
                new MultiANewArrayInstruction("[[I", 2),
                new InsnInstruction(OperationCode.RETURN)
            }
        };

        var cp = new ConstantPoolBuilder();
        var codeAttr = body.ToCodeAttribute(cp);

        Assert.NotEmpty(codeAttr.Code);
        Assert.NotEmpty(codeAttr.Attributes);
    }

    [Fact]
    public void ResolveCpReferences_HandlesInvokeDynamic()
    {
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

        var cp = new ConstantPoolBuilder();
        var codeAttr = body.ToCodeAttribute(cp);

        Assert.NotEmpty(codeAttr.Code);
    }

    [Fact]
    public void WriteBytecode_ProducesValidBytes()
    {
        var body = new MethodBody
        {
            MethodDescriptor = "()V",
            IsStatic = true,
            MaxStack = 1,
            MaxLocals = 0,
            Instructions =
            {
                new InsnInstruction(OperationCode.RETURN)
            }
        };
        body.ResolveLabels();

        using var ms = new MemoryStream();
        body.WriteBytecode(ms);

        var bytes = ms.ToArray();
        Assert.Single(bytes);
        Assert.Equal((byte)OperationCode.RETURN, bytes[0]);
    }
}
