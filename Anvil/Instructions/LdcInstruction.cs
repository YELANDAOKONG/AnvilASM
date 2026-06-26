namespace Anvil.Instructions;

public class LdcInstruction : Instruction
{
    public object? Value { get; set; }

    public int ConstantIndex { get; set; }

    private bool _resolved;

    public LdcInstruction(int value) : base(OperationCode.LDC)
    {
        Value = value;
    }

    public LdcInstruction(float value) : base(OperationCode.LDC)
    {
        Value = value;
    }

    public LdcInstruction(string value) : base(OperationCode.LDC)
    {
        Value = value;
    }

    public LdcInstruction(long value) : base(OperationCode.LDC2_W)
    {
        Value = value;
    }

    public LdcInstruction(double value) : base(OperationCode.LDC2_W)
    {
        Value = value;
    }

    public LdcInstruction(OperationCode opCode, int constantIndex) : base(opCode)
    {
        ConstantIndex = constantIndex;
        _resolved = true;
    }

    internal void Resolve(ConstantPoolBuilder cp)
    {
        if (_resolved)
        {
            return;
        }

        ConstantIndex = Value switch
        {
            int v => cp.AddInteger(v),
            float v => cp.AddFloat(v),
            long v => cp.AddLong(v),
            double v => cp.AddDouble(v),
            string v => cp.AddString(v),
            _ => throw new NotSupportedException($"LDC value type {Value?.GetType()} is not supported.")
        };
        _resolved = true;
    }

    private OperationCode EffectiveOpCode => OpCode == OperationCode.LDC2_W
        ? OperationCode.LDC2_W
        : ConstantIndex > 0xFF
            ? OperationCode.LDC_W
            : OperationCode.LDC;

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

    public override string ToString()
    {
        if (Value != null)
        {
            return $"{OpCode} {Value}";
        }

        return $"{OpCode} #{ConstantIndex}";
    }
}
