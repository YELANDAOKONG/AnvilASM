namespace Anvil.Instructions;

public class FieldInstruction : Instruction
{
    public string? Owner { get; set; }
    public string? Name { get; set; }
    public string? Descriptor { get; set; }

    public int FieldRefIndex { get; set; }

    private bool _resolved;

    public FieldInstruction(OperationCode opCode, string owner, string name, string descriptor) : base(opCode)
    {
        Owner = owner;
        Name = name;
        Descriptor = descriptor;
    }

    public FieldInstruction(OperationCode opCode, int fieldRefIndex) : base(opCode)
    {
        FieldRefIndex = fieldRefIndex;
        _resolved = true;
    }

    internal void Resolve(ConstantPoolBuilder cp)
    {
        if (_resolved)
        {
            return;
        }

        FieldRefIndex = cp.AddFieldRef(Owner!, Name!, Descriptor!);
        _resolved = true;
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
