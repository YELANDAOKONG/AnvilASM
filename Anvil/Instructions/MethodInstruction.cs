namespace Anvil.Instructions;

public class MethodInstruction : Instruction
{
    public int MethodRefIndex { get; set; }

    /// <summary>
    /// For INVOKEINTERFACE only: number of argument slots consumed by the method's parameters.
    /// Must be &gt; 0. Long and double parameters count as two slots each.
    /// </summary>
    public int Count { get; set; }

    public MethodInstruction(OperationCode opCode, int methodRefIndex) : base(opCode)
    {
        MethodRefIndex = methodRefIndex;

        if (opCode == OperationCode.INVOKEINTERFACE)
        {
            Count = 1;
        }
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

    public override string ToString() => $"{OpCode} #{MethodRefIndex}";
}
