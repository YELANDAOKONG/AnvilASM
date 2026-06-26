namespace Anvil.Instructions;

public class TypeInstruction : Instruction
{
    public int TypeIndex { get; set; }

    public TypeInstruction(OperationCode opCode, int typeIndex) : base(opCode)
    {
        TypeIndex = typeIndex;
    }

    public override int GetSize() => 3;

    public override void Write(Stream stream)
    {
        stream.WriteByte((byte)OpCode);
        stream.WriteByte((byte)((TypeIndex >> 8) & 0xFF));
        stream.WriteByte((byte)(TypeIndex & 0xFF));
    }

    public override string ToString() => $"{OpCode} #{TypeIndex}";
}
