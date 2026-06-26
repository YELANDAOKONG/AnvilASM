using Anvil.Instructions.ConstantPool;

namespace Anvil.Instructions;

public class InvokeDynamicInstruction : Instruction
{
    public int BootstrapMethodAttrIndex { get; set; }
    public string? Name { get; set; }
    public string? Descriptor { get; set; }

    private bool _resolved;

    public InvokeDynamicInstruction(int bootstrapMethodAttrIndex) : base(OperationCode.INVOKEDYNAMIC)
    {
        BootstrapMethodAttrIndex = bootstrapMethodAttrIndex;
        _resolved = true;
    }

    public InvokeDynamicInstruction(int bootstrapMethodAttrIndex, string name, string descriptor) : base(OperationCode.INVOKEDYNAMIC)
    {
        BootstrapMethodAttrIndex = bootstrapMethodAttrIndex;
        Name = name;
        Descriptor = descriptor;
    }

    internal void Resolve(ConstantPoolBuilder cp)
    {
        if (_resolved)
        {
            return;
        }

        BootstrapMethodAttrIndex = cp.AddInvokeDynamic(BootstrapMethodAttrIndex, Name!, Descriptor!);
        _resolved = true;
    }

    public override int GetSize() => 5;

    public override void Write(Stream stream)
    {
        stream.WriteByte((byte)OperationCode.INVOKEDYNAMIC);
        stream.WriteByte((byte)((BootstrapMethodAttrIndex >> 8) & 0xFF));
        stream.WriteByte((byte)(BootstrapMethodAttrIndex & 0xFF));
        stream.WriteByte(0);
        stream.WriteByte(0);
    }

    public override string ToString() => $"{OpCode} #{BootstrapMethodAttrIndex}";
}
