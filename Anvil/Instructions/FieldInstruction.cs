using Anvil.Instructions.ConstantPool;

namespace Anvil.Instructions;

public class FieldInstruction : Instruction
{
    public string? Owner { get; set; }
    public string? Name { get; set; }
    public string? Descriptor { get; set; }

    public int FieldRefIndex { get; set; }

    public FieldInstruction(OperationCode opCode, string owner, string name, string descriptor) : base(opCode)
    {
        ValidateOpCode(opCode);
        Owner = owner;
        Name = name;
        Descriptor = descriptor;
    }

    public FieldInstruction(OperationCode opCode, int fieldRefIndex) : base(opCode)
    {
        ValidateOpCode(opCode);
        FieldRefIndex = fieldRefIndex;
    }

    private static void ValidateOpCode(OperationCode opCode)
    {
        if (opCode is not (OperationCode.GETSTATIC
            or OperationCode.PUTSTATIC
            or OperationCode.GETFIELD
            or OperationCode.PUTFIELD))
        {
            throw new ArgumentException($"OpCode {opCode} is not a field instruction.", nameof(opCode));
        }
    }

    internal void Resolve(ConstantPoolBuilder cp)
    {
        if (Owner is null)
        {
            return;
        }

        FieldRefIndex = cp.AddFieldRef(Owner, Name!, Descriptor!);
    }

    public override int GetSize() => 3;

    public override void Write(Stream stream)
    {
        stream.WriteByte((byte)OpCode);
        stream.WriteByte((byte)((FieldRefIndex >> 8) & 0xFF));
        stream.WriteByte((byte)(FieldRefIndex & 0xFF));
    }

    public override string ToString()
    {
        if (Owner != null)
        {
            return $"{OpCode} {Owner}.{Name} {Descriptor}";
        }

        return $"{OpCode} #{FieldRefIndex}";
    }
}
