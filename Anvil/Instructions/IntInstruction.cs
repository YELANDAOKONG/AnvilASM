namespace Anvil.Instructions;

public class IntInstruction : Instruction
{
    public int Value { get; set; }

    public IntInstruction(OperationCode opCode, int value) : base(opCode)
    {
        Value = value;
    }

    private OperationCode EffectiveOpCode => OpCode switch
    {
        OperationCode.BIPUSH or OperationCode.SIPUSH => Value switch
        {
            -1 => OperationCode.ICONST_M1,
            >= 0 and <= 5 => (OperationCode)((byte)OperationCode.ICONST_0 + Value),
            >= -128 and <= 127 => OperationCode.BIPUSH,
            _ => OperationCode.SIPUSH
        },
        _ => OpCode
    };

    private static bool IsIconst(OperationCode opCode)
    {
        return opCode == OperationCode.ICONST_M1
               || (opCode >= OperationCode.ICONST_0 && opCode <= OperationCode.ICONST_5);
    }

    public override int GetSize()
    {
        var effective = EffectiveOpCode;

        if (IsIconst(effective))
        {
            return 1;
        }

        return effective == OperationCode.SIPUSH ? 3 : 2;
    }

    public override void Write(Stream stream)
    {
        var effective = EffectiveOpCode;
        stream.WriteByte((byte)effective);

        if (IsIconst(effective))
        {
            return;
        }

        if (effective == OperationCode.SIPUSH)
        {
            stream.WriteByte((byte)((Value >> 8) & 0xFF));
            stream.WriteByte((byte)(Value & 0xFF));
        }
        else
        {
            stream.WriteByte((byte)Value);
        }
    }

    public override string ToString() => $"{EffectiveOpCode} {Value}";
}
