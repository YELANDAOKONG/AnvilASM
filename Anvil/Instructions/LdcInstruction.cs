using Anvil.Instructions.ConstantPool;

namespace Anvil.Instructions;

public class LdcInstruction : Instruction
{
    public object? Value { get; set; }

    public int ConstantIndex { get; set; }

    internal string? StackDescriptor { get; set; }

    private bool _resolved;
    private Func<ConstantPoolBuilder, int>? _resolver;

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

    internal static LdcInstruction CreateSymbolic(
        OperationCode opCode,
        string stackDescriptor,
        Func<ConstantPoolBuilder, int> resolver)
    {
        return new LdcInstruction(opCode, 0)
        {
            StackDescriptor = stackDescriptor,
            _resolved = false,
            _resolver = resolver
        };
    }

    internal void Resolve(ConstantPoolBuilder cp)
    {
        if (_resolver is null && Value is null)
        {
            return;
        }

        ConstantIndex = _resolver?.Invoke(cp)
            ?? Value switch
            {
                int value => cp.AddInteger(value),
                float value => cp.AddFloat(value),
                long value => cp.AddLong(value),
                double value => cp.AddDouble(value),
                string value => cp.AddString(value),
                _ => throw new NotSupportedException(
                    $"LDC value type {Value?.GetType()} is not supported.")
            };
        _resolved = true;
    }

    private OperationCode EffectiveOpCode
    {
        get
        {
            if (OpCode == OperationCode.LDC2_W)
            {
                return OperationCode.LDC2_W;
            }

            if (!_resolved && OpCode == OperationCode.LDC_W)
            {
                return OperationCode.LDC_W;
            }

            return ConstantIndex > byte.MaxValue
                ? OperationCode.LDC_W
                : OperationCode.LDC;
        }
    }

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
