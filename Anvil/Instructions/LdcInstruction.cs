namespace Anvil.Instructions;

public class LdcInstruction : Instruction
{
    public int ConstantIndex { get; set; }

    public LdcInstruction(OperationCode opCode, int constantIndex) : base(opCode)
    {
        ConstantIndex = constantIndex;
    }

    private OperationCode EffectiveOpCode => OpCode switch
    {
        OperationCode.LDC => ConstantIndex > 0xFF ? OperationCode.LDC_W : OperationCode.LDC,
        OperationCode.LDC_W => ConstantIndex <= 0xFF ? OperationCode.LDC : OperationCode.LDC_W,
        _ => OpCode
    };

    public override int GetSize() => EffectiveOpCode == OperationCode.LDC ? 2 : 3;

    public override void Write(Stream stream)
    {
        var effective = EffectiveOpCode;
        stream.WriteByte((byte)effective);

        if (effective == OperationCode.LDC)
        {
            stream.WriteByte((byte)ConstantIndex);
        }
        else
        {
            stream.WriteByte((byte)((ConstantIndex >> 8) & 0xFF));
            stream.WriteByte((byte)(ConstantIndex & 0xFF));
        }
    }

    public override string ToString() => $"{OpCode} #{ConstantIndex}";
}
