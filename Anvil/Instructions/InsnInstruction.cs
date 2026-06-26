namespace Anvil.Instructions;

public class InsnInstruction : Instruction
{
    public InsnInstruction(OperationCode opCode) : base(opCode)
    {
    }

    public override int GetSize() => 1;

    public override void Write(Stream stream)
    {
        stream.WriteByte((byte)OpCode);
    }
}
