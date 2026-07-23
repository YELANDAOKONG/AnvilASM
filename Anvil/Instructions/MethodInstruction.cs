using Anvil.Descriptors;
using Anvil.Instructions.ConstantPool;

namespace Anvil.Instructions;

public class MethodInstruction : Instruction
{
    public string? Owner { get; set; }
    public string? Name { get; set; }
    public string? Descriptor { get; set; }

    public int Count { get; set; }

    public int MethodRefIndex { get; set; }

    public MethodInstruction(OperationCode opCode, string owner, string name, string descriptor) : base(opCode)
    {
        ValidateOpCode(opCode);
        Owner = owner;
        Name = name;
        Descriptor = descriptor;

        if (opCode == OperationCode.INVOKEINTERFACE)
        {
            Count = checked(DescriptorParser.ParseMethod(descriptor).ComputeSize() + 1);
        }
    }

    public MethodInstruction(OperationCode opCode, int methodRefIndex) : base(opCode)
    {
        ValidateOpCode(opCode);
        MethodRefIndex = methodRefIndex;

        if (opCode == OperationCode.INVOKEINTERFACE)
        {
            Count = 1;
        }
    }

    private static void ValidateOpCode(OperationCode opCode)
    {
        if (opCode is not (OperationCode.INVOKEVIRTUAL
            or OperationCode.INVOKESPECIAL
            or OperationCode.INVOKESTATIC
            or OperationCode.INVOKEINTERFACE))
        {
            throw new ArgumentException($"OpCode {opCode} is not a method invocation.", nameof(opCode));
        }
    }

    internal void Resolve(ConstantPoolBuilder cp)
    {
        if (Owner is null)
        {
            return;
        }

        MethodRefIndex = OpCode == OperationCode.INVOKEINTERFACE
            ? cp.AddInterfaceMethodRef(Owner, Name!, Descriptor!)
            : cp.AddMethodRef(Owner, Name!, Descriptor!);
    }

    public override int GetSize() => OpCode switch
    {
        OperationCode.INVOKEINTERFACE => 5,
        _ => 3
    };

    public override void Write(Stream stream)
    {
        stream.WriteByte((byte)OpCode);
        stream.WriteByte((byte)((MethodRefIndex >> 8) & 0xFF));
        stream.WriteByte((byte)(MethodRefIndex & 0xFF));

        if (OpCode == OperationCode.INVOKEINTERFACE)
        {
            stream.WriteByte((byte)Count);
            stream.WriteByte(0);
        }
    }

    public override string ToString()
    {
        if (Owner != null)
        {
            return $"{OpCode} {Owner}.{Name} {Descriptor}";
        }

        return $"{OpCode} #{MethodRefIndex}";
    }
}
