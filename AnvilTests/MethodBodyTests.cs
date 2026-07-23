using Anvil.Instructions;
using Anvil.Instructions.ConstantPool;
using Anvil.Structures;
using Anvil.Structures.Attributes;
using Anvil.Structures.Attributes.TypeAnnotations;
using Anvil.Types;

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

    [Fact]
    public void ResolveLabels_StringReference_RemainsPositionIndependent()
    {
        var target = new InsnInstruction(OperationCode.RETURN);
        var body = new MethodBody
        {
            Instructions =
            {
                new JumpInstruction(OperationCode.GOTO, "exit"),
                new InsnInstruction(OperationCode.NOP),
                target
            }
        };
        body.MarkLabel("exit", target);

        body.Instructions.Insert(1, new InsnInstruction(OperationCode.NOP));
        body.ResolveLabels();

        var jump = Assert.IsType<JumpInstruction>(body.Instructions[0]);
        Assert.Equal(5, jump.BranchOffset);
        Assert.Equal("exit", jump.Target.Name);
    }

    [Fact]
    public void ResolveLabels_DuplicateNameAtDifferentOffsets_Throws()
    {
        var first = new InsnInstruction(OperationCode.NOP);
        var second = new InsnInstruction(OperationCode.RETURN);
        var body = new MethodBody
        {
            Instructions = { first, second }
        };
        body.MarkLabel("duplicate", first);
        body.MarkLabel("duplicate", second);

        var exception = Assert.Throws<InvalidOperationException>(body.ResolveLabels);

        Assert.Contains("duplicate", exception.Message);
    }

    [Fact]
    public void ResolveLabels_FarConditional_InvertsAndAddsGotoW()
    {
        var target = new InsnInstruction(OperationCode.RETURN);
        var body = new MethodBody
        {
            Instructions =
            {
                new JumpInstruction(OperationCode.IFEQ, "far")
            }
        };
        body.Instructions.AddRange(
            Enumerable.Range(0, short.MaxValue + 1)
                .Select(_ => new InsnInstruction(OperationCode.NOP)));
        body.Instructions.Add(target);
        body.MarkLabel("far", target);

        body.ResolveLabels();

        var conditional = Assert.IsType<JumpInstruction>(body.Instructions[0]);
        var wideGoto = Assert.IsType<JumpInstruction>(body.Instructions[1]);
        Assert.Equal(OperationCode.IFNE, conditional.OpCode);
        Assert.Equal(OperationCode.GOTO_W, wideGoto.OpCode);
        Assert.Equal("far", wideGoto.Target.Name);
        Assert.Equal(8, conditional.BranchOffset);
    }

    [Fact]
    public void FromCodeAttribute_EndPcAtCodeLength_RelocatesWithEndLabel()
    {
        var attribute = new CodeAttribute
        {
            MaxStack = new TUShort(1),
            MaxLocals = new TUShort(0),
            Code = [(byte)OperationCode.NOP, (byte)OperationCode.RETURN],
            ExceptionTable =
            [
                new()
                {
                    StartPc = new TUShort(0),
                    EndPc = new TUShort(2),
                    HandlerPc = new TUShort(1),
                    CatchType = new TUShort(0)
                }
            ]
        };
        CpInfo?[] constantPool = [null];
        var body = MethodBody.FromCodeAttribute(attribute, constantPool);
        body.Instructions.Insert(1, new InsnInstruction(OperationCode.NOP));

        var result = body.ToCodeAttribute(new ConstantPoolBuilder(constantPool));

        Assert.Equal(3, result.ExceptionTable[0].EndPc.Value);
        Assert.Single(body.EndLabels);
    }

    [Fact]
    public void FromCodeAttribute_TruncatedOperand_ThrowsFormatException()
    {
        var attribute = new CodeAttribute
        {
            MaxStack = new TUShort(1),
            MaxLocals = new TUShort(0),
            Code = [(byte)OperationCode.BIPUSH]
        };

        var exception = Assert.Throws<FormatException>(
            () => MethodBody.FromCodeAttribute(attribute, [null]));

        Assert.Contains("bipush value", exception.Message);
    }

    [Fact]
    public void FromCodeAttribute_LineNumber_RelocatesByLabel()
    {
        var constantPoolBuilder = new ConstantPoolBuilder();
        var lineNumbers = new LineNumberTableAttribute
        {
            LineNumberTable =
            [
                new()
                {
                    StartPc = new TUShort(1),
                    LineNumber = new TUShort(42)
                }
            ]
        };
        var attribute = new CodeAttribute
        {
            MaxStack = new TUShort(0),
            MaxLocals = new TUShort(0),
            Code = [(byte)OperationCode.NOP, (byte)OperationCode.RETURN],
            Attributes =
            [
                AttributeInfo.CreateFromAttribute(
                    "LineNumberTable",
                    lineNumbers,
                    constantPoolBuilder)
            ]
        };
        var constantPool = constantPoolBuilder.Build();
        var body = MethodBody.FromCodeAttribute(attribute, constantPool);
        body.Instructions.Insert(1, new InsnInstruction(OperationCode.NOP));

        var result = body.ToCodeAttribute(new ConstantPoolBuilder(constantPool));
        var relocated = Assert.IsType<LineNumberTableAttribute>(
            Assert.Single(result.Attributes).ResolveBody(constantPool));

        Assert.Equal(2, Assert.Single(relocated.LineNumberTable).StartPc.Value);
    }

    [Fact]
    public void FromCodeAttribute_TypeAnnotation_RelocatesByLabel()
    {
        var constantPoolBuilder = new ConstantPoolBuilder();
        var annotation = new TypeAnnotation
        {
            TargetType = new TUByte(0x43),
            TargetInfo = new OffsetTarget { Offset = new TUShort(1) },
            TargetPath = new TypePath(),
            TypeIndex = new TUShort((ushort)constantPoolBuilder.AddUtf8("Lexample/Marker;"))
        };
        var annotations = new RuntimeVisibleTypeAnnotationsAttribute
        {
            Annotations = [annotation]
        };
        var attribute = new CodeAttribute
        {
            MaxStack = new TUShort(0),
            MaxLocals = new TUShort(0),
            Code = [(byte)OperationCode.NOP, (byte)OperationCode.RETURN],
            Attributes =
            [
                AttributeInfo.CreateFromAttribute(
                    "RuntimeVisibleTypeAnnotations",
                    annotations,
                    constantPoolBuilder)
            ]
        };
        var constantPool = constantPoolBuilder.Build();
        var body = MethodBody.FromCodeAttribute(attribute, constantPool);
        body.Instructions.Insert(1, new InsnInstruction(OperationCode.NOP));

        var result = body.ToCodeAttribute(new ConstantPoolBuilder(constantPool));
        var relocated = Assert.IsType<RuntimeVisibleTypeAnnotationsAttribute>(
            Assert.Single(result.Attributes).ResolveBody(constantPool));
        var target = Assert.IsType<OffsetTarget>(
            Assert.Single(relocated.Annotations).TargetInfo);

        Assert.Equal(2, target.Offset.Value);
    }
}
