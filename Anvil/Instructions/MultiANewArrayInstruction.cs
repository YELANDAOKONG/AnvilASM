namespace Anvil.Instructions;

public class MultiANewArrayInstruction : Instruction
{
    public int TypeIndex { get; set; }
    public int Dimensions { get; set; }

    public MultiANewArrayInstruction(int typeIndex, int dimensions) : base(OperationCode.MULTIANEWARRAY)
    {
        TypeIndex = typeIndex;
        Dimensions = dimensions;
    }

    public override int GetSize() => 4;

    public override void Write(Stream stream)
    {
        stream.WriteByte((byte)OperationCode.MULTIANEWARRAY);
        stream.WriteByte((byte)((TypeIndex >> 8) & 0xFF));
        stream.WriteByte((byte)(TypeIndex & 0xFF));
        stream.WriteByte((byte)Dimensions);
    }

    public override string ToString() => $"{OpCode} #{TypeIndex} {Dimensions}";
}
