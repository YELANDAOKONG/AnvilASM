namespace Anvil.Instructions;

public class InvokeDynamicInstruction : Instruction
{
    public int BootstrapMethodAttrIndex { get; set; }

    public InvokeDynamicInstruction(int bootstrapMethodAttrIndex) : base(OperationCode.INVOKEDYNAMIC)
    {
        BootstrapMethodAttrIndex = bootstrapMethodAttrIndex;
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
