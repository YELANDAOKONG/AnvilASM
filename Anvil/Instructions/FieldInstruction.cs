namespace Anvil.Instructions;

public class FieldInstruction : Instruction
{
    public int FieldRefIndex { get; set; }

    public FieldInstruction(OperationCode opCode, int fieldRefIndex) : base(opCode)
    {
        FieldRefIndex = fieldRefIndex;
    }

    public override int GetSize() => 3;

    public override void Write(Stream stream)
    {
        stream.WriteByte((byte)OpCode);
        stream.WriteByte((byte)((FieldRefIndex >> 8) & 0xFF));
        stream.WriteByte((byte)(FieldRefIndex & 0xFF));
    }

    public override string ToString() => $"{OpCode} #{FieldRefIndex}";
}
