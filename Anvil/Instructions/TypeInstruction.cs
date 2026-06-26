namespace Anvil.Instructions;

public class TypeInstruction : Instruction
{
    public string? Type { get; set; }

    public int TypeIndex { get; set; }

    private bool _resolved;

    public TypeInstruction(OperationCode opCode, string type) : base(opCode)
    {
        Type = type;
    }

    public TypeInstruction(OperationCode opCode, int typeIndex) : base(opCode)
    {
        TypeIndex = typeIndex;
        _resolved = true;
    }

    internal void Resolve(ConstantPoolBuilder cp)
    {
        if (_resolved)
        {
            return;
        }

        TypeIndex = cp.AddClass(Type!);
        _resolved = true;
    }

    public override int GetSize() => 3;

    public override void Write(Stream stream)
    {
        stream.WriteByte((byte)OpCode);
        stream.WriteByte((byte)((TypeIndex >> 8) & 0xFF));
        stream.WriteByte((byte)(TypeIndex & 0xFF));
    }

    public override string ToString()
    {
        if (Type != null)
        {
            return $"{OpCode} {Type}";
        }

        return $"{OpCode} #{TypeIndex}";
    }
}
