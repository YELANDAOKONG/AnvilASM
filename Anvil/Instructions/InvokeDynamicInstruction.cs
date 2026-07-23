using Anvil.Instructions.ConstantPool;

namespace Anvil.Instructions;

public class InvokeDynamicInstruction : Instruction
{
    public int BootstrapMethodAttrIndex { get; set; }
    public int ConstantIndex { get; private set; }
    public string? Name { get; set; }
    public string? Descriptor { get; set; }

    public InvokeDynamicInstruction(int bootstrapMethodAttrIndex) : base(OperationCode.INVOKEDYNAMIC)
    {
        ConstantIndex = bootstrapMethodAttrIndex;
    }

    public InvokeDynamicInstruction(int bootstrapMethodAttrIndex, string name, string descriptor) : base(OperationCode.INVOKEDYNAMIC)
    {
        BootstrapMethodAttrIndex = bootstrapMethodAttrIndex;
        Name = name;
        Descriptor = descriptor;
    }

    internal void Resolve(ConstantPoolBuilder cp)
    {
        if (Name is null)
        {
            return;
        }

        ConstantIndex = cp.AddInvokeDynamic(BootstrapMethodAttrIndex, Name, Descriptor!);
    }

    public override int GetSize() => 5;

    public override void Write(Stream stream)
    {
        stream.WriteByte((byte)OperationCode.INVOKEDYNAMIC);
        stream.WriteByte((byte)((ConstantIndex >> 8) & 0xFF));
        stream.WriteByte((byte)(ConstantIndex & 0xFF));
        stream.WriteByte(0);
        stream.WriteByte(0);
    }

    public override string ToString() => $"{OpCode} #{ConstantIndex}";
}
